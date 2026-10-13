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

        /// <summary>Every TMP font asset the game currently has loaded, distinct by name, sorted ordinal.</summary>
        public static IEnumerable<string> AllFontNames()
        {
            return Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                .Select(f => f.name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal);
        }

        /// <summary>The panel's configured font: a name match against the loaded fonts, or the HUD font if
        /// unset, "(HUD font)", or no longer loaded.</summary>
        public static TMP_FontAsset ResolveFont()
        {
            string name = PluginConfig.Font != null ? PluginConfig.Font.Value : null;
            if (!string.IsNullOrEmpty(name) && name != PluginConfig.HudFontLabel)
            {
                foreach (TMP_FontAsset asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                {
                    if (string.Equals(asset.name, name, StringComparison.Ordinal))
                        return asset;
                }
            }
            return Font;
        }

        private static readonly Dictionary<TMP_FontAsset, Dictionary<float, Material>> _materials =
            new Dictionary<TMP_FontAsset, Dictionary<float, Material>>();

        /// <summary>An outlined copy of the font's material for the given outline width, cached per
        /// (font, width); width 0 returns the font's own plain material. So text reads against snow and sky.</summary>
        public static Material TextMaterial(TMP_FontAsset font, float outlineWidth)
        {
            if (font == null)
                return null;
            if (outlineWidth <= 0f)
                return font.material;
            Dictionary<float, Material> byWidth;
            if (!_materials.TryGetValue(font, out byWidth))
            {
                byWidth = new Dictionary<float, Material>();
                _materials[font] = byWidth;
            }
            Material mat;
            if (byWidth.TryGetValue(outlineWidth, out mat))
                return mat;
            mat = new Material(font.material);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            mat.hideFlags = HideFlags.HideAndDontSave;
            byWidth[outlineWidth] = mat;
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
