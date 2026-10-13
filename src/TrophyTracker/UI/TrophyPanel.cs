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
        private const float Pad = 10f;
        private const float BarHeight = 3f;
        private const float BaseFontSize = 22f;

        // Geometry scales with PluginConfig.FontSize (k = FontSize / BaseFontSize); recomputed in
        // ApplyLayout, never per frame. Initial values here match k = 1 (the default FontSize), so the
        // pool built in Build() looks right even before the first ApplyLayout runs.
        private float _rowHeight = 32f;
        private float _iconSize = 26f;
        private float _nameWidth = 170f;
        private float _countWidth = 96f;
        private float _chanceWidth = 54f;
        private float _gap = 6f;

        private static readonly Color NameColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color RecentColor = new Color(0.65f, 0.85f, 1f, 1f);
        private static readonly Color ExactColor = new Color(1f, 0.82f, 0.36f, 1f);
        private static readonly Color MutedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
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
        private Canvas _sortCanvas;
        private bool _dragging;
        private Vector2 _dragStartLocal;
        private float _dragStartX;
        private float _dragStartY;
        private float _liveX;
        private float _liveY;
        private bool _outline;
        private TMP_FontAsset _styledFont;
        private Material _styledMaterial;
        private FontStyles _styledStyle;
        private float _styledSize;
        private bool _styled;

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

            // Fonts are not loaded when PluginConfig.Bind runs, so Font binds here instead, once; the same
            // scan also seeds UiUtil.ResolveFont's name lookup.
            PluginConfig.BindFont(UiUtil.ScanFonts());

            // Drawn above the inventory while dragging (PLAN.md §6.2); off otherwise.
            _sortCanvas = root.gameObject.AddComponent<Canvas>();
            _sortCanvas.overrideSorting = false;
            // TMP's SDF shader needs these; TMP only enables them on the canvas it cached at creation (the HUD's).
            _sortCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            _plate = root.gameObject.AddComponent<Image>();
            _plate.sprite = Sprites.Plate;
            _plate.type = Image.Type.Sliced;
            _plate.color = new Color(0f, 0f, 0f, PluginConfig.BackgroundOpacity.Value);
            _plate.raycastTarget = false;
            for (int i = 0; i < PoolSize; i++)
                _rows.Add(MakeRow(i));
            _more = UiUtil.Text(root, "More", BaseFontSize * 0.82f);
            _more.color = MutedColor;
            Place(_more.rectTransform, 0f, _nameWidth, _rowHeight);

            ApplyTextStyle();

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
            ApplyTextStyle();
            _layoutDirty = true;
            Runtime.Dirty = true;
        }

        /// <summary>Font, outline, bold and size on every text the panel owns, from config. Only on config
        /// change, never per frame.</summary>
        private void ApplyTextStyle()
        {
            TMP_FontAsset font = UiUtil.ResolveFont();
            float size = PluginConfig.FontSize.Value;
            FontStyles style = PluginConfig.Bold.Value ? FontStyles.Bold : FontStyles.Normal;
            Material material = UiUtil.TextMaterial(font, PluginConfig.Outline.Value);

            // Config changes unrelated to Text (Scale, OffsetX, ...) also raise Changed; skip the work
            // (and the TMP mesh rebuilds it would trigger on every text) when nothing here actually moved.
            if (_styled && font == _styledFont && material == _styledMaterial && style == _styledStyle && size == _styledSize)
                return;
            _styled = true;
            _styledFont = font;
            _styledMaterial = material;
            _styledStyle = style;
            _styledSize = size;

            float smallSize = size * 0.82f;
            StyleText(_more, font, material, style, smallSize);
            foreach (Row row in _rows)
            {
                StyleText(row.Name, font, material, style, size);
                StyleText(row.Count, font, material, style, size);
                StyleText(row.Chance, font, material, style, smallSize);
            }

            bool unicode = font != null && font.HasCharacter('★', true) && font.HasCharacter('✓', true);
            _glyphs = unicode ? Glyphs.Unicode : Glyphs.Ascii;
        }

        private static void StyleText(TextMeshProUGUI t, TMP_FontAsset font, Material material, FontStyles style, float size)
        {
            if (font != null)
                t.font = font;
            if (material != null)
                t.fontSharedMaterial = material;
            t.fontStyle = style;
            t.fontSize = size;
        }

        private Row MakeRow(int i)
        {
            RectTransform rt = UiUtil.Rect("Row" + i, _root);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            var row = new Row { Rect = rt };

            RectTransform iconRt = UiUtil.Rect("Icon", rt);
            iconRt.anchorMin = iconRt.anchorMax = iconRt.pivot = new Vector2(0f, 1f);
            iconRt.sizeDelta = new Vector2(_iconSize, _iconSize);
            row.Icon = iconRt.gameObject.AddComponent<Image>();
            row.Icon.raycastTarget = false;
            row.Icon.preserveAspect = true;

            row.Name = UiUtil.Text(rt, "Name", BaseFontSize);
            row.Name.overflowMode = TextOverflowModes.Ellipsis;
            row.Count = UiUtil.Text(rt, "Count", BaseFontSize);
            row.Count.alignment = TextAlignmentOptions.MidlineRight;
            row.Chance = UiUtil.Text(rt, "Chance", BaseFontSize * 0.82f);
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
            float w = _nameWidth + _gap + _countWidth;
            if (PluginConfig.ShowIcon.Value)
                w += _iconSize + _gap;
            if (PluginConfig.ShowChance.Value)
                w += _gap + _chanceWidth;
            return w;
        }

        private void ApplyLayout()
        {
            _layoutDirty = false;

            float k = PluginConfig.FontSize.Value / BaseFontSize;
            _rowHeight = 32f * k;
            _iconSize = 26f * k;
            _nameWidth = 170f * k;
            _countWidth = 96f * k;
            _chanceWidth = 54f * k;
            _gap = 6f * k;

            Hud hud = Hud.instance;
            if (hud != null && _root.parent != ParentFor(hud))
                _root.SetParent(ParentFor(hud), false);

            PanelAnchor anchor = PluginConfig.Anchor.Value;
            Vec2 pivot = PanelLayout.Pivot(anchor);
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(pivot.X, pivot.Y);
            ApplyPosition(PluginConfig.OffsetX.Value, PluginConfig.OffsetY.Value);
            float s = PluginConfig.Scale.Value;
            _root.localScale = new Vector3(s, s, 1f);
            if (!_outline)
                _plate.color = new Color(0f, 0f, 0f, PluginConfig.BackgroundOpacity.Value);

            Place(_more.rectTransform, 0f, _nameWidth, _rowHeight);

            bool icon = PluginConfig.ShowIcon.Value;
            float x = 0f;
            foreach (Row row in _rows)
            {
                x = 0f;
                row.Icon.gameObject.SetActive(icon);
                row.Icon.rectTransform.sizeDelta = new Vector2(_iconSize, _iconSize);
                if (icon)
                {
                    row.Icon.rectTransform.anchoredPosition = new Vector2(0f, -(_rowHeight - BarHeight - _iconSize) / 2f);
                    x += _iconSize + _gap;
                }
                Place(row.Name.rectTransform, x, _nameWidth, _rowHeight - BarHeight);
                x += _nameWidth + _gap;
                Place(row.Count.rectTransform, x, _countWidth, _rowHeight - BarHeight);
                x += _countWidth;
                row.Chance.gameObject.SetActive(PluginConfig.ShowChance.Value);
                if (PluginConfig.ShowChance.Value)
                {
                    Place(row.Chance.rectTransform, x + _gap, _chanceWidth, _rowHeight - BarHeight);
                }
                float barX = icon ? _iconSize + _gap : 0f;
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
                Drag();
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

        private bool DragModeActive()
        {
            return PluginConfig.Enabled.Value && InventoryGui.IsVisible() && Keys.Held(PluginConfig.DragModifier.Value);
        }

        /// <summary>PLAN.md §6.2. Manual hit test and mouse tracking, so no raycaster on the HUD canvas is needed.</summary>
        private void Drag()
        {
            bool mode = DragModeActive();
            if (mode != _outline)
            {
                _outline = mode;
                _sortCanvas.overrideSorting = mode;
                _sortCanvas.sortingOrder = mode ? 30000 : 0;
                _plate.color = mode ? new Color(0.25f, 0.2f, 0.05f, 0.85f) : new Color(0f, 0f, 0f, PluginConfig.BackgroundOpacity.Value);
            }
            if (!mode)
            {
                if (_dragging)
                    EndDrag();
                return;
            }

            Vector2 mouse = BepInEx.UnityInput.Current.mousePosition;
            Camera cam = EventCamera();
            if (!_dragging && BepInEx.UnityInput.Current.GetMouseButtonDown(0) &&
                RectTransformUtility.RectangleContainsScreenPoint(_root, mouse, cam))
            {
                _dragging = true;
                _dragStartLocal = ToParentLocal(mouse, cam);
                _dragStartX = _liveX = PluginConfig.OffsetX.Value;
                _dragStartY = _liveY = PluginConfig.OffsetY.Value;
            }
            if (!_dragging)
                return;
            if (!BepInEx.UnityInput.Current.GetMouseButton(0))
            {
                EndDrag();
                return;
            }
            Vector2 now = ToParentLocal(mouse, cam);
            Vec2 o = PanelLayout.OffsetAfterDrag(PluginConfig.Anchor.Value, _dragStartX, _dragStartY, now.x - _dragStartLocal.x, now.y - _dragStartLocal.y);
            _liveX = o.X;
            _liveY = o.Y;
            ApplyPosition(_liveX, _liveY);
        }

        /// <summary>Writes the offset once, on release (or when the modifier is let go mid-drag).</summary>
        private void EndDrag()
        {
            _dragging = false;
            PluginConfig.OffsetX.Value = Mathf.Round(_liveX);
            PluginConfig.OffsetY.Value = Mathf.Round(_liveY);
        }

        private Camera EventCamera()
        {
            Canvas canvas = _root.GetComponentInParent<Canvas>();
            canvas = canvas != null ? canvas.rootCanvas : null;
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private Vector2 ToParentLocal(Vector2 screen, Camera cam)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_root.parent, screen, cam, out local);
            return local;
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
                row.Rect.anchoredPosition = new Vector2(Pad, -Pad - i * _rowHeight);
                row.Rect.sizeDelta = new Vector2(width, _rowHeight);
                if (row.BoundTrophy != v.TrophyId)
                {
                    row.BoundTrophy = v.TrophyId;
                    row.Icon.sprite = IconFor(v.TrophyId);
                    row.Icon.enabled = row.Icon.sprite != null;
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
                _more.rectTransform.anchoredPosition = new Vector2(Pad, -Pad - lines * _rowHeight);
                lines++;
            }
            _root.sizeDelta = new Vector2(width + 2f * Pad, Math.Max(1, lines) * _rowHeight + 2f * Pad);
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
