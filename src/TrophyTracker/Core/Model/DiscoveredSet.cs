using System;
using System.Collections.Generic;
using System.Linq;

namespace TrophyTracker.Core.Model
{
    /// <summary>
    /// PLAN.md §1.7, §5.2: creature prefabs this character has met (health bar seen, killed, or trophy
    /// collected), saved in Player.m_customData. Nothing undiscovered is shown anywhere.
    /// </summary>
    public sealed class DiscoveredSet
    {
        public const string CustomDataKey = "TrophyTracker.Discovered";

        private readonly HashSet<string> _set = new HashSet<string>(StringComparer.Ordinal);

        public int Count { get { return _set.Count; } }

        public IEnumerable<string> Items { get { return _set; } }

        public bool Contains(string prefab)
        {
            return prefab != null && _set.Contains(prefab);
        }

        public bool Add(string prefab)
        {
            return !string.IsNullOrEmpty(prefab) && _set.Add(prefab);
        }

        public static DiscoveredSet Parse(string text)
        {
            var d = new DiscoveredSet();
            foreach (string raw in (text ?? "").Split(','))
                d.Add(raw.Trim());
            return d;
        }

        public string Serialize()
        {
            return string.Join(",", _set.OrderBy(s => s, StringComparer.Ordinal));
        }

        /// <summary>Adds every source creature with kills, and every source of a collected trophy. True if anything was added.</summary>
        public bool Backfill(TrophyCatalog catalog, IDictionary<string, float> kills, ICollection<string> collected)
        {
            bool changed = false;
            foreach (TrophyInfo trophy in catalog.All)
            {
                bool owned = collected != null && collected.Contains(trophy.Id);
                foreach (DropSource s in trophy.Sources)
                {
                    float k;
                    bool killed = kills != null && s.NameToken != null && kills.TryGetValue(s.NameToken, out k) && k > 0f;
                    if ((owned || killed) && Add(s.CreaturePrefab))
                        changed = true;
                }
            }
            return changed;
        }

        public bool IsTrophyDiscovered(TrophyInfo trophy, ICollection<string> collected)
        {
            if (trophy == null)
                return false;
            if (collected != null && collected.Contains(trophy.Id))
                return true;
            foreach (DropSource s in trophy.Sources)
            {
                if (_set.Contains(s.CreaturePrefab))
                    return true;
            }
            return false;
        }
    }
}
