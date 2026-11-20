using TaskTracker;

namespace Legacy.Tests;

public class ReportTests
{
    // Характеризаційний тест. Пошук за тегом шукає підрядок, тому "#ui"
    // знаходить і TT-104 з тегами "build,ci". Див. TD-05.
    [Fact]
    public void Search_HashUi_MatchesSubstringOfTags()
    {
        var (_, board) = TestBoard.Create();

        List<TaskCard> found = board.Search("#ui");

        Assert.Equal(new[] { "TT-103", "TT-104", "TT-108" }, found.Select(c => c.Key));
    }

    [Fact]
    public void Search_AtLoginMixedCase_ReturnsAssigneeCards()
    {
        var (_, board) = TestBoard.Create();

        List<TaskCard> found = board.Search("@Taras");

        Assert.Equal(new[] { "TT-104", "TT-107", "TT-108" }, found.Select(c => c.Key));
    }

    [Theory]
    [InlineData("crash", 1)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void Search_TitleFragmentOrEmpty_ReturnsCapturedCount(string query, int expected)
    {
        var (_, board) = TestBoard.Create();

        List<TaskCard> found = board.Search(query);

        Assert.Equal(expected, found.Count);
    }

    [Fact]
    public void AvgLeadTime_DemoBoard_ReturnsEightDays()
    {
        var (_, board) = TestBoard.Create();

        Assert.Equal(8, board.AvgLeadTime());
    }

    [Fact]
    public void Save_DemoBoard_WritesHeaderAndAllCards()
    {
        var (_, board) = TestBoard.Create();
        board.DataFile = Path.GetTempFileName();

        int saved = board.Save();

        string[] lines = File.ReadAllLines(board.DataFile);
        string created = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd");
        Assert.Equal(10, saved);
        Assert.Equal("#falcon v1", lines[0]);
        Assert.Equal("101;TT-101;Login page returns 500;InProgress;ivan;3;1;bug,auth;0;" + created,
            lines[1]);
    }

    // Характеризаційний тест: помилка запису проковтується, метод повертає 10. Див. D-06.
    [Fact]
    public void Save_PathIsDirectory_SwallowsExceptionAndReturnsCount()
    {
        var (_, board) = TestBoard.Create();
        board.DataFile = Path.GetTempPath();

        int saved = board.Save();

        Assert.Equal(10, saved);
    }

    [Fact]
    public void PrintBoard_DemoBoard_PrintsColumnsAndCards()
    {
        var (_, board) = TestBoard.Create();

        string output = TestBoard.Capture(board.PrintBoard);

        Assert.Contains("  InProgress (WIP 3):", output);
        Assert.Contains("    TT-106 Payment webhook retries (8 sp) [blocked]", output);
        Assert.Contains("    TT-101 Login page returns 500 @ivan (3 sp)", output);
    }

    [Fact]
    public void PrintStats_DemoBoard_PrintsColumnCountsAndDonePoints()
    {
        var (_, board) = TestBoard.Create();

        string output = TestBoard.Capture(board.PrintStats);

        Assert.Contains("  backlog 2, todo 2, in progress 3, review 2, done 1 (3 sp)", output);
        Assert.Contains("  avg lead time: 8 day(s)", output);
    }

    [Theory]
    [InlineData(101, 130)]
    [InlineData(102, 20)]
    [InlineData(103, 25)]
    [InlineData(106, 105)]
    [InlineData(108, 145)]
    public void Score_DemoCard_ReturnsCapturedTemperature(int id, int expected)
    {
        var repo = new BoardRepository();

        int score = new SprintReport(repo).Score(repo.FindTask(id));

        Assert.Equal(expected, score);
    }

    [Fact]
    public void PrintStandup_DemoBoard_ListsHotCardsOnly()
    {
        var repo = new BoardRepository();

        string output = TestBoard.Capture(new SprintReport(repo).PrintStandup);

        Assert.Contains("  [130] TT-101 Login page returns 500 @ivan (3 sp)", output);
        Assert.DoesNotContain("TT-103", output);
        Assert.DoesNotContain("TT-109", output);
    }
}
