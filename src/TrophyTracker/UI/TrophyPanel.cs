using System;
using System.Collections.Generic;
using TMPro;
using TrophyTracker.Core;
using TrophyTracker.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyTracker.UI
{
    /// <summary>
    /// PLAN.md §3, §6. The HUD panel: pooled rows under Hud.m_rootObject (so the game's own HUD hiding applies),
    /// rebuilt from RowBuilder only when Runtime.Dirty is set. Drag mode: PLAN.md §6.2 (Task 8).
    /// </summary>
    internal sealed class TrophyPanel : MonoBehaviour
    {
        private const int PoolSize = 12;
        private const float RowHeight = 28f;
        private const float Pad = 8f;
        private const float IconSize = 24f;
        private const float NameWidth = 150f;
        private const float CountWidth = 72f;
        private const float ChanceWidth = 46f;
        private const float Gap = 6f;
        private const float BarHeight = 3f;

        private static readonly Color NameColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color RecentColor = new Color(0.65f, 0.85f, 1f, 1f);
        private static readonly Color ExactColor = new Color(1f, 0.82f, 0.36f, 1f);
        private static readonly Color MutedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color CollectedColor = new Color(0.55f, 0.95f, 0.55f, 1f);
        private static readonly Color BarBack = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color BarFront = new Color(1f, 0.82f, 0.36f, 0.9f);
        private const float DimAlpha = 0.5f;

        public static TrophyPanel Instance;

        private sealed class Row
        {
            public RectTransform Rect;
            public Image Icon;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Count;
            public TextMeshProUGUI Chance;
            public Image BarBack;
            public Image BarFront;
            public string BoundTrophy;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private RectTransform _root;
        private Image _plate;
        private CanvasGroup _group;
        private TextMeshProUGUI _more;
        private Glyphs _glyphs = Glyphs.Unicode;
        private int _rowCount;
        private bool _layoutDirty = true;

        public static void Ensure()
        {
            if (Instance != null)
            {
                Instance.Tick();
                return;
            }
            Hud hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null || UiUtil.Font == null)
                return;
            RectTransform holder = UiUtil.Rect("TrophyTracker", ParentFor(hud));
            Instance = holder.gameObject.AddComponent<TrophyPanel>();
            Instance.Build(holder);
        }

        private static Transform ParentFor(Hud hud)
        {
            Transform root = hud.m_rootObject.transform;
            return PluginConfig.HideWithVanillaHud.Value || root.parent == null ? root : root.parent;
        }

        private void Build(RectTransform root)
        {
            _root = root;
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            _plate = root.gameObject.AddComponent<Image>();
            _plate.sprite = Sprites.Plate;
            _plate.type = Image.Type.Sliced;
            _plate.color = new Color(0f, 0f, 0f, 0.55f);
            _plate.raycastTarget = false;
            for (int i = 0; i < PoolSize; i++)
                _rows.Add(MakeRow(i));
            _more = UiUtil.Text(root, "More", 16f);
            _more.color = MutedColor;
            Place(_more.rectTransform, 0f, NameWidth, RowHeight);

            TMP_FontAsset font = UiUtil.Font;
            bool unicode = font != null && font.HasCharacter('★', true) && font.HasCharacter('≤', true) && font.HasCharacter('✓', true);
            _glyphs = unicode ? Glyphs.Unicode : Glyphs.Ascii;

            PluginConfig.Changed += OnConfigChanged;
        }

        private void OnDestroy()
        {
            PluginConfig.Changed -= OnConfigChanged;
            if (Instance == this)
                Instance = null;
        }

        private void OnConfigChanged()
        {
            _layoutDirty = true;
            Runtime.Dirty = true;
        }

        private Row MakeRow(int i)
        {
            RectTransform rt = UiUtil.Rect("Row" + i, _root);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            var row = new Row { Rect = rt };

            RectTransform iconRt = UiUtil.Rect("Icon", rt);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(IconSize, IconSize);
            row.Icon = iconRt.gameObject.AddComponent<Image>();
            row.Icon.raycastTarget = false;
            row.Icon.preserveAspect = true;

            row.Name = UiUtil.Text(rt, "Name", 18f);
            row.Name.overflowMode = TextOverflowModes.Ellipsis;
            row.Count = UiUtil.Text(rt, "Count", 18f);
            row.Count.alignment = TextAlignmentOptions.MidlineRight;
            row.Chance = UiUtil.Text(rt, "Chance", 15f);
            row.Chance.alignment = TextAlignmentOptions.MidlineRight;
            row.Chance.color = MutedColor;

            row.BarBack = MakeBar(rt, "BarBack", BarBack);
            row.BarFront = MakeBar(rt, "BarFront", BarFront);
            rt.gameObject.SetActive(false);
            return row;
        }

        private static Image MakeBar(RectTransform parent, string name, Color color)
        {
            RectTransform rt = UiUtil.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Sprites.White;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Top-left anchored child at x, with a fixed width, vertically in the text band of the row.</summary>
        private static void Place(RectTransform rt, float x, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, height);
        }

        private float RowWidth()
        {
            float w = NameWidth + Gap + CountWidth;
            if (PluginConfig.ShowIcon.Value)
                w += IconSize + Gap;
            if (PluginConfig.ShowChance.Value)
                w += Gap + ChanceWidth;
            return w;
        }

        private void ApplyLayout()
        {
            _layoutDirty = false;
            Hud hud = Hud.instance;
            if (hud != null && _root.parent != ParentFor(hud))
                _root.SetParent(ParentFor(hud), false);

            PanelAnchor anchor = PluginConfig.Anchor.Value;
            Vec2 pivot = PanelLayout.Pivot(anchor);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(pivot.X, pivot.Y);
            ApplyPosition(PluginConfig.OffsetX.Value, PluginConfig.OffsetY.Value);
            float s = PluginConfig.Scale.Value;
            _root.localScale = new Vector3(s, s, 1f);

            bool icon = PluginConfig.ShowIcon.Value;
            float x = 0f;
            foreach (Row row in _rows)
            {
                x = 0f;
                row.Icon.gameObject.SetActive(icon);
                if (icon)
                {
                    row.Icon.rectTransform.anchoredPosition = new Vector2(0f, -(RowHeight - BarHeight - IconSize) / 2f);
                    x += IconSize + Gap;
                }
                Place(row.Name.rectTransform, x, NameWidth, RowHeight - BarHeight);
                x += NameWidth + Gap;
                Place(row.Count.rectTransform, x, CountWidth, RowHeight - BarHeight);
                x += CountWidth;
                row.Chance.gameObject.SetActive(PluginConfig.ShowChance.Value);
                if (PluginConfig.ShowChance.Value)
                {
                    Place(row.Chance.rectTransform, x + Gap, ChanceWidth, RowHeight - BarHeight);
                }
                float barX = icon ? IconSize + Gap : 0f;
                row.BarBack.rectTransform.anchoredPosition = new Vector2(barX, 0f);
                row.BarBack.rectTransform.sizeDelta = new Vector2(RowWidth() - barX, BarHeight);
                row.BarFront.rectTransform.anchoredPosition = new Vector2(barX, 0f);
            }
        }

        /// <summary>Also used live while dragging (Task 8), without writing config.</summary>
        internal void ApplyPosition(float offsetX, float offsetY)
        {
            Vec2 pos = PanelLayout.AnchoredPosition(PluginConfig.Anchor.Value, offsetX, offsetY);
            _root.anchoredPosition = new Vector2(pos.X, pos.Y);
        }

        private void Tick()
        {
            try
            {
                if (_layoutDirty)
                    ApplyLayout();
                if (Runtime.Dirty)
                {
                    Runtime.Dirty = false;
                    Bind(Runtime.Catalog != null ? Runtime.BuildRows() : new RowList());
                }
                _group.alpha = Visibility.Show(VisibilityNow()) ? 1f : 0f;
            }
            catch (Exception e)
            {
                TrophyTrackerPlugin.WarnOnce("TrophyPanel", e);
                _group.alpha = 0f;
            }
        }

        private VisibilityInputs VisibilityNow()
        {
            Player p = Player.m_localPlayer;
            return new VisibilityInputs
            {
                Enabled = PluginConfig.Enabled.Value,
                ToggledOn = Runtime.ToggledOn,
                HasPlayer = p != null,
                MapOpen = Minimap.IsOpen(),
                HideOnLargeMap = PluginConfig.HideOnLargeMap.Value,
                BuildMode = p != null && p.InPlaceMode(),
                HideInBuildMode = PluginConfig.HideInBuildMode.Value,
                InventoryOpen = InventoryGui.IsVisible(),
                HideWithInventory = PluginConfig.HideWithInventory.Value,
                DragMode = DragModeActive(),
                HideWhenEmpty = PluginConfig.HideWhenEmpty.Value,
                RowCount = _rowCount,
            };
        }

        /// <summary>Replaced in Task 8.</summary>
        private bool DragModeActive()
        {
            return false;
        }

        private void Bind(RowList list)
        {
            _rowCount = list.Rows.Count;
            float width = RowWidth();
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool on = i < list.Rows.Count;
                if (row.Rect.gameObject.activeSelf != on)
                    row.Rect.gameObject.SetActive(on);
                if (!on)
                    continue;
                RowView v = list.Rows[i];
                row.Rect.anchoredPosition = new Vector2(Pad, -Pad - i * RowHeight);
                row.Rect.sizeDelta = new Vector2(width, RowHeight);
                if (row.BoundTrophy != v.TrophyId)
                {
                    row.BoundTrophy = v.TrophyId;
                    row.Icon.sprite = IconFor(v.TrophyId);
                }
                SetText(row.Name, v.NameToken != null ? Localization.instance.Localize(v.NameToken) : TrophyName(v.TrophyId));
                row.Name.color = v.Recent ? RecentColor : NameColor;
                SetText(row.Count, RowText.Count(v, Runtime.Tr, _glyphs));
                Color count = v.State == RowState.Exact ? ExactColor : v.State == RowState.Collected ? CollectedColor : MutedColor;
                if (v.Dim)
                    count.a = DimAlpha;
                row.Count.color = count;
                SetText(row.Chance, RowText.Chance(v.ChancePercent));

                bool bar = PluginConfig.ShowProgressBar.Value && v.BarFill >= 0f;
                row.BarBack.gameObject.SetActive(bar);
                row.BarFront.gameObject.SetActive(bar && v.BarFill > 0f);
                if (bar)
                {
                    float full = row.BarBack.rectTransform.sizeDelta.x;
                    row.BarFront.rectTransform.sizeDelta = new Vector2(full * v.BarFill, BarHeight);
                    Color front = BarFront;
                    if (v.Dim)
                        front.a *= DimAlpha;
                    row.BarFront.color = front;
                }
            }

            int lines = list.Rows.Count;
            _more.gameObject.SetActive(list.More > 0);
            if (list.More > 0)
            {
                SetText(_more, RowText.More(list.More, Runtime.Tr));
                _more.rectTransform.anchoredPosition = new Vector2(Pad, -Pad - lines * RowHeight);
                lines++;
            }
            _root.sizeDelta = new Vector2(width + 2f * Pad, Math.Max(1, lines) * RowHeight + 2f * Pad);
        }

        private static void SetText(TextMeshProUGUI t, string text)
        {
            if (t.text != text)
                t.text = text;
        }

        private Sprite IconFor(string trophy)
        {
            Sprite s;
            if (_icons.TryGetValue(trophy, out s))
                return s;
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(trophy) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            s = item != null ? item.m_itemData.GetIcon() : null;
            _icons[trophy] = s;
            return s;
        }

        /// <summary>The trophy's own name minus " trophy", as the vanilla Compendium does.</summary>
        internal static string TrophyName(string trophy)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(trophy) : null;
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (item == null)
                return trophy;
            string name = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
            return name.EndsWith(" trophy", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 7) : name;
        }
    }
}
