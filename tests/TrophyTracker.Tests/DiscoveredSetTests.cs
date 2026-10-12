using System.Collections.Generic;
using TrophyTracker.Core.Model;
using Xunit;

public class DiscoveredSetTests
{
    [Fact]
    public void ParsesAndSerializesSorted()
    {
        DiscoveredSet d = DiscoveredSet.Parse("Troll, Boar,,Boar");
        Assert.Equal(2, d.Count);
        Assert.Equal("Boar,Troll", d.Serialize());
        Assert.Equal(0, DiscoveredSet.Parse(null).Count);
    }

    [Fact]
    public void AddReportsWhetherItWasNew()
    {
        var d = new DiscoveredSet();
        Assert.True(d.Add("Boar"));
        Assert.False(d.Add("Boar"));
        Assert.True(d.Contains("Boar"));
    }

    [Fact]
    public void BackfillAddsKilledCreaturesAndEverySourceOfCollectedTrophies()
    {
        TrophyCatalog c = TestData.Catalog();
        var d = new DiscoveredSet();
        var kills = new Dictionary<string, float> { { "$enemy_fenring", 3f }, { "$enemy_deer", 0f } };
        var collected = new HashSet<string> { "TrophySkeleton" };

        Assert.True(d.Backfill(c, kills, collected));
        Assert.True(d.Contains("Fenring"));
        Assert.False(d.Contains("Deer"));
        Assert.True(d.Contains("Skeleton"));
        Assert.True(d.Contains("Skeleton_NoArcher"));
        Assert.False(d.Backfill(c, kills, collected));
    }

    [Fact]
    public void ATrophyIsDiscoveredWhenAnySourceIsOrItIsCollected()
    {
        TrophyCatalog c = TestData.Catalog();
        DiscoveredSet d = DiscoveredSet.Parse("Bjorn");
        var none = new HashSet<string>();
        Assert.True(d.IsTrophyDiscovered(c.Get("TrophyBjorn"), none));
        Assert.False(d.IsTrophyDiscovered(c.Get("TrophyFenring"), none));
        Assert.False(d.IsTrophyDiscovered(c.Get("TrophyOrphan"), none));
        Assert.True(d.IsTrophyDiscovered(c.Get("TrophyOrphan"), new HashSet<string> { "TrophyOrphan" }));
    }
}
