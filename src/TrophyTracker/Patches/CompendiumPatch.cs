using System;
using HarmonyLib;
using TrophyTracker.UI;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §7. Vanilla destroys everything in m_trophyList on each rebuild, including what we add.</summary>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateTrophyList))]
    internal static class CompendiumPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                CompendiumDecorator.Decorate(__instance);
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("InventoryGui.UpdateTrophyList", e);
            }
        }
    }
}
