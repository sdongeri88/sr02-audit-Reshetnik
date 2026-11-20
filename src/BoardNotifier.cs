namespace TaskTracker;

// Сповіщення учасникам: журнал надісланого і вивід у консоль
public class BoardNotifier
{
    private readonly List<string> sent = new List<string>();

    public IReadOnlyList<string> Sent => sent.AsReadOnly();

    public bool Quiet { get; set; }

    // login == null - повідомлення всій команді
    public void Notify(string login, string text)
    {
        if (text == null)
            return;
        string to = login ?? "team";
        sent.Add(to + ": " + text);
        if (Quiet)
            return;
        // TODO: пошта/Slack, коли дадуть доступ
        Console.WriteLine("  >> @" + to + ": " + text);
    }
}
