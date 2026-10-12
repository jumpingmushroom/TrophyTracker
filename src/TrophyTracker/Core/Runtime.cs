using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5: shared state, the 1 s tick, catalog and player lifecycle, collected flashes.</summary>
    internal static class Runtime
    {
        public const float FlashSeconds = 5f;

        public static TrophyCatalog Catalog;
        public static DiscoveredSet Discovered = new DiscoveredSet();
        public static readonly BarTracker Bars = new BarTracker();
        public static readonly Dictionary<string, float> FlashUntil = new Dictionary<string, float>(StringComparer.Ordinal);
        public static Translations Tr = new Translations();
        public static bool ToggledOn = true;
        public static bool Dirty = true;

        private static string _recentToken;
        private static float _recentTime = float.NegativeInfinity;
        private static ZNetScene _catalogScene;
        private static Player _player;
        private static float _nextTick;
        private static bool _initialised;
        private static readonly List<string> Expired = new List<string>();

        public static void Tick()
        {
            if (!_initialised)
                TryInit();
            EnsurePlayer();
            EnsureCatalog();

            if (PluginConfig.Enabled.Value && Keys.Down(PluginConfig.ToggleKey.Value) && !Keys.InputBlocked())
            {
                ToggledOn = !ToggledOn;
                Dirty = true;
            }

            ExpireFlashes();

            if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + 1f;
                Dirty = true;
            }
        }

        public static void OnKill(string nameToken)
        {
            _recentToken = nameToken;
            _recentTime = Time.time;
            Dirty = true;
        }

        public static void OnTrophyCollected(string id)
        {
            FlashUntil[id] = Time.time + FlashSeconds;
            Dirty = true;
        }

        public static RowList BuildRows()
        {
            var inputs = new RowInputs
            {
                Catalog = Catalog,
                Pins = PinList.Parse(PluginConfig.Pinned.Value),
                Discovered = Discovered,
                Collected = Discovery.Collected(),
                Counters = CounterReader.Snapshot(),
                Kills = Discovery.Kills(),
                Bars = Bars,
                FlashUntil = FlashUntil,
                RecentToken = _recentToken,
                RecentTime = _recentTime,
                RecentSeconds = PluginConfig.RecentKillSeconds.Value,
                ShowRecent = PluginConfig.ShowRecentKill.Value,
                Now = Time.time,
                NoPseudoDrops = CounterReader.NoPseudoDrops(),
                Dim = PluginConfig.DimInMultiplayer.Value && ZNet.instance != null && ZNet.instance.GetNrOfPlayers() > 1,
                MaxRows = PluginConfig.MaxRows.Value,
            };
            return RowBuilder.Build(inputs);
        }

        private static void EnsureCatalog()
        {
            ZNetScene scene = ZNetScene.instance;
            ObjectDB db = ObjectDB.instance;
            if (scene == null || db == null || db.m_items.Count == 0 || scene == _catalogScene)
                return;
            _catalogScene = scene;
            Catalog = CatalogBuilder.Build(db, scene);
            TrophyTrackerPlugin.Log.LogInfo("Catalog: " + Catalog.All.Count + " trophies" +
                (Catalog.SkippedEntries > 0 ? ", " + Catalog.SkippedEntries + " broken prefabs skipped" : ""));
            Discovery.Backfill();
        }

        private static void EnsurePlayer()
        {
            Player p = Player.m_localPlayer;
            // ReferenceEquals, not Player's own ==: Unity overloads == so a destroyed player compares
            // equal to null, which would hide a logout (p turns null, _player is still the old,
            // destroyed-but-not-reference-null instance) and leave the previous character's Discovered
            // set in place for whoever logs in next.
            if (ReferenceEquals(p, _player))
                return;
            _player = p;
            FlashUntil.Clear();
            _recentToken = null;
            if (p == null)
            {
                // No live player: nothing loaded, so Discovered must not be left holding the departed
                // character's set for a new login (or a stray Backfill/See) to stamp over.
                Discovered = new DiscoveredSet();
                Discovery.Unload();
            }
            else
            {
                Discovery.Load(p);
            }
            Dirty = true;
        }

        private static void ExpireFlashes()
        {
            if (FlashUntil.Count == 0)
                return;
            Expired.Clear();
            foreach (KeyValuePair<string, float> kv in FlashUntil)
            {
                if (Time.time >= kv.Value)
                    Expired.Add(kv.Key);
            }
            foreach (string id in Expired)
            {
                FlashUntil.Remove(id);
                if (PluginConfig.UnpinWhenCollected.Value && PinList.Contains(PluginConfig.Pinned.Value, id))
                    PluginConfig.Pinned.Value = PinList.Remove(PluginConfig.Pinned.Value, id);
                Dirty = true;
            }
        }

        /// <summary>Localization initialises on first access; the language is fixed for the session.</summary>
        private static void TryInit()
        {
            if (Localization.instance == null)
                return;
            _initialised = true;
            Translations english = Translations.Parse(ReadResource("TrophyTracker.English.txt"));
            string language = Localization.instance.GetSelectedLanguage();
            string custom = Path.Combine(Path.Combine(Path.Combine(Paths.ConfigPath, "TrophyTracker"), "translations"), language + ".txt");
            Tr = File.Exists(custom) ? Translations.Parse(File.ReadAllText(custom)).WithFallback(english) : english;
        }

        private static string ReadResource(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    TrophyTrackerPlugin.Log.LogError("Missing embedded resource " + name);
                    return "";
                }
                using (var r = new StreamReader(s))
                    return r.ReadToEnd();
            }
        }
    }
}
