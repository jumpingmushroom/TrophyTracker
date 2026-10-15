using TrophyTracker.Core.Model;
using Xunit;

public class BarTrackerTests
{
    private const int Key = 1100;

    [Fact]
    public void FillsFromEmptyAtTheSeedToFullWhenTheNextKillDrops()
    {
        var b = new BarTracker();
        b.Observe(Key, null, 5);
        Assert.Equal(0f, b.Fill(Key, 5));
        b.Observe(Key, 5, 4);
        Assert.Equal(0.25f, b.Fill(Key, 4));
        Assert.Equal(1f, b.Fill(Key, 1));
    }

    [Fact]
    public void ADropRestartsTheBar()
    {
        var b = new BarTracker();
        b.Observe(Key, null, 5);
        b.Observe(Key, 1, 12);
        Assert.Equal(0f, b.Fill(Key, 12));
    }

    [Fact]
    public void EachKeyHasItsOwnBar()
    {
        var b = new BarTracker();
        b.Observe(Key, null, 5);
        b.Observe(Key, 5, 4);
        b.Observe(Key + 100, null, 3);
        Assert.Equal(0.25f, b.Fill(Key, 4));
        Assert.Equal(0f, b.Fill(Key + 100, 3));
    }

    [Fact]
    public void UnobservedOrSingleKillCountersAreHandled()
    {
        var b = new BarTracker();
        Assert.Equal(0f, b.Fill(Key, 6));
        b.Observe(Key + 1, null, 0);
        Assert.Equal(1f, b.Fill(Key + 1, 0));
        b.Observe(Key + 2, 4, null);
        Assert.Equal(0f, b.Fill(Key + 2, 3));
    }
}
