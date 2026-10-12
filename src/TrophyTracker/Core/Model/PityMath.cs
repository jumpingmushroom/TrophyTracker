using System;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §1.1-1.2: the arithmetic of CharacterDrop.GenerateDropList, mirrored. Float expressions are
    /// written exactly as the game writes them, because the game compares the stored chance with ==.
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

        /// <summary>The star tier whose effective chance equals the stored chance exactly, or -1.</summary>
        public static int MatchTier(float stored, float baseChance, bool levelMultiplier)
        {
            if (!levelMultiplier)
                return stored == baseChance ? 0 : -1;
            for (int stars = 0; stars <= MaxStars; stars++)
            {
                if (EffectiveChance(baseChance, true, stars) == stored)
                    return stars;
            }
            return -1;
        }
    }
}
