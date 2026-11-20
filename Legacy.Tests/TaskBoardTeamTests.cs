using TaskTracker;

namespace Legacy.Tests;

public class TaskBoardTeamTests
{
    // Характеризаційний тест. Поле personalLimit = 2 (коментар обіцяє 3), а taras
    // уже має три задачі в роботі (104, 107, 108). Див. D-11.
    [Fact]
    public void Assign_UserOverPersonalLimit_Refused()
    {
        var (_, board) = TestBoard.Create();

        bool assigned = board.Assign(110, "taras");

        Assert.False(assigned);
        Assert.Equal("Taras Hnatiuk already has 3 task(s) in work", board.LastError);
    }

    [Fact]
    public void Assign_FreeUser_AssignsAndNotifies()
    {
        var (repo, board) = TestBoard.Create();

        bool assigned = board.Assign(110, " Olena");

        Assert.True(assigned);
        Assert.Equal("olena", repo.FindTask(110).Assignee);
        Assert.Contains("olena: TT-110 was assigned to you", board.Sent);
    }

    [Fact]
    public void Assign_UnknownTask_ReturnsFalseWithMessage()
    {
        var (_, board) = TestBoard.Create();

        bool assigned = board.Assign(404, "ivan");

        Assert.False(assigned);
        Assert.Equal("task #404 not found", board.LastError);
    }

    [Fact]
    public void AddUser_LegacyRoleAndMixedCaseLogin_NormalisedToDev()
    {
        var (_, board) = TestBoard.Create();

        bool added = board.AddUser("Nadia", "Nadiia Bondar", "designer");

        User user = board.FindUser("NADIA");
        Assert.True(added);
        Assert.Equal(("nadia", "dev"), (user.Login, user.Role));
        Assert.False(board.AddUser("nadia", "Duplicate", "qa"));
        Assert.False(board.AddUser("  ", "Nobody", "qa"));
    }

    [Fact]
    public void CreateTask_InvalidPriorityAndUpperTags_Normalised()
    {
        var (repo, board) = TestBoard.Create();

        TaskCard card = board.CreateTask(" Two-factor login ", 8, 7, "Feature,Auth", "olena");

        Assert.Equal(("Two-factor login", 3, "feature,auth", Column.Backlog),
            (card.Title, card.Priority, card.Tags, card.Column));
        Assert.Equal("TT-" + card.Id, card.Key);
        Assert.Equal(TestBoard.Morning, card.CreatedAt);
        Assert.Equal(11, repo.Tasks.Count);
    }

    [Fact]
    public void CreateTask_BlankTitle_ReturnsNull()
    {
        var (repo, board) = TestBoard.Create();

        TaskCard card = board.CreateTask("   ", 1, 1, "x", "olena");

        Assert.Null(card);
        Assert.Equal(10, repo.Tasks.Count);
    }

    [Fact]
    public void Block_EmptyReason_Unblocks()
    {
        var (repo, board) = TestBoard.Create();

        bool done = board.Block(106, "");

        Assert.True(done);
        Assert.False(repo.FindTask(106).Blocked);
        Assert.Null(repo.FindTask(106).BlockReason);
    }

    [Fact]
    public void Block_WithReason_BlocksAndNotifiesAssignee()
    {
        var (repo, board) = TestBoard.Create();

        board.Block(104, "disk full");

        Assert.True(repo.FindTask(104).Blocked);
        Assert.Contains("taras: TT-104 is blocked: disk full", board.Sent);
        Assert.False(board.Block(999, "x"));
    }
}
