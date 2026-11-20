using TaskTracker;

namespace Legacy.Tests;

// Розрахунковий вузол варіанта 15: правила TaskBoard.Move (таблиця рішень у звіті).
public class TaskBoardMoveTests
{
    [Fact]
    public void Move_TodoToInProgressWhenWipFull_RefusesWithLimitMessage()
    {
        var (repo, board) = TestBoard.Create();

        bool moved = board.Move(102, Column.InProgress, "sofia", false);

        Assert.False(moved);
        Assert.Equal("WIP limit 3 reached in InProgress", board.LastError);
        Assert.Equal(Column.Todo, repo.FindTask(102).Column);
    }

    [Fact]
    public void Move_ReviewToDoneByAnotherUser_ClosesTask()
    {
        var (repo, board) = TestBoard.Create();

        bool moved = board.Move(105, Column.Done, "sofia", false);

        Assert.True(moved);
        Assert.Null(board.LastError);
        Assert.Equal(Column.Done, repo.FindTask(105).Column);
        Assert.Equal(TestBoard.Morning, repo.FindTask(105).DoneAt);
    }

    [Fact]
    public void Move_BacklogToDone_RefusesStraightClose()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(110, Column.Done, "olena", false);

        Assert.False(moved);
        Assert.Equal("cannot close TT-110 straight from Backlog", board.LastError);
    }

    [Fact]
    public void Move_BlockedTask_RefusesWithBlockReason()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(106, Column.InProgress, "taras", false);

        Assert.False(moved);
        Assert.Equal("TT-106 is blocked: waiting for bank sandbox keys", board.LastError);
    }

    [Fact]
    public void Move_BigTaskFromInProgressToDone_RequiresReview()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(107, Column.Done, "ivan", false);

        Assert.False(moved);
        Assert.Equal("TT-107 must go through Review", board.LastError);
    }

    [Fact]
    public void Move_OwnTaskSameCase_RefusesAuthor()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(108, Column.Done, "taras", false);

        Assert.False(moved);
        Assert.Equal("author cannot accept own task TT-108", board.LastError);
    }

    // Характеризаційний тест. Автора порівнюють із сирим рядком who без Trim/ToLower,
    // тому "Taras" приймає власну задачу. Це наявна поведінка; див. TD-02 у реєстрі боргу.
    [Fact]
    public void Move_OwnTaskDifferentCase_AcceptsBecauseOfRawComparison()
    {
        var (repo, board) = TestBoard.Create();

        bool moved = board.Move(108, Column.Done, "Taras", false);

        Assert.True(moved);
        Assert.Equal(Column.Done, repo.FindTask(108).Column);
    }

    [Fact]
    public void Move_OnePointTaskFromInProgress_ClosesWithoutReview()
    {
        var (repo, board) = TestBoard.Create();
        board.WipLimits[Column.InProgress] = 10;
        board.Move(105, Column.InProgress, "olena", false);

        bool moved = board.Move(105, Column.Done, "sofia", false);

        Assert.True(moved);
        Assert.Equal(Column.Done, repo.FindTask(105).Column);
    }

    [Fact]
    public void Move_UnassignedTaskToInProgress_AssignsMoverAndStartsClock()
    {
        var (repo, board) = TestBoard.Create();
        board.WipLimits[Column.InProgress] = 10;

        bool moved = board.Move(102, Column.InProgress, " Sofia ", false);

        TaskCard card = repo.FindTask(102);
        Assert.True(moved);
        Assert.Equal("sofia", card.Assignee);
        Assert.Equal(TestBoard.Morning, card.StartedAt);
    }

    [Fact]
    public void Move_BacklogToTodo_PlansTaskAndNotifiesTeam()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(103, Column.Todo, "olena", false);

        Assert.True(moved);
        Assert.Contains("team: TT-103 is planned: Dark theme for settings", board.Sent);
    }

    [Fact]
    public void Move_ExpediteAfterNineteen_Refused()
    {
        var (_, board) = TestBoard.Create(TestBoard.Evening);

        bool moved = board.Move(102, Column.InProgress, "sofia", true);

        Assert.False(moved);
        Assert.Equal("expedite lane is closed after 19:00", board.LastError);
    }

    [Fact]
    public void Move_ExpediteBeforeNineteen_IgnoresWipLimit()
    {
        var (repo, board) = TestBoard.Create();

        bool moved = board.Move(102, Column.InProgress, "sofia", true);

        Assert.True(moved);
        Assert.Equal(4, repo.Tasks.Count(c => c.Column == Column.InProgress));
    }

    // Характеризаційний тест. Коментар у коді каже, що заблоковані картки не рахуються
    // в ліміт WIP, але код їх рахує: у Todo лише 102 і заблокована 106. Див. TD-03.
    [Fact]
    public void Move_WipLimitCountsBlockedCards()
    {
        var (_, board) = TestBoard.Create();
        board.WipLimits[Column.Todo] = 2;

        bool moved = board.Move(103, Column.Todo, "olena", false);

        Assert.False(moved);
        Assert.Equal("WIP limit 2 reached in Todo", board.LastError);
    }

    [Fact]
    public void Move_Success_RecordsHistoryAndCountsMove()
    {
        var (repo, board) = TestBoard.Create();
        int movesBefore = TaskBoard.MovesToday;

        board.Move(103, Column.Todo, "Olena", false);

        HistoryEntry entry = Assert.Single(repo.History);
        Assert.Equal((103, Column.Backlog, Column.Todo, "olena", TestBoard.Morning),
            (entry.TaskId, entry.From, entry.To, entry.By, entry.At));
        Assert.Equal(movesBefore + 1, TaskBoard.MovesToday);
    }

    [Fact]
    public void Move_UnknownTask_ReturnsFalseWithMessage()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(999, Column.Todo, "olena", false);

        Assert.False(moved);
        Assert.Equal("task #999 not found", board.LastError);
    }

    [Fact]
    public void Move_InactiveUser_Refused()
    {
        var (_, board) = TestBoard.Create();

        bool moved = board.Move(110, Column.Todo, "petro", false);

        Assert.False(moved);
        Assert.Equal("unknown or inactive user 'petro'", board.LastError);
    }

    // Характеризаційний тест: null замість логіна не перевіряється і дає
    // NullReferenceException у who.Trim(). Див. TD-04.
    [Fact]
    public void Move_NullUser_ThrowsNullReference()
    {
        var (_, board) = TestBoard.Create();

        Assert.Throws<NullReferenceException>(() => board.Move(110, Column.Todo, null, false));
    }

    [Fact]
    public void Move_ReviewBackToInProgress_NotifiesRework()
    {
        var (_, board) = TestBoard.Create();
        board.WipLimits[Column.InProgress] = 10;

        board.Move(108, Column.InProgress, "olena", false);

        Assert.Contains("taras: TT-108 needs rework", board.Sent);
    }

    [Fact]
    public void Move_CriticalTaskToDone_AddsProdWarning()
    {
        var (_, board) = TestBoard.Create();

        board.Move(108, Column.Done, "olena", false);

        Assert.Contains("taras: TT-108 is accepted, thanks! Critical - keep an eye on prod.",
            board.Sent);
    }
}
