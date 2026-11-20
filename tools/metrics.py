"""Метрики методів C# для СР2 (методика розділу М1–М3 методички).

Запуск: python tools/metrics.py src docs/metrics-before.csv

Для кожного методу (і конструктора) рахуються:
  LOC     — логічні рядки тіла: без сигнатури, порожніх рядків, рядків лише з дужкою
            і рядків лише з коментарем;
  CC      — 1 + if/else if + ?: + for/foreach/while/do + case/плече switch-виразу
            + && + || + catch + ?? + ?.  (else, try, finally не рахуються);
  Nesting — глибина блоків над найглибшим рядком; тіло методу — рівень 1;
            тіло if/for/foreach без дужок теж є блоком;
  Params  — кількість параметрів;
  FanOut  — для класу: кількість інших власних типів проєкту, згаданих у класі.
Клони — однакові (після нормалізації пробілів) послідовності з 6+ логічних рядків.
"""
import csv
import os
import re
import sys

CONTROL = ('if', 'else', 'for', 'foreach', 'while', 'switch', 'catch', 'using', 'lock', 'do', 'try',
           'return', 'new', 'finally')


def strip_code(text):
    """Прибирає коментарі й вміст рядкових/символьних літералів, зберігаючи кількість рядків."""
    out = []
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if text.startswith('//', i):
            j = text.find('\n', i)
            j = n if j < 0 else j
            out.append(' ' * (j - i))
            i = j
        elif text.startswith('/*', i):
            j = text.find('*/', i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r'[^\n]', ' ', text[i:j]))
            i = j
        elif c == '"' or (c == '$' and i + 1 < n and text[i + 1] == '"'):
            k = i + (2 if c == '$' else 1)
            out.append('""' if c == '"' else '$""')
            while k < n and text[k] != '"':
                k += 2 if text[k] == '\\' else 1
            i = k + 1
        elif c == "'":
            k = i + 1
            while k < n and text[k] != "'":
                k += 2 if text[k] == '\\' else 1
            out.append("' '")
            i = k + 1
        else:
            out.append(c)
            i += 1
    return ''.join(out)


def is_logical(raw_line):
    s = raw_line.strip()
    if not s or s in ('{', '}', '};', '{}', '});', ')', ');'):
        return False
    if s.startswith('//') or s.startswith('/*') or s.startswith('*'):
        return False
    return True


SIG = re.compile(r'^(\s*)(?:(?:public|private|protected|internal|static|virtual|override|sealed|'
                 r'abstract|async|readonly|extern|new)\s+)+([\w<>\[\],.? ]+?\s+)?(\w+)\s*\(([^)]*)\)\s*'
                 r'(?::\s*(?:base|this)\([^)]*\))?\s*(=>.*)?$')


def find_methods(lines_clean, lines_raw, cls_name):
    methods = []
    i = 0
    while i < len(lines_clean):
        line = lines_clean[i]
        m = SIG.match(line)
        if not m or m.group(3) in CONTROL or '=' in line.split('(')[0]:
            i += 1
            continue
        indent = len(m.group(1))
        name = m.group(3)
        if name == cls_name and m.group(2) is None:
            name = '.ctor'
        params = [p for p in m.group(4).split(',') if p.strip()]
        body_start = i + 1
        if m.group(5):  # тіло-вираз
            j = i
            while not lines_clean[j].rstrip().endswith(';'):
                j += 1
            body = [(k, lines_clean[k], lines_raw[k]) for k in range(i, j + 1)]
            body[0] = (i, lines_clean[i][lines_clean[i].index('=>') + 2:],
                       lines_raw[i][lines_raw[i].index('=>') + 2:])
            methods.append((name, len(params), indent, body, i + 1))
            i = j + 1
            continue
        if body_start < len(lines_clean) and lines_clean[body_start].strip().startswith('=>'):
            j = body_start
            while not lines_clean[j].rstrip().endswith(';'):
                j += 1
            body = [(k, lines_clean[k], lines_raw[k]) for k in range(body_start, j + 1)]
            methods.append((name, len(params), indent, body, i + 1))
            i = j + 1
            continue
        # блочне тіло: від '{' до парної '}'
        j = i
        while '{' not in lines_clean[j]:
            j += 1
        depth = 0
        body = []
        k = j
        started = False
        while k < len(lines_clean):
            for ch in lines_clean[k]:
                if ch == '{':
                    depth += 1
                    started = True
                elif ch == '}':
                    depth -= 1
            body.append((k, lines_clean[k], lines_raw[k]))
            if started and depth == 0:
                break
            k += 1
        methods.append((name, len(params), indent, body[1:-1] if len(body) > 1 else [], i + 1))
        i = k + 1
    return methods


TOKENS_CC = [r'\bif\b', r'\bfor\b', r'\bforeach\b', r'\bwhile\b', r'\bcase\b', r'&&', r'\|\|',
             r'\bcatch\b', r'\?\?', r'\?\.']


def cyclomatic(body_clean):
    text = '\n'.join(body_clean)
    cc = 1
    for t in TOKENS_CC:
        cc += len(re.findall(t, text))
    # тернарний ?:  (знак ?, за яким не йде . ? або =, і перед яким не ?)
    cc += len(re.findall(r'(?<![?\w<])\?(?![.?=])\s', text))
    # плечі switch-виразу (рядки "X => Y," усередині switch { ... })
    for sw in re.finditer(r'\bswitch\s*\{(.*?)\}', text, re.S):
        cc += len(re.findall(r'=>', sw.group(1)))
    return cc


def nesting(body, indent):
    """Глибина блоків: рівень рядка = (відступ - відступ методу)/4; продовження виразів пропускаються."""
    best = 1 if body else 0
    prev = '{'
    for _, clean, raw in body:
        s = clean.strip()
        if not s:
            continue
        cont = not (prev.endswith((';', '{', '}')) or re.match(r'^(if|else|for|foreach|while)\b', prev)
                    or prev.startswith('else'))
        if is_logical(raw) and not cont:
            level = (len(raw) - len(raw.lstrip(' '))) // 4 - indent // 4
            best = max(best, level)
        prev = s
    return best


def analyze(src_dir):
    files = sorted(f for f in os.listdir(src_dir) if f.endswith('.cs'))
    project_types = set()
    texts = {}
    for f in files:
        raw = open(os.path.join(src_dir, f), encoding='utf-8-sig').read()
        clean = strip_code(raw)
        texts[f] = (raw, clean)
        project_types.update(re.findall(r'\b(?:class|enum|interface|record|struct)\s+(\w+)', clean))
    rows = []
    inventory = []
    for f in files:
        raw, clean = texts[f]
        rl, cl = raw.split('\n'), clean.split('\n')
        # класи у файлі: межі за відступом 0
        decls = [(m.start(), m.group(1), m.group(2)) for m in
                 re.finditer(r'^(?:public |internal )?(?:static |sealed |abstract )*(class|enum|interface|record|struct)\s+(\w+)',
                             clean, re.M)]
        for idx, (pos, kind, name) in enumerate(decls):
            start = clean[:pos].count('\n')
            end = clean[:decls[idx + 1][0]].count('\n') if idx + 1 < len(decls) else len(cl)
            cls_clean = cl[start:end]
            cls_raw = rl[start:end]
            used = set(re.findall(r'\b(' + '|'.join(sorted(project_types)) + r')\b', '\n'.join(cls_clean[1:])))
            used.discard(name)
            fan_out = len(used)
            methods = find_methods(cls_clean, cls_raw, name) if kind in ('class', 'record', 'struct') else []
            public_methods = sum(1 for (mn, _, _, _, ln) in methods
                                 if re.match(r'\s*public', cls_clean[ln - 1]))
            loc_cls = sum(1 for r in cls_raw if is_logical(r))
            inventory.append((f, name, kind, public_methods, loc_cls, len(methods), fan_out))
            for (mn, np, ind, body, ln) in methods:
                loc = sum(1 for _, _, r in body if is_logical(r))
                cc = cyclomatic([c for _, c, _ in body])
                nest = nesting(body, ind)
                rows.append({'File': f, 'Class': name, 'Method': mn, 'Line': start + ln,
                             'LOC': loc, 'CC': cc, 'Nesting': nest, 'Params': np, 'FanOut': fan_out})
    return rows, inventory, texts


def clones(texts, min_len=6):
    seqs = []
    for f, (raw, clean) in texts.items():
        rl = raw.split('\n')
        for i, r in enumerate(rl):
            if is_logical(r):
                seqs.append((f, i + 1, re.sub(r'\s+', ' ', r.strip())))
    index = {}
    for k in range(len(seqs) - min_len + 1):
        win = seqs[k:k + min_len]
        if len({w[0] for w in win}) != 1:
            continue
        key = tuple(w[2] for w in win)
        index.setdefault(key, []).append((win[0][0], win[0][1], win[-1][1]))
    found = []
    for key, places in index.items():
        if len(places) > 1:
            found.append(places)
    # злиття перекриттів: групуємо за першим входженням
    merged = {}
    for places in found:
        sig = tuple(sorted((p[0], p[1]) for p in places))
        merged[sig] = places
    groups = []
    for places in sorted(merged.values()):
        placed = False
        for g in groups:
            if all(any(p[0] == q[0] and abs(p[1] - q[1]) <= 12 for q in g) for p in places) and len(g) == len(places):
                for idx, p in enumerate(sorted(places)):
                    q = sorted(g)[idx]
                    g.remove(q)
                    g.append((q[0], min(q[1], p[1]), max(q[2], p[2])))
                placed = True
                break
        if not placed:
            groups.append(list(places))
    return groups


def main():
    src, out = sys.argv[1], sys.argv[2]
    rows, inventory, texts = analyze(src)
    with open(out, 'w', newline='', encoding='utf-8') as fh:
        w = csv.DictWriter(fh, fieldnames=['File', 'Class', 'Method', 'Line', 'LOC', 'CC', 'Nesting',
                                           'Params', 'FanOut'], delimiter=';')
        w.writeheader()
        for r in rows:
            w.writerow(r)
    phys = sum(len(raw.rstrip('\n').split('\n')) for raw, _ in texts.values())
    logical = sum(sum(1 for r in raw.split('\n') if is_logical(r)) for raw, _ in texts.values())
    big = [r for r in rows if r['LOC'] > 5]
    print(f'files={len(texts)} types={len(inventory)} methods={len(rows)} measured={len(big)} '
          f'physical={phys} logical={logical}')
    print('maxCC', max(r['CC'] for r in rows), 'avgCC', round(sum(r['CC'] for r in rows) / len(rows), 2),
          'maxLOC', max(r['LOC'] for r in rows), 'maxNest', max(r['Nesting'] for r in rows),
          'maxFanOut', max(i[6] for i in inventory))
    for g in clones(texts):
        print('clone', g)
    for i in inventory:
        print('inv', i)


if __name__ == '__main__':
    main()
