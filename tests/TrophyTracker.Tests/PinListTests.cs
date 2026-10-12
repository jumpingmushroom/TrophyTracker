using TrophyTracker.Core.Model;
using Xunit;

public class PinListTests
{
    [Fact]
    public void ParsesTrimsDropsEmptiesAndDuplicatesKeepingOrder()
    {
        Assert.Equal(new[] { "TrophyFenring", "TrophySerpent" }, PinList.Parse(" TrophyFenring, ,TrophySerpent,TrophyFenring ").ToArray());
        Assert.Empty(PinList.Parse(null));
        Assert.Empty(PinList.Parse(""));
    }

    [Fact]
    public void SerializeRoundTrips()
    {
        Assert.Equal("A,B,C", PinList.Serialize(PinList.Parse("A, B ,C")));
    }

    [Fact]
    public void ToggleAddsAtTheEndAndRemovesWherever()
    {
        Assert.Equal("A,B,C", PinList.Toggle("A,B", "C"));
        Assert.Equal("A,C", PinList.Toggle("A,B,C", "B"));
        Assert.Equal("A", PinList.Remove("A,B", "B"));
        Assert.Equal("A,B", PinList.Remove("A,B", "Z"));
        Assert.True(PinList.Contains("A, B", "B"));
        Assert.False(PinList.Contains("A,B", "b"));
    }
}
