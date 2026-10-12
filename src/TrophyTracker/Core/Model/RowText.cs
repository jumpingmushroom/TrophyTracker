using System.Globalization;

namespace TrophyTracker.Core.Model
{
    /// <summary>Symbols used in row text; Ascii when the HUD font lacks them (PLAN.md §3.1).</summary>
    public struct Glyphs
    {
        public string Star;
        public string Check;

        public static readonly Glyphs Unicode = new Glyphs { Star = "★", Check = "✓" };
        public static readonly Glyphs Ascii = new Glyphs { Star = "*", Check = "+" };
    }

    /// <summary>PLAN.md §3.2: the text of a row's count and chance columns.</summary>
    public static class RowText
    {
        public static string Count(RowView v, Translations tr, Glyphs g)
        {
            switch (v.State)
            {
                case RowState.Exact:
                    string n = v.Count.ToString(CultureInfo.InvariantCulture);
                    return v.Stars >= 0 ? n + " " + g.Star + v.Stars.ToString(CultureInfo.InvariantCulture) : n;
                case RowState.Unknown:
                    return "? " + Word(tr, "count_max", "max") + " " + v.Count.ToString(CultureInfo.InvariantCulture);
                case RowState.Roll:
                    return Word(tr, "count_roll", "roll");
                case RowState.Always:
                    return Word(tr, "count_always", "always");
                case RowState.Collected:
                    return g.Check + " " + Word(tr, "collected", "collected");
                default:
                    return Word(tr, "count_none", "-");
            }
        }

        public static string Chance(float percent)
        {
            if (percent < 0f)
                return "";
            string format = percent >= 10f ? "0" : percent >= 1f ? "0.#" : "0.##";
            return percent.ToString(format, CultureInfo.InvariantCulture) + "%";
        }

        public static string More(int more, Translations tr)
        {
            return Word(tr, "more", "+{0} more").Replace("{0}", more.ToString(CultureInfo.InvariantCulture));
        }

        private static string Word(Translations tr, string key, string english)
        {
            return (tr != null ? tr.Get(key) : null) ?? english;
        }
    }
}
