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
        /// <summary>The player Runtime.Discovered was loaded for. See and Backfill refuse to save unless this
        /// is still the live Player.m_localPlayer, so a stale set (from the previous character, or from
        /// before EnsurePlayer has loaded the new one) can never be written into another character's
        /// m_customData.</summary>
        private static Player _loadedFor;

        public static void Load(Player p)
        {
            Checked.Clear();
            _loadedFor = p;
            string raw;
            p.m_customData.TryGetValue(DiscoveredSet.CustomDataKey, out raw);
            Runtime.Discovered = DiscoveredSet.Parse(raw);
            Backfill();
        }

        /// <summary>Called when the local player goes away (logout, death before respawn): nothing is loaded
        /// for anyone any more, so no save can be attributed to a character until Load runs again.</summary>
        public static void Unload()
        {
            _loadedFor = null;
            Checked.Clear();
        }

        public static void Backfill()
        {
            Player p = Player.m_localPlayer;
            if (p == null || Runtime.Catalog == null || !IsLive())
                return;
            if (Runtime.Discovered.Backfill(Runtime.Catalog, Kills(), Collected()))
                Save(p);
            Runtime.Dirty = true;
        }

        public static void See(Character c)
        {
            // Catalog must be checked before Checked.Add: a creature seen before the catalog exists
            // would otherwise be marked checked forever and never get a chance to be discovered. IsLive
            // must also come before Checked.Add, for the same reason: a creature seen in the brief window
            // before EnsurePlayer finishes loading the new character's set must stay un-checked so it is
            // rechecked once Discovered is actually loaded for that character.
            if (c == null || Runtime.Catalog == null || c.IsPlayer() || !IsLive() || !Checked.Add(c.GetInstanceID()))
                return;
            string prefab = Utils.GetPrefabName(c.gameObject);
            if (!Runtime.Catalog.IsCreature(prefab) || !Runtime.Discovered.Add(prefab))
                return;
            Save(Player.m_localPlayer);
            Runtime.Dirty = true;
        }

        /// <summary>True only while Runtime.Discovered was loaded for the player who is still the live local
        /// player right now. Player's own == is overloaded so a destroyed player compares equal to null;
        /// ReferenceEquals is used here so a genuinely different (or not-yet-loaded) player is never missed.</summary>
        private static bool IsLive()
        {
            return _loadedFor != null && ReferenceEquals(Player.m_localPlayer, _loadedFor);
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
