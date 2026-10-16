using System.Collections.Generic;
using TrophyTracker.Core.Model;
using Xunit;

public class RowTextTests
{
    private static readonly Translations Tr = Translations.Parse(
        "count_exact = in {0} kills\n" +
        "count_next = next kill\n" +
        "count_unknown = within {0} kills\n" +
        "count_roll = random roll\n" +
        "count_always = always drops\n" +
        "count_none = -\n" +
        "collected = collected\n" +
        "tier_exact = {0} in {1}\n" +
        "tier_next = {0} next\n" +
        "chance = {0} drop\n" +
        "more = +{0} more");

    [Fact]
    public void FormatsEveryState()
    {
        Assert.Equal("in 14 kills", RowText.Count(new RowView { State = RowState.Exact, Count = 14 }, Tr, Glyphs.Unicode));
        Assert.Equal("in 14 kills", RowText.Count(new RowView { State = RowState.Exact, Count = 14, Extras = new List<TierCount>() }, Tr, Glyphs.Unicode));
        Assert.Equal("next kill", RowText.Count(new RowView { State = RowState.Exact, Count = 1 }, Tr, Glyphs.Unicode));
        Assert.Equal("within 39 kills", RowText.Count(new RowView { State = RowState.Unknown, Count = 39 }, Tr, Glyphs.Unicode));
        Assert.Equal("next kill", RowText.Count(new RowView { State = RowState.Unknown, Count = 1 }, Tr, Glyphs.Unicode));
        Assert.Equal("random roll", RowText.Count(new RowView { State = RowState.Roll }, Tr, Glyphs.Unicode));
        Assert.Equal("always drops", RowText.Count(new RowView { State = RowState.Always }, Tr, Glyphs.Unicode));
        Assert.Equal("-", RowText.Count(new RowView { State = RowState.NoSource }, Tr, Glyphs.Unicode));
        Assert.Equal("✓ collected", RowText.Count(new RowView { State = RowState.Collected }, Tr, Glyphs.Unicode));
    }

    [Fact]
    public void AsciiGlyphsWhenTheFontLacksTheSymbols()
    {
        Assert.Equal("within 40 kills", RowText.Count(new RowView { State = RowState.Unknown, Count = 40 }, Tr, Glyphs.Ascii));
        Assert.Equal("in 14 kills, 1* in 3", RowText.Count(new RowView { State = RowState.Exact, Count = 14, Extras = Tiers(1, 3) }, Tr, Glyphs.Ascii));
    }

    [Fact]
    public void EachGlyphFallsBackOnItsOwn()
    {
        RowView v = new RowView { State = RowState.Exact, Count = 14, Extras = Tiers(1, 3) };
        Assert.Equal("in 14 kills, 1★ in 3", RowText.Count(v, Tr, Glyphs.For(true, false)));
        Assert.Equal("in 14 kills · 1* in 3", RowText.Count(v, Tr, Glyphs.For(false, true)));
        Assert.Equal("✓ collected", RowText.Count(new RowView { State = RowState.Collected }, Tr, Glyphs.For(true, false)));
    }

    private static List<TierCount> Tiers(params int[] starsThenKills)
    {
        var list = new List<TierCount>();
        for (int k = 0; k + 1 < starsThenKills.Length; k += 2)
            list.Add(new TierCount { Stars = starsThenKills[k], KillsLeft = starsThenKills[k + 1] });
        return list;
    }

    [Fact]
    public void StarredTierCountdownsFollowTheMainCount()
    {
        Assert.Equal("in 14 kills · 1★ in 3", RowText.Count(new RowView { State = RowState.Exact, Count = 14, Extras = Tiers(1, 3) }, Tr, Glyphs.Unicode));
        Assert.Equal("in 14 kills · 1★ in 3 · 2★ next", RowText.Count(new RowView { State = RowState.Exact, Count = 14, Extras = Tiers(1, 3, 2, 1) }, Tr, Glyphs.Unicode));
        Assert.Equal("within 39 kills · 1★ in 3", RowText.Count(new RowView { State = RowState.Unknown, Count = 39, Extras = Tiers(1, 3) }, Tr, Glyphs.Unicode));
        Assert.Equal("within 39 kills, 1* in 3, 2* next", RowText.Count(new RowView { State = RowState.Unknown, Count = 39, Extras = Tiers(1, 3, 2, 1) }, Tr, Glyphs.Ascii));
    }

    [Theory]
    [InlineData(10f, "10% drop")]
    [InlineData(50f, "50% drop")]
    [InlineData(2.5f, "2.5% drop")]
    [InlineData(0.5f, "0.5% drop")]
    [InlineData(-1f, "")]
    public void FormatsChance(float percent, string expected)
    {
        Assert.Equal(expected, RowText.Chance(Tr, percent));
    }

    [Theory]
    [InlineData(10f, "10%")]
    [InlineData(50f, "50%")]
    [InlineData(2.5f, "2.5%")]
    [InlineData(0.5f, "0.5%")]
    [InlineData(-1f, "")]
    public void FormatsPercent(float percent, string expected)
    {
        Assert.Equal(expected, RowText.Percent(percent));
    }

    [Fact]
    public void FormatsMore()
    {
        Assert.Equal("+3 more", RowText.More(3, Tr));
    }

    [Fact]
    public void FallsBackToEnglishWordsWithoutTranslations()
    {
        Assert.Equal("random roll", RowText.Count(new RowView { State = RowState.Roll }, new Translations(), Glyphs.Unicode));
    }
}
