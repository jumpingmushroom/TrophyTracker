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
    public void CounterKeyIsTheGamesHashPlusTheChanceInThousandths()
    {
        Assert.Equal(1000 + 100, PityMath.CounterKey(1000, 0.1f));
        Assert.Equal(1000 + 200, PityMath.CounterKey(1000, PityMath.EffectiveChance(0.1f, true, 1)));
        Assert.Equal(-500 + 50, PityMath.CounterKey(-500, 0.05f));
        Assert.NotEqual(PityMath.CounterKey(7, 0.1f), PityMath.CounterKey(7, 0.2f));
    }

    [Theory]
    [InlineData(0.1f, 21)]
    [InlineData(0.05f, 41)]
    [InlineData(0.2f, 11)]
    [InlineData(0.3f, 7)]
    public void ReseedMaxIsOneAboveTheSeedBound(float p, int expected)
    {
        Assert.Equal(expected, PityMath.ReseedMax(p));
    }

    [Theory]
    [InlineData(0.1f, true, false, new[] { 0, 1 })]
    [InlineData(0.05f, true, false, new[] { 0, 1, 2 })]
    [InlineData(0.1f, false, false, new[] { 0 })]
    [InlineData(0.5f, true, false, new int[0])]
    [InlineData(0.1f, true, true, new int[0])]
    public void PityTiersAreTheStarLevelsStillAtOrBelowThirtyPercent(float b, bool lm, bool noPseudo, int[] expected)
    {
        Assert.Equal(expected, PityMath.PityTiers(b, lm, noPseudo).ToArray());
    }
}
