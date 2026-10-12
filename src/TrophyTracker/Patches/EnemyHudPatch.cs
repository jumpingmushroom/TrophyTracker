using System;
using HarmonyLib;
using TrophyTracker.Core;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §1.7, §5.3: a creature's health bar appearing near you discovers it.</summary>
    [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
    internal static class EnemyHudPatch
    {
        private static void Postfix(Character c)
        {
            try
            {
                Discovery.See(c);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("EnemyHud.ShowHud", e);
            }
        }
    }
}
