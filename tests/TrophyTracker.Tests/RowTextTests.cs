using TrophyTracker.Core.Model;
using Xunit;

public class RowTextTests
{
    private static readonly Translations Tr = Translations.Parse("count_roll = roll\ncount_always = always\ncount_none = -\ncollected = collected\nmore = +{0} more");

    [Fact]
    public void FormatsEveryState()
    {
        Assert.Equal("7 ★0", RowText.Count(new RowView { State = RowState.Exact, Count = 7, Stars = 0 }, Tr, Glyphs.Unicode));
        Assert.Equal("7", RowText.Count(new RowView { State = RowState.Exact, Count = 7, Stars = -1 }, Tr, Glyphs.Unicode));
        Assert.Equal("?  ≤20", RowText.Count(new RowView { State = RowState.Unknown, Count = 20 }, Tr, Glyphs.Unicode));
        Assert.Equal("roll", RowText.Count(new RowView { State = RowState.Roll }, Tr, Glyphs.Unicode));
        Assert.Equal("always", RowText.Count(new RowView { State = RowState.Always }, Tr, Glyphs.Unicode));
        Assert.Equal("-", RowText.Count(new RowView { State = RowState.NoSource }, Tr, Glyphs.Unicode));
        Assert.Equal("✓ collected", RowText.Count(new RowView { State = RowState.Collected }, Tr, Glyphs.Unicode));
    }

    [Fact]
    public void AsciiGlyphsWhenTheFontLacksTheSymbols()
    {
        Assert.Equal("3 *1", RowText.Count(new RowView { State = RowState.Exact, Count = 3, Stars = 1 }, Tr, Glyphs.Ascii));
        Assert.Equal("?  <=40", RowText.Count(new RowView { State = RowState.Unknown, Count = 40 }, Tr, Glyphs.Ascii));
    }

    [Theory]
    [InlineData(10f, "10%")]
    [InlineData(50f, "50%")]
    [InlineData(2.5f, "2.5%")]
    [InlineData(0.5f, "0.5%")]
    [InlineData(-1f, "")]
    public void FormatsChance(float percent, string expected)
    {
        Assert.Equal(expected, RowText.Chance(percent));
    }

    [Fact]
    public void FormatsMore()
    {
        Assert.Equal("+3 more", RowText.More(3, Tr));
    }

    [Fact]
    public void FallsBackToEnglishWordsWithoutTranslations()
    {
        Assert.Equal("roll", RowText.Count(new RowView { State = RowState.Roll }, new Translations(), Glyphs.Unicode));
    }
}
