# План рефакторингу з пріоритезацією «вигода / ризик» (СР2, варіант 15)

Шкали: **В** — вигода 1–5; **Р** — ризик 1–5; **Т** — трудомісткість, хв; **Індекс = В ÷ Р**.
Сортування: спершу за групою залежностей (A → H: крок групи спирається на результат попередньої),
усередині групи — за індексом за спаданням, за однакового індексу — менш трудомісткий вище.
Група H додана після проміжного вимірювання: після кроків A–G три методи ще мали вкладеність 4.

| Черга | Група | Дефект | Код | Рефакторинг | В | Р | Т, хв | Індекс |
|---|---|---|---|---|---|---|---|---|
| 1 | A | D-07 | R17 | Remove Dead Code: reopen, бонус у Score, LastQuery | 3 | 1 | 10 | 3,0 |
| 2 | A | D-05, D-11 | R04 | Replace Magic Number with Constant у TaskBoard | 3 | 1 | 15 | 3,0 |
| 3 | A | D-12 | R02 | Rename: t/u/x/h/cnt, save_no, busyCount, cardToLine | 3 | 1 | 15 | 3,0 |
| 4 | B | D-03 | R01 | Extract Method FindTaskOrFail, FindActiveUserOrFail | 4 | 2 | 20 | 2,0 |
| 5 | B | D-02 | R06 | Replace Nested Conditional with Guard Clauses: IsLaneOpen | 4 | 2 | 20 | 2,0 |
| 6 | B | D-02 | R05 | Decompose Conditional: CanAccept, NeedsReview, IsOwnTask | 4 | 2 | 20 | 2,0 |
| 7 | B | D-02 | R01 | Extract Method CanLeave, ApplyMove, StartWork, RecordHistory | 5 | 3 | 30 | 1,67 |
| 8 | C | D-10 | R10 | Move Method BusyCount → BoardRepository.CountInWork | 3 | 1 | 10 | 3,0 |
| 9 | C | D-04 | R10 | Move Method: форматування картки → TaskCard.Describe | 4 | 2 | 20 | 2,0 |
| 10 | D | D-01 | R09 | Extract Class UserDirectory | 5 | 3 | 40 | 1,67 |
| 11 | D | D-09 | R13 | Encapsulate Collection: Users → IReadOnlyList | 3 | 2 | 10 | 1,5 |
| 12 | D | D-01 | R09 | Extract Class BoardNotifier | 4 | 3 | 30 | 1,33 |
| 13 | D | D-01, D-06 | R09 | Extract Class BoardStorage | 4 | 3 | 30 | 1,33 |
| 14 | E | — | R16 | Split Loop у PrintStats | 2 | 1 | 10 | 2,0 |
| 15 | E | — | R15 | Replace Temp with Query: sp → DonePoints | 2 | 1 | 10 | 2,0 |
| 16 | F | D-05 | R08 | Replace Conditional with Table: пріоритет → PriorityScore | 4 | 1 | 20 | 4,0 |
| 17 | F | D-05 | R04 | Replace Magic Number with Constant у SprintReport | 3 | 1 | 15 | 3,0 |
| 18 | F | — | R03 | Extract Variable: offBoard, inWork | 2 | 1 | 10 | 2,0 |
| 19 | F | D-10 | R14 | Extract Interface ITaskSource для SprintReport | 3 | 2 | 20 | 1,5 |
| 20 | G | D-14 | R11 | Introduce Parameter Object CardSeed | 2 | 2 | 20 | 1,0 |
| 21 | G | — | R18 | Introduce Assertion у BoardRepository.Add (після R11) | 2 | 1 | 10 | 2,0 |
| 22 | H | D-02 | R01 | Extract Method Matches у Search | 3 | 1 | 15 | 3,0 |
| 23 | H | — | R01 | Extract Method DoneNotice у MoveNotice | 2 | 1 | 10 | 2,0 |
| 24 | H | — | R01 | Extract Method PrintColumn у PrintBoard | 2 | 1 | 10 | 2,0 |

Виняток із сортування: R18 (черга 21) має вищий індекс за R11, але твердження перевіряє поля CardSeed,
тому виконується після R11. Від R20 (Replace Error Code with Exception) і R12 (Replace Primitive with
Value Object для тегів) свідомо відмовились — див. журнал рішень DL-04, DL-05 і реєстр боргу.

## Правила безпеки

1. Один рефакторинг — один коміт `refactor(Клас): Rnn суть`. Змішувати рефакторинг зі зміною поведінки заборонено.
2. Крок довший за 20 хвилин розбивається на менші (Extract Class виконано трьома комітами, по класу на коміт).
3. Після кожного кроку — `dotnet build` і `dotnet test`; коміт лише за зеленої сітки (перевіряє скрипт).
4. Якщо тести червоні понад 10 хвилин — `git checkout .` і крок спочатку меншими порціями.
5. Спершу кроки з низьким ризиком (група A, Р = 1): вони прибирають шум і роблять наступні кроки безпечнішими.
6. Файли тестів не змінюються після тегу safety-net; публічний API TaskBoard зберігається (делегування),
   щоб жоден рефакторинг не вимагав правки тестів.
7. Порядок побічних ефектів у Move перевіряється окремо: тести на StartedAt, DoneAt, History і Sent.
