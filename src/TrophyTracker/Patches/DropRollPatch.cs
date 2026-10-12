using System;
using System.Collections.Generic;
using HarmonyLib;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §5.4: snapshot this creature's trophy counters around the roll, to see reseeds for the bar.</summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class DropRollPatch
    {
        private static void Prefix(CharacterDrop __instance, out Dictionary<string, CounterEntry?> __state)
        {
            __state = null;
            try
            {
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    if (drop == null || drop.m_prefab == null)
                        continue;
                    ItemDrop item = drop.m_prefab.GetComponent<ItemDrop>();
                    if (item == null || item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                        continue;
                    if (__state == null)
                        __state = new Dictionary<string, CounterEntry?>();
                    __state[drop.m_prefab.name] = CounterReader.Get(drop.m_prefab.name);
                }
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("GenerateDropList prefix", e);
            }
        }

        private static void Postfix(Dictionary<string, CounterEntry?> __state)
        {
            if (__state == null)
                return;
            try
            {
                foreach (KeyValuePair<string, CounterEntry?> kv in __state)
                    Runtime.Bars.Observe(kv.Key, kv.Value, CounterReader.Get(kv.Key));
                Runtime.Dirty = true;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("GenerateDropList postfix", e);
            }
        }
    }
}
