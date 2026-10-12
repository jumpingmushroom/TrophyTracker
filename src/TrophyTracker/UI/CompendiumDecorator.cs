using System;
using System.Collections.Generic;
using TMPro;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>PLAN.md §1.5, §7: after vanilla rebuilds the trophy page, tag its elements for pinning and add
    /// silhouettes for discovered but uncollected trophies at their own grid slots.</summary>
    internal static class CompendiumDecorator
    {
        private static readonly Color Silhouette = new Color(0.08f, 0.08f, 0.08f, 0.9f);

        public static void Decorate(InventoryGui gui)
        {
            Player p = Player.m_localPlayer;
            TrophyCatalog catalog = Runtime.Catalog;
            if (p == null || catalog == null || ObjectDB.instance == null)
                return;

            // 1. Vanilla's elements, matched by repeating its own filter and order (InventoryGui.UpdateTrophyList).
            List<string> owned = p.GetTrophies();
            int k = 0;
            foreach (string id in owned)
            {
                if (ObjectDB.instance.GetItemPrefab(id) == null)
                    continue;
                if (k >= gui.m_trophyList.Count)
                    break;
                gui.m_trophyList[k++].AddComponent<PinToggle>().Init(id, true);
            }

            // 2. Silhouettes.
            var ownedSet = new HashSet<string>(owned, StringComparer.Ordinal);
            IDictionary<string, float> kills = Discovery.Kills();
            foreach (TrophyInfo t in catalog.All)
            {
                if (ownedSet.Contains(t.Id) || !Runtime.Discovered.IsTrophyDiscovered(t, ownedSet))
                    continue;
                GameObject prefab = ObjectDB.instance.GetItemPrefab(t.Id);
                ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (item == null)
                    continue;

                GameObject el = UnityEngine.Object.Instantiate(gui.m_trophieElementPrefab, gui.m_trophieListRoot);
                el.SetActive(true);
                var rt = (RectTransform)el.transform;
                Vector2Int pos = item.m_itemData.m_shared.m_trophyPos;
                rt.anchoredPosition = new Vector2(pos.x * gui.m_trophieListSpace, pos.y * -gui.m_trophieListSpace);

                Image icon = rt.Find("icon_bkg/icon").GetComponent<Image>();
                icon.sprite = item.m_itemData.GetIcon();
                icon.color = Silhouette;

                DropSource s = RowBuilder.PrimarySource(t, kills, Runtime.Discovered);
                rt.Find("name").GetComponent<TMP_Text>().text = s != null ? Localization.instance.Localize(s.NameToken) : TrophyPanel.TrophyName(t.Id);
                string hint = s != null
                    ? (Runtime.Tr.Get("uncollected") ?? "Not yet collected. {0} drop. Click to pin.").Replace("{0}", RowText.Chance(s.Chance * 100f))
                    : Runtime.Tr.Get("uncollected_nochance") ?? "Not yet collected. Click to pin.";
                rt.Find("description").GetComponent<TMP_Text>().text = hint;

                gui.m_trophyList.Add(el);
                el.AddComponent<PinToggle>().Init(t.Id, false);
            }

            // 3. Scroll height, with vanilla's formula over every element.
            float lowest = 0f;
            foreach (GameObject el in gui.m_trophyList)
                lowest = Mathf.Min(lowest, ((RectTransform)el.transform).anchoredPosition.y - gui.m_trophieListSpace);
            gui.m_trophieListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(gui.m_trophieListBaseSize, -lowest));
        }
    }
}
