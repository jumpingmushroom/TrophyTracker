using System;
using HarmonyLib;
using TrophyTracker.Core;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.4, §5.5: runs on your client for every creature you damaged, owner or not.</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.RPC_RegisterKill))]
    internal static class KillPatch
    {
        private static void Postfix(string enemyName)
        {
            try
            {
                Runtime.OnKill(enemyName);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("Game.RPC_RegisterKill", e);
            }
        }
    }
}
