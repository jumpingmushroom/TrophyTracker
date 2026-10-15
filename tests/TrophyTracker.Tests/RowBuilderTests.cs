using System;
using System.Collections.Generic;
using System.Linq;
using TrophyTracker.Core.Model;
using Xunit;

public class RowBuilderTests
{
    /// <summary>Deterministic stand-in for GetStableHashCode; distinct for every trophy in TestData and
    /// spaced so chance offsets (≤ 1000) never collide.</summary>
    private static readonly Func<string, int> FakeHash = s => (s.Length * 31 + s[6]) * 10000;

    private static int Key(string trophy, float baseChance, int stars)
    {
        return PityMath.CounterKey(FakeHash(trophy), PityMath.EffectiveChance(baseChance, true, stars));
    }

    private static RowInputs Inputs(params string[] pins)
    {
        return new RowInputs
        {
            Catalog = TestData.Catalog(),
            Pins = pins.ToList(),
            Discovered = DiscoveredSet.Parse("Fenring,Surtling,Deer,Eikthyr,Skeleton,Bjorn"),
            Collected = new HashSet<string>(),
            Counters = new Dictionary<int, int>(),
            Hash = FakeHash,
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
    public void ExactFromTheBaseTierCounter()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters[Key("TrophyFenring", 0.1f, 0)] = 7;
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(7, v.Count);
        Assert.Equal(0f, v.BarFill);
        Assert.True(v.Extras == null || v.Extras.Count == 0);
    }

    [Fact]
    public void ExactAtZeroMeansTheNextKillDrops()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters[Key("TrophyFenring", 0.1f, 0)] = 0;
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(1, v.Count);
    }

    [Fact]
    public void AStarredCounterAloneLeavesTheBaseTierUnknownWithAnExtra()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Counters[Key("TrophyFenring", 0.1f, 1)] = 3;
        RowView v = Only(i);
        Assert.Equal(RowState.Unknown, v.State);
        Assert.Equal(20, v.Count);
        TierCount extra = Assert.Single(v.Extras);
        Assert.Equal(1, extra.Stars);
        Assert.Equal(3, extra.KillsLeft);
    }

    [Fact]
    public void StarredTiersWithACounterFollowTheBaseCount()
    {
        RowInputs i = Inputs("TrophySurtling");
        i.Counters[Key("TrophySurtling", 0.05f, 0)] = 7;
        i.Counters[Key("TrophySurtling", 0.05f, 2)] = 0;
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(7, v.Count);
        TierCount extra = Assert.Single(v.Extras);
        Assert.Equal(2, extra.Stars);
        Assert.Equal(1, extra.KillsLeft);

        i.Counters[Key("TrophySurtling", 0.05f, 1)] = 4;
        v = Only(i);
        Assert.Equal(new[] { 1, 2 }, v.Extras.Select(e => e.Stars).ToArray());
        Assert.Equal(new[] { 4, 1 }, v.Extras.Select(e => e.KillsLeft).ToArray());
    }

    [Fact]
    public void TiersAboveThirtyPercentAreNeverListed()
    {
        RowInputs i = Inputs("TrophyFenring");
        // 2★ Fenring is 40%: a plain roll, even if a stale counter exists for that key.
        i.Counters[Key("TrophyFenring", 0.1f, 2)] = 5;
        RowView v = Only(i);
        Assert.Equal(RowState.Unknown, v.State);
        Assert.True(v.Extras == null || v.Extras.Count == 0);
    }

    [Fact]
    public void ADropWithoutLevelMultiplierHasOnlyTheBaseTier()
    {
        var c = new TrophyCatalog();
        c.AddSource("TrophyFlat", new DropSource { CreaturePrefab = "Flat", NameToken = "$enemy_flat", Chance = 0.1f, LevelMultiplier = false });
        RowInputs i = Inputs("TrophyFlat");
        i.Catalog = c;
        i.Discovered = DiscoveredSet.Parse("Flat");
        i.Counters[PityMath.CounterKey(FakeHash("TrophyFlat"), 0.1f)] = 6;
        i.Counters[PityMath.CounterKey(FakeHash("TrophyFlat"), 0.2f)] = 2;
        RowView v = Only(i);
        Assert.Equal(RowState.Exact, v.State);
        Assert.Equal(6, v.Count);
        Assert.True(v.Extras == null || v.Extras.Count == 0);
    }

    [Fact]
    public void RollRowsHaveNoExtras()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.NoPseudoDrops = true;
        i.Counters[Key("TrophyFenring", 0.1f, 1)] = 3;
        RowView v = Only(i);
        Assert.Equal(RowState.Roll, v.State);
        Assert.True(v.Extras == null || v.Extras.Count == 0);
    }

    [Fact]
    public void ExactIsDimmedInMultiplayer()
    {
        RowInputs i = Inputs("TrophyFenring");
        i.Dim = true;
        i.Counters[Key("TrophyFenring", 0.1f, 0)] = 3;
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
        i.Counters[Key("TrophyFenring", 0.1f, 0)] = 3;
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
