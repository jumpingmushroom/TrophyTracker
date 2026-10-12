using System.Linq;
using TrophyTracker.Core.Model;
using Xunit;

public class TrophyCatalogTests
{
    [Fact]
    public void KeepsEveryTrophyInInsertionOrderIncludingOnesWithNoSource()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "TrophyFenring", "TrophySurtling", "TrophyDeer", "TrophyEikthyr", "TrophySkeleton", "TrophyBjorn", "TrophyOrphan" },
            c.All.Select(t => t.Id).ToArray());
        Assert.Empty(c.Get("TrophyOrphan").Sources);
    }

    [Fact]
    public void MapsAllCreaturesThatDropATrophyNotJustTheFirst()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "Bjorn_Variant", "Bjorn" }, c.Get("TrophyBjorn").Sources.Select(s => s.CreaturePrefab).ToArray());
    }

    [Fact]
    public void IgnoresTheSameCreatureTwice()
    {
        var c = new TrophyCatalog();
        c.AddSource("TrophyBoar", new DropSource { CreaturePrefab = "Boar", NameToken = "$enemy_boar", Chance = 0.15f });
        c.AddSource("TrophyBoar", new DropSource { CreaturePrefab = "Boar", NameToken = "$enemy_boar", Chance = 0.15f });
        Assert.Single(c.Get("TrophyBoar").Sources);
    }

    [Fact]
    public void LooksUpByTokenAndByCreature()
    {
        TrophyCatalog c = TestData.Catalog();
        Assert.Equal(new[] { "TrophySkeleton" }, c.TrophiesForToken("$enemy_skeleton").ToArray());
        Assert.Equal(new[] { "TrophyBjorn" }, c.TrophiesForCreature("Bjorn").ToArray());
        Assert.Empty(c.TrophiesForToken("$enemy_nobody"));
        Assert.True(c.IsCreature("Skeleton_NoArcher"));
        Assert.False(c.IsCreature("Greyling"));
        Assert.Null(c.Get("TrophyMissing"));
    }
}
