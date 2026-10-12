using TrophyTracker.Core.Model;

/// <summary>A small catalog shaped like the real one: one creature per trophy, a shared trophy, a bear
/// with variants, a high-chance trophy, a boss, and a trophy nobody drops.</summary>
internal static class TestData
{
    public static TrophyCatalog Catalog()
    {
        var c = new TrophyCatalog();
        c.AddSource("TrophyFenring", new DropSource { CreaturePrefab = "Fenring", NameToken = "$enemy_fenring", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophySurtling", new DropSource { CreaturePrefab = "Surtling", NameToken = "$enemy_surtling", Chance = 0.05f, LevelMultiplier = true });
        c.AddSource("TrophyDeer", new DropSource { CreaturePrefab = "Deer", NameToken = "$enemy_deer", Chance = 0.5f, LevelMultiplier = true });
        c.AddSource("TrophyEikthyr", new DropSource { CreaturePrefab = "Eikthyr", NameToken = "$enemy_eikthyr", Chance = 1f, LevelMultiplier = false });
        c.AddSource("TrophySkeleton", new DropSource { CreaturePrefab = "Skeleton", NameToken = "$enemy_skeleton", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophySkeleton", new DropSource { CreaturePrefab = "Skeleton_NoArcher", NameToken = "$enemy_skeleton", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophyBjorn", new DropSource { CreaturePrefab = "Bjorn_Variant", NameToken = "$enemy_bjorn_variant", Chance = 0.1f, LevelMultiplier = true });
        c.AddSource("TrophyBjorn", new DropSource { CreaturePrefab = "Bjorn", NameToken = "$enemy_bjorn", Chance = 0.1f, LevelMultiplier = true });
        c.AddTrophy("TrophyOrphan");
        return c;
    }
}
