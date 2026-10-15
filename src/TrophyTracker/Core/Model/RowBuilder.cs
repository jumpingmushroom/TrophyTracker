using System;
using System.Collections.Generic;

namespace TrophyTracker.Core.Model
{
    /// <summary>PLAN.md §3: which rows, in which order, in which state.</summary>
    public static class RowBuilder
    {
        public static RowList Build(RowInputs i)
        {
            var result = new RowList();
            var ordered = new List<RowView>();
            var pinned = new HashSet<string>(i.Pins ?? new List<string>(), StringComparer.Ordinal);

            string recent = RecentTrophy(i, pinned);
            if (recent != null)
                ordered.Add(Row(i, i.Catalog.Get(recent), true));

            if (i.Pins != null)
            {
                foreach (string id in i.Pins)
                {
                    TrophyInfo t = i.Catalog.Get(id);
                    if (t == null || !i.Discovered.IsTrophyDiscovered(t, i.Collected))
                        continue;
                    ordered.Add(Row(i, t, false));
                }
            }

            int max = Math.Max(1, i.MaxRows);
            for (int k = 0; k < ordered.Count && k < max; k++)
                result.Rows.Add(ordered[k]);
            result.More = Math.Max(0, ordered.Count - max);
            return result;
        }

        /// <summary>The source whose name a row shows: most kills; on a tie a discovered one; then catalog order.</summary>
        public static DropSource PrimarySource(TrophyInfo t, IDictionary<string, float> kills, DiscoveredSet discovered)
        {
            DropSource best = null;
            float bestKills = -1f;
            foreach (DropSource s in t.Sources)
            {
                float k = 0f;
                if (kills != null && s.NameToken != null)
                    kills.TryGetValue(s.NameToken, out k);
                bool better = k > bestKills ||
                    (k == bestKills && discovered != null && discovered.Contains(s.CreaturePrefab) && !discovered.Contains(best.CreaturePrefab));
                if (better)
                {
                    best = s;
                    bestKills = k;
                }
            }
            return best;
        }

        private static string RecentTrophy(RowInputs i, HashSet<string> pinned)
        {
            if (!i.ShowRecent || i.RecentToken == null || (i.RecentSeconds > 0f && i.Now - i.RecentTime > i.RecentSeconds))
                return null;
            foreach (string id in i.Catalog.TrophiesForToken(i.RecentToken))
            {
                if (pinned.Contains(id))
                    continue;
                if (!i.Discovered.IsTrophyDiscovered(i.Catalog.Get(id), i.Collected))
                    continue;
                return id;
            }
            return null;
        }

        private static bool Flashing(RowInputs i, string id)
        {
            float until;
            return i.FlashUntil != null && i.FlashUntil.TryGetValue(id, out until) && i.Now < until;
        }

        private static RowView Row(RowInputs i, TrophyInfo t, bool recent)
        {
            var v = new RowView { TrophyId = t.Id, ChancePercent = -1f, BarFill = -1f, Recent = recent };
            DropSource s = PrimarySource(t, i.Kills, i.Discovered);
            if (s != null)
            {
                v.NameToken = s.NameToken;
                v.ChancePercent = s.Chance * 100f;
            }

            if (Flashing(i, t.Id))
            {
                v.State = RowState.Collected;
                return v;
            }
            if (s == null)
            {
                v.State = RowState.NoSource;
                return v;
            }
            if (s.Chance >= 1f)
            {
                v.State = RowState.Always;
                return v;
            }
            if (!PityMath.UsesPity(s.Chance, i.NoPseudoDrops))
            {
                v.State = RowState.Roll;
                return v;
            }

            if (i.Hash == null || i.Counters == null)
            {
                v.State = RowState.Unknown;
                v.Count = PityMath.WorstCaseUnseeded(s.Chance);
                return v;
            }
            int key0 = PityMath.CounterKey(i.Hash(t.Id), PityMath.EffectiveChance(s.Chance, s.LevelMultiplier, 0));
            int c;
            if (i.Counters.TryGetValue(key0, out c))
            {
                v.State = RowState.Exact;
                v.Count = PityMath.KillsLeft(c);
                v.BarFill = i.Bars != null ? i.Bars.Fill(key0, c) : -1f;
                v.Dim = i.Dim;
            }
            else
            {
                v.State = RowState.Unknown;
                v.Count = PityMath.WorstCaseUnseeded(s.Chance);
            }
            v.Extras = Extras(i, t, s);
            return v;
        }

        /// <summary>Starred tiers that still use pity and have a counter of their own (PLAN.md §1.1, §3.2).</summary>
        private static List<TierCount> Extras(RowInputs i, TrophyInfo t, DropSource s)
        {
            if (i.Counters.Count == 0 || !s.LevelMultiplier)
                return null;
            List<TierCount> extras = null;
            int hash = i.Hash(t.Id);
            foreach (int stars in PityMath.PityTiers(s.Chance, s.LevelMultiplier, i.NoPseudoDrops))
            {
                if (stars == 0)
                    continue;
                int c;
                if (!i.Counters.TryGetValue(PityMath.CounterKey(hash, PityMath.EffectiveChance(s.Chance, true, stars)), out c))
                    continue;
                if (extras == null)
                    extras = new List<TierCount>();
                extras.Add(new TierCount { Stars = stars, KillsLeft = PityMath.KillsLeft(c) });
            }
            return extras;
        }
    }
}
