# Реєстр дефектів проєктування коду (СР2, варіант 15 TaskTracker)

Код: тег `baseline`. Рядки — за вихідними файлами `src/*.cs`.
Серйозність — 1–5; частота — кількість місць (понад 4 рахуємо як 4); пріоритет = серйозність × частота.
Профільний дефект варіанта — D-01 (клас-менеджер, що робить усе).

| ID | Місце (файл:рядки) | Дефект | Серйозність | Частота | Пріоритет |
|---|---|---|---|---|---|
| D-01 | TaskBoard.cs:4-371 | Клас-менеджер із низькою зв'язністю (великий клас): 9 полів, 17 методів, 5 обов'язків | 5 | 4 | 20 |
| D-13 | TaskBoard.cs:49,90,115,131,142; 264,294-323 | Прихована залежність від годинника й консолі | 4 | 4 | 16 |
| D-05 | TaskBoard.cs:11,25-27,90,120; SprintReport.cs:16-33,41 | Магічні числа | 3 | 4 | 12 |
| D-08 | Models.cs:14-15,28; TaskBoard.cs:172,356-357 | Одержимість елементарними типами (теги, роль, пріоритет — рядки й числа) | 3 | 4 | 12 |
| D-09 | Models.cs:8-21; TaskBoard.cs:8-19; BoardRepository.cs:6-8 | Публічні поля й статичний змінюваний стан | 3 | 4 | 12 |
| D-15 | TaskBoard.cs:37-40,55-145,187-215; BoardRepository.cs:45-51 | Код помилки замість винятку (false + LastError, null) | 3 | 4 | 12 |
| D-04 | Program.cs:62-69; SprintReport.cs:44-51; TaskBoard.cs:299-306 | Дублювання: форматування рядка картки, 8 рядків, 3 входження | 3 | 3 | 9 |
| D-11 | TaskBoard.cs:11,98; Models.cs:5 | Коментар, що бреше або виправдовує | 3 | 3 | 9 |
| D-03 | TaskBoard.cs:57-73 і 189-205 | Дублювання: пошук картки й учасника, 15 рядків, 2 входження | 4 | 2 | 8 |
| D-12 | TaskBoard.cs:14,58-142,218,246 | Імена, що не пояснюють призначення (t, u, x, h, cnt, save_no, busyCount, cardToLine) | 2 | 4 | 8 |
| D-07 | TaskBoard.cs:133-134,367-370; SprintReport.cs:29 | Мертвий код (закоментовані гілки, невикористаний метод) | 2 | 3 | 6 |
| D-10 | TaskBoard.cs:218-228,246-252; SprintReport.cs:14-35 | Заздрість до чужих даних | 2 | 3 | 6 |
| D-02 | TaskBoard.cs:55-145 | Довгий метод Move: 54 логічні рядки, CC 21, вкладеність 4 | 5 | 1 | 5 |
| D-06 | TaskBoard.cs:238-242 | Проковтнутий виняток у Save | 4 | 1 | 4 |
| D-14 | BoardRepository.cs:27-28 | Довгий список параметрів: Add з 8 параметрами | 2 | 2 | 4 |

Різних типів дефектів за каталогом теми 7: великий клас, довгий метод, дублювання коду, магічне число,
проковтнутий виняток, мертвий код, одержимість елементарними типами, заздрість до чужих даних,
коментар-виправдання, довгий список параметрів — 10, а також три дефекти поза каталогом (D-09, D-13, D-15).

## Картки дефектів

### D-01. Клас-менеджер, що робить усе (профільний)
- **Місце:** TaskBoard.cs, рядки 4–371, увесь клас.
- **Дефект:** низька зв'язність: 9 полів і 17 методів, які утворюють п'ять незалежних груп — переміщення
  карток, довідник людей, збереження у файл, сповіщення, звіти й пошук. Коментарі-розділювачі
  `// --- люди ---`, `// --- збереження у файл ---` самі показують межі майбутніх класів. Fan-out 5.
- **Доказ:**
```csharp
public class TaskBoard
{
    private BoardRepository repo;
    public Dictionary<Column, int> WipLimits = ...;
    public string LastError;
    // --- люди ---
    public List<User> Users = new List<User>();
    // --- збереження у файл ---
    public string DataFile = Path.Combine(...);
    // --- сповіщення ---
    public List<string> Sent = new List<string>();
```
- **Чим шкодить:** будь-яка зміна формату файлу, команди чи сповіщень торкається класу дошки; тест збереження
  мусить створити всю дошку з людьми й лімітами WIP.
- **Категорія впливу:** змінюваність, тестованість, читаність.
- **Серйозність:** 5; **частота:** 4 (п'ять груп обов'язків); **пріоритет:** 20.
- **Гіпотеза виправлення:** Extract Class (UserDirectory, BoardNotifier, BoardStorage), Move Method
  (BusyCount у BoardRepository, форматування в TaskCard).

### D-02. Довгий метод Move
- **Місце:** TaskBoard.cs, рядки 55–145, метод Move.
- **Дефект:** 54 логічні рядки, CC = 21 (і ручний підрахунок, і CA1502), вкладеність 4, сім причин відмови.
- **Доказ:**
```csharp
if (expedite)
{
    // поки є черговий; WIP не дивимось
    if (DateTime.Now.Hour >= 19) { ... return false; }
}
else if (WipLimits.ContainsKey(to))
{
    int cnt = 0;
    foreach (TaskCard x in repo.Tasks)
    {
        if (x.Column == to)
            cnt++;
```
- **Чим шкодить:** 21 незалежний шлях — для повного покриття потрібно 21 тест; нове правило переходу
  доводиться вписувати в середину методу.
- **Категорія впливу:** читаність, тестованість, змінюваність.
- **Серйозність:** 5; **частота:** 1; **пріоритет:** 5.
- **Гіпотеза:** Extract Method (FindTaskOrFail, CanLeave, ApplyMove), Replace Nested Conditional with Guard
  Clauses (IsLaneOpen), Decompose Conditional (CanAccept).

### D-03. Дублювання пошуку картки й учасника
- **Місце:** TaskBoard.cs, рядки 57–73 (Move) і 189–205 (Assign).
- **Дефект:** клон на 15 логічних рядків, 2 входження.
- **Доказ:**
```csharp
TaskCard t = repo.FindTask(id);
if (t == null)
{
    LastError = "task #" + id + " not found";
    return false;
}
User u = null;
foreach (User x in Users)
{
    if (x.Login == who.Trim().ToLower())
        u = x;
}
```
- **Чим шкодить:** зміна правила пошуку учасника (наприклад, за e-mail) вимагає двох однакових правок.
- **Категорія впливу:** змінюваність, надійність.
- **Серйозність:** 4; **частота:** 2; **пріоритет:** 8.
- **Гіпотеза:** Extract Method FindTaskOrFail і FindActiveUserOrFail.

### D-04. Дублювання форматування рядка картки
- **Місце:** Program.cs:62–69, SprintReport.cs:44–51, TaskBoard.cs:299–306.
- **Дефект:** клон на 8 рядків, 3 входження (різниться лише відступ на початку).
- **Доказ:**
```csharp
string line = "    " + c.Key + " " + c.Title;
if (c.Assignee != null)
    line += " @" + c.Assignee;
if (c.Points > 0)
    line += " (" + c.Points + " sp)";
if (c.Blocked)
    line += " [blocked]";
Console.WriteLine(line);
```
- **Чим шкодить:** нова позначка картки (наприклад, «[expedite]») потребує правки в трьох файлах — дробовик.
- **Категорія впливу:** змінюваність.
- **Серйозність:** 3; **частота:** 3; **пріоритет:** 9.
- **Гіпотеза:** Move Method — метод TaskCard.Describe.

### D-05. Магічні числа
- **Місце:** TaskBoard.cs:11, 25–27, 90, 120; SprintReport.cs:16–33, 41.
- **Дефект:** ліміти WIP 6/3/2, особистий ліміт 2, година 19, поріг 1 sp, ваги 100/50/20/5/30/40/15,
  давність 14 днів, поріг стендапу 50.
- **Доказ:**
```csharp
WipLimits.Add(Column.Todo, 6);
WipLimits.Add(Column.InProgress, 3);
WipLimits.Add(Column.Review, 2);
...
if (DateTime.Now.Hour >= 19)
...
if (from != Column.Review && t.Points > 1)
```
- **Чим шкодить:** зміст чисел відомий лише автору; число 2 у різних місцях означає різні речі.
- **Категорія впливу:** читаність, змінюваність.
- **Серйозність:** 3; **частота:** 4; **пріоритет:** 12.
- **Гіпотеза:** Replace Magic Number with Constant; для пріоритетів — Replace Conditional with Table.

### D-06. Проковтнутий виняток
- **Місце:** TaskBoard.cs, рядки 238–242, метод Save.
- **Доказ:**
```csharp
try
{
    File.WriteAllLines(DataFile, lines);
}
catch (Exception) { }
return lines.Count - 1;
```
- **Чим шкодить:** при повному диску чи хибному шляху Save повідомляє «збережено 10 карток», а файл
  не записано; тест Save_PathIsDirectory це фіксує.
- **Категорія впливу:** надійність.
- **Серйозність:** 4; **частота:** 1; **пріоритет:** 4.
- **Гіпотеза:** Replace Error Code with Exception; змінює поведінку — до реєстру боргу (TD-01).

### D-07. Мертвий код
- **Місце:** TaskBoard.cs:133–134 (закоментований reopen), 367–370 (LastQuery без викликів);
  SprintReport.cs:29 (закоментований бонус).
- **Доказ:**
```csharp
//if (from == Column.Done)
//    t.DoneAt = DateTime.MinValue;   // reopen - спитати Олену
...
public string LastQuery()
{
    return recentQueries.Count == 0 ? null
        : recentQueries[recentQueries.Count - 1];
}
```
- **Чим шкодить:** читач не знає, чи це вимога, чи чернетка; невикористаний метод тягне за собою поле.
- **Категорія впливу:** читаність.
- **Серйозність:** 2; **частота:** 3; **пріоритет:** 6.
- **Гіпотеза:** Remove Dead Code.

### D-08. Одержимість елементарними типами
- **Місце:** Models.cs:14–15 (Priority int, Tags string), 28 (Role string); TaskBoard.cs:172, 356–357.
- **Доказ:**
```csharp
public int Priority = 3;        // 1 - critical ... 4 - low
public string Tags = "";        // "bug,ui"
...
else if (q.StartsWith("#"))
{
    if (c.Tags.Contains(q.Substring(1)))
        res.Add(c);
}
```
- **Чим шкодить:** пошук `#ui` знаходить картку з тегом `build` (підрядок); пріоритет 7 тихо стає 3.
- **Категорія впливу:** надійність, читаність.
- **Серйозність:** 3; **частота:** 4; **пріоритет:** 12.
- **Гіпотеза:** Replace Primitive with Value Object (TagSet, Priority) — змінює поведінку пошуку, тому TD-05.

### D-09. Публічні поля й статичний змінюваний стан
- **Місце:** Models.cs:8–21; TaskBoard.cs:8–19; BoardRepository.cs:6–8.
- **Доказ:**
```csharp
public static int nextId = 111;     // 101-110 - демо-картки
public List<TaskCard> Tasks = new List<TaskCard>();
...
public List<User> Users = new List<User>();
public List<string> Sent = new List<string>();
public static int MovesToday;
```
- **Чим шкодить:** будь-хто може додати учасника в обхід AddUser; статичні лічильники змушують
  запускати тести послідовно.
- **Категорія впливу:** надійність, тестованість.
- **Серйозність:** 3; **частота:** 4; **пріоритет:** 12.
- **Гіпотеза:** Encapsulate Collection; статичний стан — TD-07.

### D-10. Заздрість до чужих даних
- **Місце:** TaskBoard.cs:218–228 (busyCount), 246–252 (cardToLine); SprintReport.cs:14–35 (Score).
- **Доказ:**
```csharp
public int busyCount(string who)
{
    int n = 0;
    foreach (TaskCard c in repo.Tasks)
    {
        if (c.Assignee == who && (c.Column == Column.InProgress
            || c.Column == Column.Review))
            n++;
    }
    return n;
}
```
- **Чим шкодить:** метод не використовує жодного поля TaskBoard, лише дані сховища; зміна сховища
  ламає дошку.
- **Категорія впливу:** змінюваність.
- **Серйозність:** 2; **частота:** 3; **пріоритет:** 6.
- **Гіпотеза:** Move Method (CountInWork у BoardRepository), Extract Interface для звіту.

### D-11. Коментар, що бреше або виправдовує
- **Місце:** TaskBoard.cs:11, 98; Models.cs:5.
- **Доказ:**
```csharp
private int personalLimit = 2;      // max 3 задачі на людину
...
// заблоковані в ліміт не рахуються
int cnt = 0;
foreach (TaskCard x in repo.Tasks)
{
    if (x.Column == to)
        cnt++;
```
- **Чим шкодить:** коментар обіцяє 3 задачі й виключення заблокованих, код робить 2 і рахує всіх;
  тести Assign_UserOverPersonalLimit і Move_WipLimitCountsBlockedCards фіксують поведінку коду.
- **Категорія впливу:** читаність, надійність.
- **Серйозність:** 3; **частота:** 3; **пріоритет:** 9.
- **Гіпотеза:** Replace Magic Number with Constant (ім'я замість коментаря); розбіжність про
  заблоковані — до TD-03.

### D-12. Імена, що не пояснюють призначення
- **Місце:** TaskBoard.cs:14 (save_no), 58–142 (t, u, x, h, cnt у Move), 218 (busyCount), 246 (cardToLine).
- **Доказ:**
```csharp
TaskCard t = repo.FindTask(id);
User u = null;
foreach (User x in Users)
...
int cnt = 0;
foreach (TaskCard x in repo.Tasks)
...
HistoryEntry h = new HistoryEntry();
```
- **Чим шкодить:** у методі на 54 рядки `x` означає то учасника, то картку; публічні методи з малої
  літери порушують конвенцію .NET.
- **Категорія впливу:** читаність.
- **Серйозність:** 2; **частота:** 4; **пріоритет:** 8.
- **Гіпотеза:** Rename.

### D-13. Прихована залежність від годинника й консолі
- **Місце:** TaskBoard.cs:49, 90, 115, 131, 142 (DateTime.Now); 264, 294–323 (Console).
- **Доказ:**
```csharp
if (expedite)
{
    if (DateTime.Now.Hour >= 19)
    {
        LastError = "expedite lane is closed after 19:00";
        return false;
    }
}
...
t.StartedAt = DateTime.Now;
```
- **Чим шкодить:** правило експрес-смуги неможливо перевірити тестом удень; час старту не порівняти.
- **Категорія впливу:** тестованість.
- **Серйозність:** 4; **частота:** 4; **пріоритет:** 16.
- **Гіпотеза:** шов Now() (прийом 2) — виконано до тестів; консоль — перехоплення Console.Out у тестах.

### D-14. Довгий список параметрів
- **Місце:** BoardRepository.cs:27–28, метод Add.
- **Доказ:**
```csharp
public TaskCard Add(int id, string title, Column col,
    string who, int sp, int prio, string tags, int age)
...
Add(101, "Login page returns 500", Column.InProgress,
    "ivan", 3, 1, "bug,auth", 6);
```
- **Чим шкодить:** три цілі числа поспіль (3, 1, 6) легко переплутати місцями — компілятор не допоможе.
- **Категорія впливу:** читаність, надійність.
- **Серйозність:** 2; **частота:** 2; **пріоритет:** 4.
- **Гіпотеза:** Introduce Parameter Object (CardSeed).

### D-15. Код помилки замість винятку
- **Місце:** TaskBoard.cs:37–40 (CreateTask → null), 55–145 (Move → false + LastError), 187–215 (Assign);
  BoardRepository.cs:45–51 (FindTask → null).
- **Доказ:**
```csharp
public bool Move(int id, Column to, string who,
    bool expedite)
{
    LastError = null;
    ...
    LastError = "task #" + id + " not found";
    return false;
```
- **Чим шкодить:** викликач мусить пам'ятати перевірити результат і прочитати LastError одразу,
  до наступного виклику (тимчасове поле); null від CreateTask падає далі за течією.
- **Категорія впливу:** надійність.
- **Серйозність:** 3; **частота:** 4; **пріоритет:** 12.
- **Гіпотеза:** Replace Error Code with Exception — змінює поведінку й вивід Program, тому відмова
  (рішення DL-04) і TD-04.
