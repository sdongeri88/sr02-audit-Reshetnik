namespace TaskTracker;

// Сховище в пам'яті; демо-дані - дошка команди Falcon
public class BoardRepository : ITaskSource
{
    public static int nextId = 111;     // 101-110 - демо-картки
    public List<TaskCard> Tasks = new List<TaskCard>();
    public List<HistoryEntry> History = new List<HistoryEntry>();

    public IReadOnlyList<TaskCard> Cards => Tasks;

    public BoardRepository()
    {
        Add(new CardSeed(101, "Login page returns 500", Column.InProgress, "ivan", 3, 1, "bug,auth", 6));
        Add(new CardSeed(102, "Export board to CSV", Column.Todo, null, 5, 3, "feature,export", 10));
        Add(new CardSeed(103, "Dark theme for settings", Column.Backlog, null, 2, 4, "ui", 20));
        Add(new CardSeed(104, "Build agents on .NET 8", Column.InProgress, "taras", 3, 2, "build,ci", 12));
        Add(new CardSeed(105, "Typo in welcome email", Column.Review, "ivan", 1, 4, "bug,mail", 4));
        TaskCard t = Add(new CardSeed(106, "Payment webhook retries", Column.Todo, null, 8, 2, "pay", 15));
        t.Blocked = true;
        t.BlockReason = "waiting for bank sandbox keys";
        Add(new CardSeed(107, "Search by tag", Column.InProgress, "taras", 5, 3, "feature,search", 7));
        Add(new CardSeed(108, "Crash on avatar upload", Column.Review, "taras", 2, 1, "bug,ui", 5));
        t = Add(new CardSeed(109, "Onboarding checklist", Column.Done, "olena", 3, 3, "docs", 10));
        t.DoneAt = DateTime.Today.AddDays(-2);
        Add(new CardSeed(110, "Rate limit for public API", Column.Backlog, null, 5, 2, "feature,api", 4));
    }

    public TaskCard Add(CardSeed seed)
    {
        TaskCard t = new TaskCard();
        t.Id = seed.Id;
        t.Key = "TT-" + seed.Id;
        t.Title = seed.Title;
        t.Column = seed.Column;
        t.Assignee = seed.Assignee;
        t.Points = seed.Points;
        t.Priority = seed.Priority;
        t.Tags = seed.Tags;
        t.Reporter = "olena";
        t.CreatedAt = DateTime.Today.AddDays(-seed.AgeDays);
        Tasks.Add(t);
        return t;
    }

    // задачі людини в роботі (In progress + Review)
    public int CountInWork(string login)
    {
        int n = 0;
        foreach (TaskCard c in Tasks)
        {
            bool inWork = c.Column == Column.InProgress || c.Column == Column.Review;
            n += c.Assignee == login && inWork ? 1 : 0;
        }
        return n;
    }

    public TaskCard FindTask(int id)
    {
        foreach (TaskCard t in Tasks)
            if (t.Id == id)
                return t;
        return null;
    }
}
