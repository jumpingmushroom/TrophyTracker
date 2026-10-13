using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TrophyTracker.UI
{
    internal static class UiUtil
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>The HUD's own font, so captions match the game and cover its languages.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                Hud hud = Hud.instance;
                return hud != null && hud.m_healthText != null ? hud.m_healthText.font : null;
            }
        }

        private static Dictionary<string, TMP_FontAsset> _fontsByName;

        /// <summary>Scans every loaded TMP_FontAsset once (first match wins on a duplicate name), caches it
        /// by name for ResolveFont, and returns the names, distinct and sorted ordinal. Call once, when the
        /// panel is first built — the same scan feeds PluginConfig.BindFont. The console command's "fonts"
        /// rescans on demand, in case more fonts loaded since.</summary>
        public static IEnumerable<string> ScanFonts()
        {
            var map = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
            foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (!map.ContainsKey(asset.name))
                    map[asset.name] = asset;
            }
            _fontsByName = map;
            return map.Keys.OrderBy(n => n, StringComparer.Ordinal);
        }

        /// <summary>The panel's configured font: a name match against the fonts from the last ScanFonts, or
        /// the HUD font if unset, "(HUD font)", not found, or ScanFonts has not run yet.</summary>
        public static TMP_FontAsset ResolveFont()
        {
            string name = PluginConfig.Font != null ? PluginConfig.Font.Value : null;
            TMP_FontAsset asset;
            if (!string.IsNullOrEmpty(name) && name != PluginConfig.HudFontLabel &&
                _fontsByName != null && _fontsByName.TryGetValue(name, out asset))
                return asset;
            return Font;
        }

        private static readonly Dictionary<TMP_FontAsset, Material> _materials = new Dictionary<TMP_FontAsset, Material>();

        /// <summary>One outlined material per font, its width updated in place (SetFloat) rather than a new
        /// material per value; width 0 returns the font's own plain material instead. Only the panel uses the
        /// shared material, so mutating it live is safe. So text reads against snow and sky.</summary>
        public static Material TextMaterial(TMP_FontAsset font, float outlineWidth)
        {
            if (font == null)
                return null;
            if (outlineWidth <= 0f)
                return font.material;
            Material mat;
            if (!_materials.TryGetValue(font, out mat))
            {
                mat = new Material(font.material);
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
                mat.hideFlags = HideFlags.HideAndDontSave;
                _materials[font] = mat;
            }
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            return mat;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float size)
        {
            RectTransform rt = Rect(name, parent);
            // Added while inactive so TMP's Awake runs after the font is set; otherwise it looks
            // for its default LiberationSans (not shipped with the game) and logs a warning.
            rt.gameObject.SetActive(false);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = ResolveFont();
            if (font != null)
                t.font = font;
            rt.gameObject.SetActive(true);
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
