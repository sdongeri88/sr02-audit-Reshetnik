namespace TaskTracker;

// Довідник команди: логіни зберігаються в нижньому регістрі
public class UserDirectory
{
    private readonly List<User> users = new List<User>();

    public List<User> All => users;

    public bool Add(string login, string name, string role)
    {
        if (login == null || login.Trim() == "")
            return false;
        if (Find(login) != null)
            return false;
        User u = new User();
        u.Login = login.Trim().ToLower();
        u.Name = name;
        // старі ролі ("developer", "backend") - це dev
        u.Role = role == "lead" || role == "qa" ? role : "dev";
        users.Add(u);
        return true;
    }

    public User Find(string login)
    {
        foreach (User u in users)
        {
            if (u.Login == login.Trim().ToLower())
                return u;
        }
        return null;
    }
}
