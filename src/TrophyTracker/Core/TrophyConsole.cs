using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using TrophyTracker.Core.Model;
using TrophyTracker.UI;

namespace TrophyTracker.Core
{
    /// <summary>"trophytracker": the catalog with every source, live counters with decoded tier, the discovered
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
            List<string> names = UiUtil.AllFontNames().ToList();
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
            Dictionary<string, CounterEntry> counters = CounterReader.Snapshot();
            HashSet<string> collected = Discovery.Collected();
            Say(ctx, "TrophyTracker: " + cat.All.Count + " trophies, " + Runtime.Discovered.Count + " creatures discovered, " +
                cat.SkippedEntries + " broken prefabs skipped, NoPseudoDrops=" + CounterReader.NoPseudoDrops());
            foreach (TrophyInfo t in cat.All)
            {
                string sources = t.Sources.Count == 0 ? "(no creature drops it)" :
                    string.Join(", ", t.Sources.Select(s => s.CreaturePrefab + " " + s.NameToken + " " + Pct(s.Chance) +
                        (Runtime.Discovered.Contains(s.CreaturePrefab) ? " seen" : "")));
                string counter = "";
                CounterEntry c;
                if (counters.TryGetValue(t.Id, out c))
                {
                    int tier = t.Sources.Count > 0 ? PityMath.MatchTier(c.Chance, t.Sources[0].Chance, t.Sources[0].LevelMultiplier) : -1;
                    counter = "  counter " + Pct(c.Chance) + " n=" + c.Remaining + (tier >= 0 ? " tier " + tier : " tier ?");
                }
                Say(ctx, "  " + t.Id + (collected.Contains(t.Id) ? " [owned]" : "") + ": " + sources + counter);
            }
            List<string> unknown = PinList.Parse(PluginConfig.Pinned.Value).Where(id => cat.Get(id) == null).ToList();
            if (unknown.Count > 0)
                Say(ctx, "Unknown pins (kept in config, not shown): " + string.Join(", ", unknown));
        }
    }
}
