using System;
using System.Collections.Generic;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5.2-5.3: the discovered set, kept in Player.m_customData so the game saves it with the character.</summary>
    internal static class Discovery
    {
        private static readonly Dictionary<string, float> NoKills = new Dictionary<string, float>();
        /// <summary>Character instances already checked, so EnemyHud's per-frame calls cost one HashSet lookup.</summary>
        private static readonly HashSet<int> Checked = new HashSet<int>();

        public static void Load(Player p)
        {
            Checked.Clear();
            string raw;
            p.m_customData.TryGetValue(DiscoveredSet.CustomDataKey, out raw);
            Runtime.Discovered = DiscoveredSet.Parse(raw);
            Backfill();
        }

        public static void Backfill()
        {
            Player p = Player.m_localPlayer;
            if (p == null || Runtime.Catalog == null)
                return;
            if (Runtime.Discovered.Backfill(Runtime.Catalog, Kills(), Collected()))
                Save(p);
            Runtime.Dirty = true;
        }

        public static void See(Character c)
        {
            // Catalog must be checked before Checked.Add: a creature seen before the catalog exists
            // would otherwise be marked checked forever and never get a chance to be discovered.
            if (c == null || Runtime.Catalog == null || c.IsPlayer() || !Checked.Add(c.GetInstanceID()))
                return;
            string prefab = Utils.GetPrefabName(c.gameObject);
            if (!Runtime.Catalog.IsCreature(prefab) || !Runtime.Discovered.Add(prefab))
                return;
            Save(Player.m_localPlayer);
            Runtime.Dirty = true;
        }

        public static IDictionary<string, float> Kills()
        {
            Game game = Game.instance;
            PlayerProfile profile = game != null ? game.GetPlayerProfile() : null;
            if (profile == null || profile.m_playerStats == null || profile.m_playerStats[0] == null)
                return NoKills;
            return profile.m_playerStats[0].m_enemyStats[(int)KillModifiers.MixedAndTotal] ?? NoKills;
        }

        public static HashSet<string> Collected()
        {
            Player p = Player.m_localPlayer;
            return p != null ? new HashSet<string>(p.GetTrophies(), StringComparer.Ordinal) : new HashSet<string>();
        }

        private static void Save(Player p)
        {
            if (p != null)
                p.m_customData[DiscoveredSet.CustomDataKey] = Runtime.Discovered.Serialize();
        }
    }
}
