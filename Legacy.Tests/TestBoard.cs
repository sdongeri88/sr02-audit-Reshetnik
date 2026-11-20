using TaskTracker;

// Тести змінюють статичні TaskBoard.MovesToday і BoardRepository.nextId,
// тому запускаються послідовно.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Legacy.Tests;

// Дошка з фіксованим годинником: використовує шов TaskBoard.Now().
internal sealed class FixedClockBoard : TaskBoard
{
    private readonly DateTime _now;

    public FixedClockBoard(BoardRepository repo, DateTime now)
        : base(repo)
    {
        _now = now;
        Quiet = true;   // наявний шов: сповіщення не друкуються в консоль
    }

    protected override DateTime Now() => _now;
}

internal static class TestBoard
{
    public static readonly DateTime Morning = new DateTime(2026, 11, 20, 10, 0, 0);
    public static readonly DateTime Evening = new DateTime(2026, 11, 20, 20, 0, 0);

    public static (BoardRepository Repo, TaskBoard Board) Create(DateTime? now = null)
    {
        var repo = new BoardRepository();
        var board = new FixedClockBoard(repo, now ?? Morning);
        return (repo, board);
    }

    // Другий наявний шов: перехоплення Console.Out для методів Print*.
    public static string Capture(Action action)
    {
        TextWriter original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}
