using System;
using HarmonyLib;
using TrophyTracker.Core;
using UnityEngine;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.5, §5.6: a trophy entering the Compendium for the first time starts the collected flash.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AddTrophy))]
    internal static class TrophyPatch
    {
        private static void Prefix(Player __instance, out int __state)
        {
            __state = 0;
            try
            {
                __state = __instance.m_trophies.Count;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("Player.AddTrophy prefix", e);
            }
        }

        private static void Postfix(Player __instance, ItemDrop.ItemData item, int __state)
        {
            try
            {
                if (__instance != Player.m_localPlayer || __instance.m_trophies.Count == __state)
                    return;
                GameObject prefab = item.m_dropPrefab != null ? item.m_dropPrefab : ObjectDB.instance.GetItemPrefab(item.m_shared);
                if (prefab != null)
                    Runtime.OnTrophyCollected(prefab.name);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("Player.AddTrophy", e);
            }
        }
    }
}
