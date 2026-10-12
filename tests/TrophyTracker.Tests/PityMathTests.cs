using TrophyTracker.Core.Model;
using Xunit;

public class PityMathTests
{
    [Theory]
    [InlineData(0.1f, true, 0, 0.1f)]
    [InlineData(0.1f, true, 1, 0.2f)]
    [InlineData(0.1f, true, 2, 0.4f)]
    [InlineData(0.1f, false, 2, 0.1f)]
    public void EffectiveChanceDoublesPerStarWithLevelMultiplier(float b, bool lm, int stars, float expected)
    {
        Assert.Equal(expected, PityMath.EffectiveChance(b, lm, stars));
    }

    [Theory]
    [InlineData(0.3f, false, true)]
    [InlineData(0.31f, false, false)]
    [InlineData(0.1f, true, false)]
    [InlineData(0f, false, false)]
    public void PityAppliesAtOrBelowThirtyPercentUnlessWorldDisablesIt(float p, bool noPseudo, bool expected)
    {
        Assert.Equal(expected, PityMath.UsesPity(p, noPseudo));
    }

    [Theory]
    [InlineData(0.1f, 20)]
    [InlineData(0.05f, 40)]
    [InlineData(0.2f, 10)]
    [InlineData(0.3f, 6)]
    public void WorstCaseFromAnUnseededCounterIsTheSeedBound(float p, int expected)
    {
        Assert.Equal(expected, PityMath.WorstCaseUnseeded(p));
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    public void KillsLeftIsAtLeastOne(int remaining, int expected)
    {
        Assert.Equal(expected, PityMath.KillsLeft(remaining));
    }

    [Fact]
    public void MatchTierFindsTheStarTierFromTheStoredChance()
    {
        Assert.Equal(0, PityMath.MatchTier(0.1f, 0.1f, true));
        Assert.Equal(1, PityMath.MatchTier(0.1f * 2f, 0.1f, true));
        Assert.Equal(-1, PityMath.MatchTier(0.15f, 0.1f, true));
        Assert.Equal(0, PityMath.MatchTier(0.1f, 0.1f, false));
        Assert.Equal(-1, PityMath.MatchTier(0.2f, 0.1f, false));
    }
}
