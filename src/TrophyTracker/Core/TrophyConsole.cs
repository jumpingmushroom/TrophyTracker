using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using TrophyTracker.Core.Model;
using TrophyTracker.UI;

namespace TrophyTracker.Core
{
    /// <summary>"trophytracker": the catalog with every source, saved counters per star tier, the discovered
    /// set and unknown pins (PLAN.md §7). "trophytracker fonts": every loaded TMP font and which one the panel
    /// uses now. Mirrored to the BepInEx log.</summary>
    internal static class TrophyConsole
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("trophytracker", "TrophyTracker: trophies, their sources, live counters, discovered creatures (fonts: every loaded font and the one in use)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    try
                    {
                        string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                        if (sub == "fonts")
                            Fonts(args.Context);
                        else
                            Dump(args.Context);
                    }
                    catch (Exception e)
                    {
                        TrophyTrackerPlugin.WarnOnce("trophytracker console", e);
                    }
                });
        }

        private static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            TrophyTrackerPlugin.Log.LogInfo(line);
        }

        private static string Pct(float chance)
        {
            return (chance * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        private static void Fonts(Terminal ctx)
        {
            // Rescans (unlike the panel, which scans once at Build) so a font loaded since shows up.
            List<string> names = UiUtil.ScanFonts().ToList();
            TMP_FontAsset current = UiUtil.ResolveFont();
            string currentName = current != null ? current.name : "(none)";
            Say(ctx, "TrophyTracker: " + names.Count + " fonts loaded, panel uses " + currentName + ".");
            foreach (string n in names)
                Say(ctx, "  " + n);
        }

        private static void Dump(Terminal ctx)
        {
            TrophyCatalog cat = Runtime.Catalog;
            if (cat == null)
            {
                Say(ctx, "TrophyTracker: no catalog yet (load a world).");
                return;
            }
            Dictionary<int, int> counters = CounterReader.Snapshot();
            bool noPseudo = CounterReader.NoPseudoDrops();
            IDictionary<string, float> kills = Discovery.Kills();
            HashSet<string> collected = Discovery.Collected();
            Say(ctx, "TrophyTracker: " + cat.All.Count + " trophies, " + Runtime.Discovered.Count + " creatures discovered, " +
                cat.SkippedEntries + " broken prefabs skipped, NoPseudoDrops=" + noPseudo + ", " + counters.Count + " counters saved");
            foreach (TrophyInfo t in cat.All)
            {
                string sources = t.Sources.Count == 0 ? "(no creature drops it)" :
                    string.Join(", ", t.Sources.Select(s => s.CreaturePrefab + " " + s.NameToken + " " + Pct(s.Chance) +
                        (s.LevelMultiplier ? " lm" : "") + (s.LevelMultiplierNotAmount ? " notAmount" : "") +
                        (Runtime.Discovered.Contains(s.CreaturePrefab) ? " seen" : "")));
                Say(ctx, "  " + t.Id + (collected.Contains(t.Id) ? " [owned]" : "") + ": " + sources + Tiers(t, kills, counters, noPseudo));
            }
            List<string> unknown = PinList.Parse(PluginConfig.Pinned.Value).Where(id => cat.Get(id) == null).ToList();
            if (unknown.Count > 0)
                Say(ctx, "Unknown pins (kept in config, not shown): " + string.Join(", ", unknown));
        }

        /// <summary>"  ★0 n=7  ★1 -": every pity tier of the primary source with its saved counter, or "-".</summary>
        private static string Tiers(TrophyInfo t, IDictionary<string, float> kills, Dictionary<int, int> counters, bool noPseudo)
        {
            DropSource s = RowBuilder.PrimarySource(t, kills, Runtime.Discovered);
            if (s == null)
                return "";
            int hash = t.Id.GetStableHashCode();
            var sb = new StringBuilder();
            foreach (int stars in PityMath.PityTiers(s.Chance, s.LevelMultiplier, noPseudo))
            {
                int key = PityMath.CounterKey(hash, PityMath.EffectiveChance(s.Chance, s.LevelMultiplier, stars));
                int n;
                sb.Append("  ★").Append(stars.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(counters.TryGetValue(key, out n) ? "n=" + n.ToString(CultureInfo.InvariantCulture) : "-");
            }
            return sb.ToString();
        }
    }
}
