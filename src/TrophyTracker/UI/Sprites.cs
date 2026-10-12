using System;
using UnityEngine;

namespace TrophyTracker.UI
{
    /// <summary>White sprites drawn in code and tinted: a rounded plate for 9-slicing, a solid square for bars,
    /// a round dot for the Compendium pin badge.</summary>
    internal static class Sprites
    {
        private static Sprite _plate;
        private static Sprite _white;
        private static Sprite _dot;

        public static Sprite Plate => _plate ?? (_plate = Make(32, PlatePixel, new Vector4(12f, 12f, 12f, 12f)));
        public static Sprite White => _white ?? (_white = Make(4, (x, y) => true, Vector4.zero));
        public static Sprite Dot => _dot ?? (_dot = Make(32, DotPixel, Vector4.zero));

        private static bool PlatePixel(float x, float y)
        {
            const float r = 10f;
            float dx = Math.Max(Math.Max(r - x, 0f), x - (32f - r));
            float dy = Math.Max(Math.Max(r - y, 0f), y - (32f - r));
            return dx * dx + dy * dy <= r * r;
        }

        private static bool DotPixel(float x, float y)
        {
            float dx = x - 16f, dy = y - 16f;
            return dx * dx + dy * dy <= 14f * 14f;
        }

        private static Sprite Make(int size, Func<float, float, bool> inside, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            if (inside(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss))
                                hits++;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (ss * ss)));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
