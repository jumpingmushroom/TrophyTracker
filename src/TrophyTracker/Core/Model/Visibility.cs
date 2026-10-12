namespace TrophyTracker.Core.Model
{
    public struct VisibilityInputs
    {
        public bool Enabled;
        public bool ToggledOn;
        public bool HasPlayer;
        public bool MapOpen;
        public bool HideOnLargeMap;
        public bool BuildMode;
        public bool HideInBuildMode;
        public bool InventoryOpen;
        public bool HideWithInventory;
        /// <summary>Inventory open and the drag modifier held (PLAN.md §6.2).</summary>
        public bool DragMode;
        public bool HideWhenEmpty;
        public int RowCount;
    }

    /// <summary>PLAN.md §6.4. Hiding with the vanilla HUD is done by parenting, not here.</summary>
    public static class Visibility
    {
        public static bool Show(VisibilityInputs v)
        {
            if (!v.Enabled || !v.HasPlayer)
                return false;
            if (v.DragMode)
                return true;
            if (!v.ToggledOn)
                return false;
            if (v.HideOnLargeMap && v.MapOpen)
                return false;
            if (v.HideInBuildMode && v.BuildMode)
                return false;
            if (v.HideWithInventory && v.InventoryOpen)
                return false;
            if (v.HideWhenEmpty && v.RowCount == 0)
                return false;
            return true;
        }
    }
}
