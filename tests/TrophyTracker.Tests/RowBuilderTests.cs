using System.Collections.Generic;
using System.Linq;
using TrophyTracker.Core.Model;
using Xunit;

public class RowBuilderTests
{
    private static RowInputs Inputs(params string[] pins)
    {
        return new RowInputs
        {
            Catalog = TestData.Catalog(),
            Pins = pins.ToList(),
            Discovered = DiscoveredSet.Parse("Fenring,Surtling,Deer,Eikthyr,Skeleton,Bjorn"),
            Collected = new HashSet<string>(),
            Counters = new Dictionary<string, CounterEntry>(),
            Kills = new Dictionary<string, float>(),
            Bars = new BarTracker(),
            FlashUntil = new Dictionary<string, float>(),
            RecentTime = float.NegativeInfinity,
            RecentSeconds = 60f,
            ShowRecent = true,
            Now = 100f,
            MaxRows = 8,
        };
    }

    private static RowView Only(RowInputs i)
    {
        return Assert.Single(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void UnknownBeforeTheFirstKillShowsTheWorstCase()
    {
        RowView v = Only(Inputs("TrophyFenring"));
        Assert.Equal(RowState.Unknown, v.State);
        Assert.Equal(20, v.Count);
        Assert.Equal(10f, v.ChancePercent, 3);
        Assert.Equal("$enemy_fenring", v.NameToken);
        Assert.Equal(-1f, v.BarFill);
    }

    [Fact]
    public void ExactWithTheTierItTracks()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters["TrophyFenring"] = new CounterEntry(PityMath.EffectiveChance(0.1f, true, 1), 7);
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(7, v.Count);
        Assert.Equal(1, v.Stars);
        Assert.Equal(0f, v.BarFill);
    }

    [Fact]
    public void ExactWithAnUnmatchedChanceHasNoStar()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters["TrophyFenring"] = new CounterEntry(0.123f, 0);
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(1, v.Count);
        Assert.Equal(-1, v.Stars);
    }

    [Fact]
    public void ExactIsDimmedInMultiplayer()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Dim = true;
        i.Counters["TrophyFenring"] = new CounterEntry(0.1f, 3);
        Assert.True(Only(i).Dim);
        i.Counters.Clear();
        Assert.False(Only(i).Dim);
    }

    [Fact]
    public void HighChanceRollsBossesAlwaysDropOrphansHaveNoSource()
    {
        Assert.Equal(RowState.Roll, Only(Inputs("TrophyDeer")).State);
        Assert.Equal(RowState.Always, Only(Inputs("TrophyEikthyr")).State);
        RowInputs i = Inputs("TrophyOrphan");
        i.Collected.Add("TrophyOrphan");
        RowView v = Only(i);
        Assert.Equal(RowState.NoSource, v.State);
        Assert.Null(v.NameToken);
    }

    [Fact]
    public void NoPseudoDropsTurnsEverythingIntoARoll()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.NoPseudoDrops = true;
        i.Counters["TrophyFenring"] = new CounterEntry(0.1f, 3);
        Assert.Equal(RowState.Roll, Only(i).State);
    }

    [Fact]
    public void UndiscoveredPinsAndUnknownPinsAreNotShown()
    {
        RowInputs i = Inputs("TrophySerpent", "TrophyFenring");
        i.Discovered = DiscoveredSet.Parse("Surtling");
        Assert.Empty(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void PrimarySourceIsTheMostKilledThenTheDiscoveredOne()
    {
        TrophyCatalog c = TestData.Catalog();
        TrophyInfo bear = c.Get("TrophyBjorn");
        var d = DiscoveredSet.Parse("Bjorn");
        Assert.Equal("Bjorn", RowBuilder.PrimarySource(bear, new Dictionary<string, float>(), d).CreaturePrefab);
        var kills = new Dictionary<string, float> { { "$enemy_bjorn_variant", 2f }, { "$enemy_bjorn", 1f } };
        Assert.Equal("Bjorn_Variant", RowBuilder.PrimarySource(bear, kills, d).CreaturePrefab);
    }

    [Fact]
    public void RecentKillComesFirstForSixtySecondsUnlessPinned()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.RecentToken = "$enemy_surtling";
        i.RecentTime = 50f;
        List<RowView> rows = RowBuilder.Build(i).Rows;
        Assert.Equal(new[] { "TrophySurtling", "TrophyFenring" }, rows.Select(r => r.TrophyId).ToArray());
        Assert.True(rows[0].Recent);
        Assert.False(rows[1].Recent);

        i.Now = 111f;
        Assert.Single(RowBuilder.Build(i).Rows);

        i.Now = 100f;
        i.Collected.Add("TrophySurtling");
        rows = RowBuilder.Build(i).Rows;
        Assert.Equal(new[] { "TrophySurtling", "TrophyFenring" }, rows.Select(r => r.TrophyId).ToArray());
        Assert.True(rows[0].Recent);

        i.Collected.Clear();
        i.ShowRecent = false;
        Assert.Single(RowBuilder.Build(i).Rows);

        i.ShowRecent = true;
        i.RecentToken = "$enemy_fenring";
        Assert.Single(RowBuilder.Build(i).Rows);
    }

    [Fact]
    public void CollectedFlashesThenFallsBackToTheNormalState()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Collected.Add("TrophyFenring");
        i.FlashUntil["TrophyFenring"] = 103f;
        Assert.Equal(RowState.Collected, Only(i).State);
        i.Now = 104f;
        Assert.Equal(RowState.Unknown, Only(i).State);
    }

    [Fact]
    public void MaxRowsCollapsesTheRestIntoMore()
    {
        RowInputs i = Inputs("TrophyFenring", "TrophySurtling", "TrophyDeer", "TrophyEikthyr");
        i.MaxRows = 2;
        RowList list = RowBuilder.Build(i);
        Assert.Equal(2, list.Rows.Count);
        Assert.Equal(2, list.More);
        Assert.Equal("TrophyFenring", list.Rows[0].TrophyId);
    }

    [Fact]
    public void RecentKillWithZeroSecondsNeverExpires()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.RecentToken = "$enemy_surtling";
        i.RecentTime = 50f;
        i.RecentSeconds = 0f;
        i.Now = 100000f;
        List<RowView> rows = RowBuilder.Build(i).Rows;
        Assert.Equal(2, rows.Count);
        Assert.Equal("TrophySurtling", rows[0].TrophyId);
        Assert.True(rows[0].Recent);
    }
}
