using System;
using System.Collections.Generic;
using HarmonyLib;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker.Patches
{
    /// <summary>PLAN.md §5.4: snapshot the counters this roll will touch (one per trophy at this creature's star
    /// tier), to see reseeds for the bar.</summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class DropRollPatch
    {
        private static void Prefix(CharacterDrop __instance, out Dictionary<int, int?> __state)
        {
            __state = null;
            try
            {
                // Exactly the game's level multiplier and effective chance (CharacterDrop.GenerateDropList, 1.0.20).
                Character character = __instance.m_character;
                int multiplier = !character ? 1 : Mathf.Max(1, (int)Mathf.Pow(2f, character.GetLevel() - 1));
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    if (drop == null || drop.m_prefab == null)
                        continue;
                    ItemDrop item = drop.m_prefab.GetComponent<ItemDrop>();
                    if (item == null || item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                        continue;
                    float chance = drop.m_chance;
                    if (drop.m_levelMultiplier)
                        chance *= (float)multiplier;
                    int key = PityMath.CounterKey(drop.m_prefab.name.GetStableHashCode(), chance);
                    if (__state == null)
                        __state = new Dictionary<int, int?>();
                    __state[key] = CounterReader.Get(key);
                }
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("GenerateDropList prefix", e);
            }
        }

        private static void Postfix(Dictionary<int, int?> __state)
        {
            if (__state == null)
                return;
            try
            {
                foreach (KeyValuePair<int, int?> kv in __state)
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
