namespace TaskTracker;

// Звіти для стендапу (писав Андрій)
public class SprintReport
{
    // базова "температура" за пріоритетом (1 - critical ... 4 - low)
    private static readonly Dictionary<int, int> PriorityScore = new Dictionary<int, int>
    {
        { 1, 100 },
        { 2, 50 },
        { 3, 20 },
    };

    private const int LowPriorityScore = 5;
    private const int BugBonus = 30;
    private const int BlockedBonus = 40;
    private const int ReviewBonus = 15;
    private const int StaleAfterDays = 14;
    private const int HotScore = 50;

    private BoardRepository repo;

    public SprintReport(BoardRepository repo)
    {
        this.repo = repo;
    }

    // "температура" картки: що обговорюємо першим
    public int Score(TaskCard c)
    {
        int s = PriorityScore.TryGetValue(c.Priority, out int byPriority) ? byPriority : LowPriorityScore;
        if (c.Tags.Contains("bug"))
            s += BugBonus;
        if (c.Blocked)
            s += BlockedBonus;  // blockers first
        if (c.Column == Column.Review)
            s += ReviewBonus;
        int age = (DateTime.Today - c.CreatedAt.Date).Days;
        if (age > StaleAfterDays)
            s += age;           // давні задачі піднімаємо
        return s;
    }

    // hot cards for the standup: everything on the board with 50+
    public void PrintStandup()
    {
        foreach (TaskCard c in repo.Tasks)
        {
            bool offBoard = c.Column == Column.Done || c.Column == Column.Backlog;
            if (offBoard || Score(c) < HotScore)
                continue;
            Console.Write("  [" + Score(c) + "]");
            Console.WriteLine(" " + c.Describe());
        }
    }
}
