using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace TrophyTracker
{
    /// <summary>
    /// How many kills are left until each pinned trophy drops, read from the game's own pity
    /// counter (PLAN.md §1). Reads game state and draws on the HUD; never changes drops. Client-side.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class TrophyTrackerPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.trophytracker";
        public const string PluginName = "TrophyTracker";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private static readonly HashSet<string> Warned = new HashSet<string>();
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(TrophyTrackerPlugin).Assembly);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        /// <summary>Log an exception once per key, so a broken hook can't flood the log every frame.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (Warned.Add(key))
                Log.LogWarning(key + ": " + e);
        }
    }
}
