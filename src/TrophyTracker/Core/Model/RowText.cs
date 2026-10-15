using System.Globalization;
using System.Text;

namespace TrophyTracker.Core.Model
{
    /// <summary>Symbols used in row text; Ascii when the HUD font lacks them (PLAN.md §3.1).</summary>
    public struct Glyphs
    {
        public string Star;
        public string Check;
        public string Separator;

        public static readonly Glyphs Unicode = new Glyphs { Star = "★", Check = "✓", Separator = " · " };
        public static readonly Glyphs Ascii = new Glyphs { Star = "*", Check = "+", Separator = " - " };
    }

    /// <summary>PLAN.md §3.2: the text of a row's count and chance columns.</summary>
    public static class RowText
    {
        public static string Count(RowView v, Translations tr, Glyphs g)
        {
            switch (v.State)
            {
                case RowState.Exact:
                    return MainCount(v, tr, "count_exact", "in {0} kills", "count_next", "next kill") + Extras(v, tr, g);
                case RowState.Unknown:
                    return Word(tr, "count_unknown", "within {0} kills").Replace("{0}", v.Count.ToString(CultureInfo.InvariantCulture)) + Extras(v, tr, g);
                case RowState.Roll:
                    return Word(tr, "count_roll", "random roll");
                case RowState.Always:
                    return Word(tr, "count_always", "always drops");
                case RowState.Collected:
                    return g.Check + " " + Word(tr, "collected", "collected");
                default:
                    return Word(tr, "count_none", "-");
            }
        }

        private static string MainCount(RowView v, Translations tr, string exactKey, string exactEnglish, string nextKey, string nextEnglish)
        {
            if (v.Count == 1)
                return Word(tr, nextKey, nextEnglish);
            return Word(tr, exactKey, exactEnglish).Replace("{0}", v.Count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>" · 1★ in 3 · 2★ next": each starred tier's own countdown after the 0★ count, joined with
        /// the separator glyph; "" when there are none.</summary>
        private static string Extras(RowView v, Translations tr, Glyphs g)
        {
            if (v.Extras == null || v.Extras.Count == 0)
                return "";
            var sb = new StringBuilder();
            foreach (TierCount e in v.Extras)
            {
                string stars = e.Stars.ToString(CultureInfo.InvariantCulture) + g.Star;
                string tier = e.KillsLeft == 1
                    ? Word(tr, "tier_next", "{0} next").Replace("{0}", stars)
                    : Word(tr, "tier_exact", "{0} in {1}").Replace("{0}", stars).Replace("{1}", e.KillsLeft.ToString(CultureInfo.InvariantCulture));
                sb.Append(g.Separator).Append(tier);
            }
            return sb.ToString();
        }

        /// <summary>Today's percent, or "" for no source. Used by the chance column (wrapped in the
        /// "chance" template) and by the Compendium's uncollected hint (plain, no template).</summary>
        public static string Percent(float percent)
        {
            if (percent < 0f)
                return "";
            string format = percent >= 10f ? "0" : percent >= 1f ? "0.#" : "0.##";
            return percent.ToString(format, CultureInfo.InvariantCulture) + "%";
        }

        public static string Chance(Translations tr, float percent)
        {
            string pct = Percent(percent);
            if (pct.Length == 0)
                return "";
            return Word(tr, "chance", "{0} drop").Replace("{0}", pct);
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
