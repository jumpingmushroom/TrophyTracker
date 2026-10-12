using System;
using TrophyTracker.Core.Model;
using Xunit;

public class PitySimulationTests
{
    /// <summary>The game's counter loop for one drop, PLAN.md §1.1. UnityEngine.Random.Range(int, int) is
    /// min-inclusive, max-exclusive, like System.Random.Next.</summary>
    private sealed class GameCounter
    {
        private readonly Random _rng;
        public CounterEntry? Entry;

        public GameCounter(int seed) { _rng = new Random(seed); }

        public bool Kill(float p)
        {
            int n;
            if (Entry.HasValue && Entry.Value.Chance == p)
            {
                n = Entry.Value.Remaining - 1;
            }
            else
            {
                n = _rng.Next(0, (int)(1f / p * 2f));
                Entry = new CounterEntry(p, n);
            }
            if (n <= 0)
            {
                Entry = new CounterEntry(p, _rng.Next(0, (int)(1f / p * 2f + 1f)));
                return true;
            }
            Entry = new CounterEntry(p, n);
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
            int predicted = PityMath.KillsLeft(game.Entry.Value.Remaining);
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
            int predicted = PityMath.KillsLeft(game.Entry.Value.Remaining);
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
    public void KillingAnotherStarTierReseedsTheCounter()
    {
        var game = new GameCounter(7);
        float p0 = PityMath.EffectiveChance(0.1f, true, 0);
        float p1 = PityMath.EffectiveChance(0.1f, true, 1);
        game.Kill(p0);
        Assert.Equal(0, PityMath.MatchTier(game.Entry.Value.Chance, 0.1f, true));
        game.Kill(p1);
        Assert.Equal(1, PityMath.MatchTier(game.Entry.Value.Chance, 0.1f, true));
    }
}
