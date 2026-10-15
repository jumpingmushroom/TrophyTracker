using System;
using TrophyTracker.Core.Model;
using UnityEngine;

namespace TrophyTracker.Core
{
    /// <summary>PLAN.md §5.1: every ObjectDB trophy, and every ZNetScene creature that drops one.</summary>
    internal static class CatalogBuilder
    {
        public static TrophyCatalog Build(ObjectDB db, ZNetScene scene)
        {
            var catalog = new TrophyCatalog();
            foreach (GameObject go in db.m_items)
            {
                if (go == null)
                    continue;
                ItemDrop item = go.GetComponent<ItemDrop>();
                if (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    catalog.AddTrophy(go.name);
            }

            foreach (GameObject go in scene.m_prefabs)
            {
                if (go == null)
                    continue;
                try
                {
                    Character character = go.GetComponent<Character>();
                    CharacterDrop drops = go.GetComponent<CharacterDrop>();
                    if (character == null || drops == null || character is Player)
                        continue;
                    foreach (CharacterDrop.Drop drop in drops.m_drops)
                    {
                        if (drop == null || drop.m_prefab == null)
                            continue;
                        ItemDrop item = drop.m_prefab.GetComponent<ItemDrop>();
                        if (item == null || item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                            continue;
                        catalog.AddSource(drop.m_prefab.name, new DropSource
                        {
                            CreaturePrefab = go.name,
                            NameToken = character.m_name,
                            Chance = drop.m_chance,
                            LevelMultiplier = drop.m_levelMultiplier,
                            LevelMultiplierNotAmount = drop.m_levelMultiplierNotAmount,
                        });
                    }
                }
                catch (Exception e)
                {
                    catalog.SkippedEntries++;
                    TrophyTrackerPlugin.WarnOnce("catalog " + go.name, e);
                }
            }
            return catalog;
        }
    }
}
