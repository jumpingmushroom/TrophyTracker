using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TrophyTracker.Core.Model;
using Xunit;

public class TranslationsTests
{
    private static string Root([CallerFilePath] string here = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));
    }

    [Fact]
    public void ParsesKeysValuesAndComments()
    {
        Translations t = Translations.Parse("# comment\na = one\n b=two \nbad line\n");
        Assert.Equal("one", t.Get("a"));
        Assert.Equal("two", t.Get("B"));
        Assert.Null(t.Get("bad line"));
    }

    [Fact]
    public void FallsBack()
    {
        Translations t = Translations.Parse("a = eins").WithFallback(Translations.Parse("a = one\nb = two"));
        Assert.Equal("eins", t.Get("a"));
        Assert.Equal("two", t.Get("b"));
    }

    [Fact]
    public void EnglishFileHasEveryKeyTheCodeUses()
    {
        Translations en = Translations.Parse(File.ReadAllText(Path.Combine(Root(), "src", "TrophyTracker", "translations", "English.txt")));
        string[] used = { "count_roll", "count_always", "count_none", "collected", "more", "uncollected", "uncollected_nochance", "pinned" };
        Assert.Empty(used.Where(k => en.Get(k) == null));
    }
}
