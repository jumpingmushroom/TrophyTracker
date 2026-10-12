using TrophyTracker.Core.Model;
using Xunit;

public class BarTrackerTests
{
    [Fact]
    public void FillsFromEmptyAtTheSeedToFullWhenTheNextKillDrops()
    {
        var b = new BarTracker();
        b.Observe("T", null, new CounterEntry(0.1f, 5));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 5)));
        b.Observe("T", new CounterEntry(0.1f, 5), new CounterEntry(0.1f, 4));
        Assert.Equal(0.25f, b.Fill("T", new CounterEntry(0.1f, 4)));
        Assert.Equal(1f, b.Fill("T", new CounterEntry(0.1f, 1)));
    }

    [Fact]
    public void ADropOrATierChangeRestartsTheBar()
    {
        var b = new BarTracker();
        b.Observe("T", null, new CounterEntry(0.1f, 5));
        b.Observe("T", new CounterEntry(0.1f, 1), new CounterEntry(0.1f, 12));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 12)));
        b.Observe("T", new CounterEntry(0.1f, 12), new CounterEntry(0.2f, 3));
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.2f, 3)));
    }

    [Fact]
    public void UnobservedOrSingleKillCountersAreHandled()
    {
        var b = new BarTracker();
        Assert.Equal(0f, b.Fill("T", new CounterEntry(0.1f, 6)));
        b.Observe("U", null, new CounterEntry(0.1f, 0));
        Assert.Equal(1f, b.Fill("U", new CounterEntry(0.1f, 0)));
    }
}
