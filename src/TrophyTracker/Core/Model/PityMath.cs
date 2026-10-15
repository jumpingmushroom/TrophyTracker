using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §1.1-1.2: the arithmetic of CharacterDrop.GenerateDropList, mirrored. Float expressions are
    /// written exactly as the game writes them, because the effective chance is truncated into the counter key.
    /// </summary>
    public static class PityMath
    {
        public const float PityThreshold = 0.3f;
        public const int MaxStars = 10;

        /// <summary>The game: chance × max(1, (int)2^(level-1)) when the drop has a level multiplier. stars = level - 1.</summary>
        public static float EffectiveChance(float baseChance, bool levelMultiplier, int stars)
        {
            if (!levelMultiplier)
                return baseChance;
            int multiplier = Math.Max(1, (int)Math.Pow(2.0, stars));
            return baseChance * (float)multiplier;
        }

        /// <summary>Pity applies at or below 30% unless the world has the NoPseudoDrops modifier.</summary>
        public static bool UsesPity(float effectiveChance, bool noPseudoDrops)
        {
            return effectiveChance > 0f && effectiveChance <= PityThreshold && !noPseudoDrops;
        }

        /// <summary>Exclusive upper bound of the first seed: Random.Range(0, (int)(1/p × 2)).</summary>
        /// <remarks>In game (Mono) this evaluates to 19 for p = 0.1f, not 20: Mono evaluates the float
        /// expression at higher precision, and 1/0.1f is a hair under 10 (PLAN.md §1.2).</remarks>
        public static int SeedBound(float p)
        {
            return (int)(1f / p * 2f);
        }

        /// <summary>From an unseeded counter the drop lands on kill 1..SeedBound: seed 0 drops at once, seed s ≥ 1 drops on kill s + 1.</summary>
        public static int WorstCaseUnseeded(float p)
        {
            return Math.Max(1, SeedBound(p));
        }

        /// <summary>With a stored count n the k-th kill from now drops when n - k ≤ 0.</summary>
        public static int KillsLeft(int remaining)
        {
            return Math.Max(1, remaining);
        }

        /// <summary>From a stored count n the drop lands on kill max(1, n). Right after a drop the game reseeds
        /// Random.Range(0, (int)(1/p × 2) + 2): at most SeedBound + 1 kills to the next one (PLAN.md §1.2).</summary>
        public static int ReseedMax(float p)
        {
            return SeedBound(p) + 1;
        }

        /// <summary>The s_pseudoCounter key: the trophy prefab name's GetStableHashCode plus the effective chance in
        /// thousandths, one counter per (trophy, star tier). Written exactly as the game writes it: the float
        /// product is truncated by the same JIT the game runs under, so the key matches the game's bit for bit.
        /// Unchecked int addition, like the game.</summary>
        public static int CounterKey(int trophyHash, float effectiveChance)
        {
            return trophyHash + (int)(effectiveChance * 1000f);
        }

        /// <summary>The star tiers 0..MaxStars whose effective chance still uses pity (each has its own counter).
        /// Without a level multiplier every star shares the 0★ chance, so only tier 0. Empty when none uses pity.</summary>
        public static List<int> PityTiers(float baseChance, bool levelMultiplier, bool noPseudoDrops)
        {
            var tiers = new List<int>();
            int top = levelMultiplier ? MaxStars : 0;
            for (int stars = 0; stars <= top; stars++)
            {
                if (UsesPity(EffectiveChance(baseChance, levelMultiplier, stars), noPseudoDrops))
                    tiers.Add(stars);
            }
            return tiers;
        }
    }
}
