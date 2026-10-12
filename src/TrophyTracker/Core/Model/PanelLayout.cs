namespace TrophyTracker.Core.Model
{
    public enum PanelAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// PLAN.md §6.1-6.2. Anchor and pivot share the preset corner, so the panel grows away from it. Offsets
    /// point inward: X away from the left/right edge (right for centre columns), Y away from the top/bottom
    /// edge (up for the middle row). Unity UI space: y up.
    /// </summary>
    public static class PanelLayout
    {
        public static Vec2 Pivot(PanelAnchor a)
        {
            int col = (int)a % 3;
            int row = (int)a / 3;
            return new Vec2(col * 0.5f, 1f - row * 0.5f);
        }

        public static Vec2 AnchoredPosition(PanelAnchor a, float offsetX, float offsetY)
        {
            return new Vec2(SignX(a) * offsetX, SignY(a) * offsetY);
        }

        /// <summary>dx, dy: the drag in the parent's local units.</summary>
        public static Vec2 OffsetAfterDrag(PanelAnchor a, float offsetX, float offsetY, float dx, float dy)
        {
            return new Vec2(offsetX + SignX(a) * dx, offsetY + SignY(a) * dy);
        }

        private static float SignX(PanelAnchor a)
        {
            return (int)a % 3 == 2 ? -1f : 1f;
        }

        private static float SignY(PanelAnchor a)
        {
            return (int)a / 3 == 0 ? -1f : 1f;
        }
    }
}
