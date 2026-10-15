using System;
using System.Collections.Generic;
using TrophyTracker.Core.Model;
using Xunit;

public class PitySimulationTests
{
    private const int FakeHash = 123456;

    /// <summary>The game's 1.0.20 counter loop for one trophy, PLAN.md §1.1: one counter per (trophy, effective
    /// chance). UnityEngine.Random.Range(int, int) is min-inclusive, max-exclusive, like System.Random.Next.</summary>
    private sealed class GameCounter
    {
        private readonly Random _rng;
        public readonly Dictionary<int, int> Counters = new Dictionary<int, int>();

        public GameCounter(int seed) { _rng = new Random(seed); }

        public int Remaining(float p) { return Counters[PityMath.CounterKey(FakeHash, p)]; }

        public bool Kill(float p)
        {
            int key = PityMath.CounterKey(FakeHash, p);
            int n;
            int value;
            if (Counters.TryGetValue(key, out value))
            {
                n = value - 1;
            }
            else
            {
                n = _rng.Next(0, (int)(1f / p * 2f));
                Counters[key] = n;
            }
            if (n <= 0)
            {
                Counters[key] = _rng.Next(0, (int)(1f / p * 2f) + 2);
                return true;
            }
            Counters[key] = n;
            return false;
        }
    }

    [Theory]
    [InlineData(0.02f)]
    [InlineData(0.05f)]
    [InlineData(0.1f)]
    [InlineData(0.2f)]
    [InlineData(0.3f)]
    public void PredictedKillsLeftIsExactlyTheKillThatDrops(float p)
    {
        var game = new GameCounter(1234);
        // First cycle from an unseeded counter.
        int kills = 0;
        do { kills++; } while (!game.Kill(p));
        Assert.InRange(kills, 1, PityMath.WorstCaseUnseeded(p));

        for (int cycle = 0; cycle < 10000; cycle++)
        {
            int predicted = PityMath.KillsLeft(game.Remaining(p));
            kills = 0;
            do { kills++; } while (!game.Kill(p));
            Assert.Equal(predicted, kills);
        }
    }

    [Fact]
    public void PredictionHoldsAfterEveryKillNotJustAfterDrops()
    {
        var game = new GameCounter(99);
        const float p = 0.1f;
        game.Kill(p);
        for (int i = 0; i < 10000; i++)
        {
            int predicted = PityMath.KillsLeft(game.Remaining(p));
            bool dropped = game.Kill(p);
            Assert.Equal(predicted == 1, dropped);
        }
    }

    [Fact]
    public void UnseededWorstCaseIsReachedAndNeverExceeded()
    {
        const float p = 0.1f;
        int worst = 0;
        for (int trial = 0; trial < 10000; trial++)
        {
            var game = new GameCounter(trial);
            int kills = 0;
            do { kills++; } while (!game.Kill(p));
            worst = Math.Max(worst, kills);
        }
        Assert.Equal(PityMath.WorstCaseUnseeded(p), worst);
    }

    [Fact]
    public void KillingAnotherStarTierLeavesTheBaseCountdownUnchanged()
    {
        var game = new GameCounter(7);
        float p0 = PityMath.EffectiveChance(0.1f, true, 0);
        float p1 = PityMath.EffectiveChance(0.1f, true, 1);
        for (int i = 0; i < 1000; i++)
        {
            game.Kill(p0);
            int before = game.Remaining(p0);
            game.Kill(p1);
            game.Kill(p1);
            Assert.Equal(before, game.Remaining(p0));
        }
        Assert.Equal(2, game.Counters.Count);
    }

    [Fact]
    public void AfterADropTheReseedNeverExceedsReseedMax()
    {
        const float p = 0.1f;
        var game = new GameCounter(42);
        int highest = 0;
        for (int i = 0; i < 100000; i++)
        {
            if (game.Kill(p))
            {
                Assert.InRange(game.Remaining(p), 0, PityMath.ReseedMax(p));
                highest = Math.Max(highest, game.Remaining(p));
            }
        }
        Assert.Equal(PityMath.ReseedMax(p), highest);
    }
}
