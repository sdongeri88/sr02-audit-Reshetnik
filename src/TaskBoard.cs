namespace TaskTracker;

// Дошка команди. Спершу тут був лише Move
public class TaskBoard
{
    private const int TodoWipLimit = 6;
    private const int InProgressWipLimit = 3;
    private const int ReviewWipLimit = 2;
    private const int PersonalWipLimit = 2;     // задач у роботі на одну людину
    private const int ExpediteClosingHour = 19; // після цієї години експрес-смуга закрита
    private const int SmallTaskPoints = 1;      // такі задачі можна закривати без рев'ю

    private BoardRepository repo;
    public Dictionary<Column, int> WipLimits = new Dictionary<Column, int>();
    public string LastError;            // причина останньої відмови
    // --- люди ---
    private readonly UserDirectory team = new UserDirectory();
    // --- збереження у файл ---
    public string DataFile = Path.Combine(Path.GetTempPath(), "falcon_board.txt");
    private int saveNumber;
    // --- сповіщення ---
    private readonly BoardNotifier notifier = new BoardNotifier();
    // --- статистика і пошук ---
    public static int MovesToday;
    private List<string> recentQueries = new List<string>();

    public TaskBoard(BoardRepository repo)
    {
        this.repo = repo;
        WipLimits.Add(Column.Todo, TodoWipLimit);
        WipLimits.Add(Column.InProgress, InProgressWipLimit);
        WipLimits.Add(Column.Review, ReviewWipLimit);
        // TODO: load the team from HR system
        AddUser("olena", "Olena Kravets", "lead");
        AddUser("ivan", "Ivan Moroz", "dev");
        AddUser("taras", "Taras Hnatiuk", "dev");
        AddUser("sofia", "Sofiia Lytvyn", "qa");
        AddUser("petro", "Petro Savchuk", "dev");
        FindUser("petro").Active = false;   // тимчасово, поки нема Deactivate
    }

    public TaskCard CreateTask(string title, int points, int prio, string tags, string by)
    {
        if (title == null || title.Trim() == "")
            return null;
        TaskCard t = new TaskCard();
        t.Id = BoardRepository.nextId++;
        t.Key = "TT-" + t.Id;
        t.Title = title.Trim();
        t.Points = points;
        t.Priority = prio < 1 || prio > 4 ? 3 : prio;
        t.Tags = tags == null ? "" : tags.ToLower();
        t.Reporter = by;
        t.CreatedAt = Now();
        repo.Tasks.Add(t);
        return t;
    }

    // Шов для тестів (СР2): поточний час можна підмінити в нащадку
    protected virtual DateTime Now()
    {
        return DateTime.Now;
    }

    // Переносить картку (drag&drop). false - див. LastError
    public bool Move(int id, Column to, string who, bool expedite)
    {
        LastError = null;
        TaskCard task = FindTaskOrFail(id);
        if (task == null)
            return false;
        User mover = FindActiveUserOrFail(who);
        if (mover == null)
            return false;
        Column from = task.Column;
        if (!CanLeave(task, from, to) || !IsLaneOpen(to, expedite))
            return false;
        if (to == Column.Done && !CanAccept(task, from, who))
            return false;
        ApplyMove(task, from, to, mover);
        return true;
    }

    private bool CanLeave(TaskCard task, Column from, Column to)
    {
        if (task.Blocked)
        {
            LastError = task.Key + " is blocked: " + task.BlockReason;
            return false;
        }
        // з беклогу одразу в Done не можна
        if (from == Column.Backlog && to == Column.Done)
        {
            LastError = "cannot close " + task.Key + " straight from Backlog";
            return false;
        }
        return true;
    }

    private void ApplyMove(TaskCard task, Column from, Column to, User mover)
    {
        if (to == Column.InProgress)
            StartWork(task, mover);
        if (to == Column.Done)
            task.DoneAt = Now();
        task.Column = to;
        MovesToday++;
        RecordHistory(task, from, to, mover);
        Notify(task.Assignee, MoveNotice(task, from, to));
    }

    private void StartWork(TaskCard task, User mover)
    {
        if (task.Assignee == null)
            task.Assignee = mover.Login;       // хто взяв, той і робить
        task.StartedAt = Now();
    }

    private void RecordHistory(TaskCard task, Column from, Column to, User mover)
    {
        HistoryEntry entry = new HistoryEntry();
        entry.TaskId = task.Id;
        entry.From = from;
        entry.To = to;
        entry.By = mover.Login;
        entry.At = Now();
        repo.History.Add(entry);
    }

    // експрес-смуга працює, поки є черговий, і не дивиться на WIP;
    // звичайна смуга впирається в ліміт колонки
    private bool IsLaneOpen(Column to, bool expedite)
    {
        if (expedite && Now().Hour >= ExpediteClosingHour)
        {
            LastError = "expedite lane is closed after 19:00";
            return false;
        }
        if (expedite || !WipLimits.ContainsKey(to))
            return true;
        // заблоковані в ліміт не рахуються
        int inColumn = repo.Tasks.Count(card => card.Column == to);
        if (inColumn < WipLimits[to])
            return true;
        LastError = "WIP limit " + WipLimits[to] + " reached in " + to;
        return false;
    }

    private bool CanAccept(TaskCard task, Column from, string who)
    {
        if (NeedsReview(task, from))
        {
            LastError = task.Key + " must go through Review";
            return false;
        }
        if (IsOwnTask(task, who))
        {
            LastError = "author cannot accept own task " + task.Key;
            return false;
        }
        return true;
    }

    // рев'ю обов'язкове, крім дрібниць на 1 sp
    private static bool NeedsReview(TaskCard task, Column from)
    {
        return from != Column.Review && task.Points > SmallTaskPoints;
    }

    // автор сам собі задачу не приймає
    private static bool IsOwnTask(TaskCard task, string who)
    {
        return who == task.Assignee;
    }

    // спільні кроки Move і Assign: false/null - причина в LastError
    private TaskCard FindTaskOrFail(int id)
    {
        TaskCard task = repo.FindTask(id);
        if (task == null)
            LastError = "task #" + id + " not found";
        return task;
    }

    private User FindActiveUserOrFail(string who)
    {
        User user = FindUser(who);
        if (user == null || !user.Active)
        {
            LastError = "unknown or inactive user '" + who + "'";
            return null;
        }
        return user;
    }

    // порожня причина = зняти блок
    public bool Block(int id, string reason)
    {
        TaskCard t = repo.FindTask(id);
        if (t == null)
            return false;
        t.Blocked = reason != null && reason.Trim() != "";
        t.BlockReason = t.Blocked ? reason : null;
        if (t.Blocked)
            Notify(t.Assignee, t.Key + " is blocked: " + reason);
        return true;
    }

    // ================= люди =================
    public IReadOnlyList<User> Users => team.All;

    public bool AddUser(string login, string name, string role)
    {
        return team.Add(login, name, role);
    }

    public User FindUser(string login)
    {
        return team.Find(login);
    }

    public bool Assign(int id, string who)
    {
        LastError = null;
        TaskCard task = FindTaskOrFail(id);
        if (task == null)
            return false;
        User user = FindActiveUserOrFail(who);
        if (user == null)
            return false;
        if (repo.CountInWork(user.Login) >= PersonalWipLimit)
        {
            LastError = user.Name + " already has " + repo.CountInWork(user.Login)
                + " task(s) in work";
            return false;
        }
        task.Assignee = user.Login;
        Notify(user.Login, task.Key + " was assigned to you");
        return true;
    }

    // ================= файл =================
    public int Save()
    {
        saveNumber++;
        List<string> lines = new List<string>();
        lines.Add("#falcon v" + saveNumber);
        foreach (TaskCard c in repo.Tasks)
            lines.Add(ToStorageLine(c));
        try
        {
            File.WriteAllLines(DataFile, lines);
        }
        catch (Exception) { }
        return lines.Count - 1;
    }

    public string ToStorageLine(TaskCard c)
    {
        string s = c.Id + ";" + c.Key + ";" + c.Title.Replace(";", ",") + ";" + c.Column;
        s += ";" + (c.Assignee == null ? "" : c.Assignee) + ";" + c.Points + ";" + c.Priority;
        return s + ";" + c.Tags + ";" + (c.Blocked ? "1" : "0") + ";"
            + c.CreatedAt.ToString("yyyy-MM-dd");
    }

    // ================= сповіщення =================
    public IReadOnlyList<string> Sent => notifier.Sent;

    // вмикається на час імпорту
    public bool Quiet
    {
        get => notifier.Quiet;
        set => notifier.Quiet = value;
    }

    public void Notify(string login, string text)
    {
        notifier.Notify(login, text);
    }

    public string MoveNotice(TaskCard t, Column from, Column to)
    {
        switch (to)
        {
            case Column.Todo:
                return from == Column.Backlog ? t.Key + " is planned: " + t.Title : null;
            case Column.InProgress:
                return from == Column.Review ? t.Key + " needs rework" : null;
            case Column.Review:
                return t.Key + " is in the review queue (" + t.Points + " sp)";
            case Column.Done:
                string s = t.Key + " is accepted, thanks!";
                if (t.Priority == 1)
                    s += " Critical - keep an eye on prod.";
                return s;
        }
        return null;
    }

    // ================= звіти і пошук =================
    public void PrintBoard()
    {
        foreach (Column col in Enum.GetValues(typeof(Column)))
        {
            string head = "  " + col;
            if (WipLimits.ContainsKey(col))
                head += " (WIP " + WipLimits[col] + ")";
            Console.WriteLine(head + ":");
            foreach (TaskCard c in repo.Tasks)
            {
                if (c.Column != col)
                    continue;
                Console.WriteLine("    " + c.Describe());
            }
        }
    }

    public void PrintStats()
    {
        int[] cnt = new int[5];
        int sp = 0;
        foreach (TaskCard c in repo.Tasks)
        {
            cnt[(int)c.Column]++;
            if (c.Column == Column.Done)
                sp += c.Points;
        }
        Console.WriteLine("  backlog " + cnt[0] + ", todo " + cnt[1] + ", in progress "
            + cnt[2] + ", review " + cnt[3] + ", done " + cnt[4] + " (" + sp + " sp)");
        Console.WriteLine("  avg lead time: " + AvgLeadTime() + " day(s), moves: "
            + MovesToday + ", searches: " + recentQueries.Count);
    }

    // середній lead time у днях: від створення до Done
    public int AvgLeadTime()
    {
        int sum = 0, n = 0;
        foreach (TaskCard c in repo.Tasks)
        {
            if (c.Column != Column.Done || c.DoneAt == DateTime.MinValue)
                continue;
            sum += (c.DoneAt.Date - c.CreatedAt.Date).Days;
            n++;
        }
        return n == 0 ? 0 : sum / n;
    }

    // @логін, #тег або шматок назви
    public List<TaskCard> Search(string q)
    {
        List<TaskCard> res = new List<TaskCard>();
        if (q == null || q.Trim() == "")
            return res;
        q = q.Trim().ToLower();
        recentQueries.Add(q);
        foreach (TaskCard c in repo.Tasks)
        {
            if (q.StartsWith("@"))
            {
                if (c.Assignee == q.Substring(1))
                    res.Add(c);
            }
            else if (q.StartsWith("#"))
            {
                if (c.Tags.Contains(q.Substring(1)))
                    res.Add(c);
            }
            else if (c.Title.ToLower().Contains(q))
                res.Add(c);
        }
        return res;
    }
}
