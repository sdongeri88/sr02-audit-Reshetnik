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

    private BoardRepository repo;

    public SprintReport(BoardRepository repo)
    {
        this.repo = repo;
    }

    // "температура" картки: що обговорюємо першим
    public int Score(TaskCard c)
    {
        int s = PriorityScore.TryGetValue(c.Priority, out int byPriority) ? byPriority : 5;
        if (c.Tags.Contains("bug"))
            s += 30;
        if (c.Blocked)
            s += 40;            // blockers first
        if (c.Column == Column.Review)
            s += 15;
        int age = (DateTime.Today - c.CreatedAt.Date).Days;
        if (age > 14)
            s += age;           // давні задачі піднімаємо
        return s;
    }

    // hot cards for the standup: everything on the board with 50+
    public void PrintStandup()
    {
        foreach (TaskCard c in repo.Tasks)
        {
            if (c.Column == Column.Done || c.Column == Column.Backlog || Score(c) < 50)
                continue;
            Console.Write("  [" + Score(c) + "]");
            Console.WriteLine(" " + c.Describe());
        }
    }
}
