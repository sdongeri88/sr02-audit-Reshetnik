namespace TaskTracker;

// Збереження дошки в текстовий файл "id;key;title;column;..."
public class BoardStorage
{
    private int saveNumber;

    public string DataFile { get; set; } = Path.Combine(Path.GetTempPath(), "falcon_board.txt");

    public int Save(IEnumerable<TaskCard> tasks)
    {
        saveNumber++;
        List<string> lines = new List<string>();
        lines.Add("#falcon v" + saveNumber);
        foreach (TaskCard c in tasks)
            lines.Add(ToStorageLine(c));
        try
        {
            File.WriteAllLines(DataFile, lines);
        }
        // TODO TD-01: виняток запису ковтається, Save повертає кількість навіть без файлу
        catch (Exception) { }
        return lines.Count - 1;
    }

    public static string ToStorageLine(TaskCard c)
    {
        string s = c.Id + ";" + c.Key + ";" + c.Title.Replace(";", ",") + ";" + c.Column;
        s += ";" + (c.Assignee == null ? "" : c.Assignee) + ";" + c.Points + ";" + c.Priority;
        return s + ";" + c.Tags + ";" + (c.Blocked ? "1" : "0") + ";"
            + c.CreatedAt.ToString("yyyy-MM-dd");
    }
}
