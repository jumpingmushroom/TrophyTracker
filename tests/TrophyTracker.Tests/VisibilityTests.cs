using TrophyTracker.Core.Model;
using Xunit;

public class VisibilityTests
{
    private static VisibilityInputs Shown()
    {
        return new VisibilityInputs
        {
            Enabled = true, ToggledOn = true, HasPlayer = true,
            HideOnLargeMap = true, HideInBuildMode = true, HideWithInventory = true, HideWhenEmpty = true,
            RowCount = 2,
        };
    }

    [Fact]
    public void ShownByDefault()
    {
        Assert.True(Visibility.Show(Shown()));
    }

    [Fact]
    public void EachHideConditionHidesOnlyWhenItsToggleIsOn()
    {
        VisibilityInputs v = Shown(); v.MapOpen = true; Assert.False(Visibility.Show(v));
        v.HideOnLargeMap = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.BuildMode = true; Assert.False(Visibility.Show(v));
        v.HideInBuildMode = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.InventoryOpen = true; Assert.False(Visibility.Show(v));
        v.HideWithInventory = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.RowCount = 0; Assert.False(Visibility.Show(v));
        v.HideWhenEmpty = false; Assert.True(Visibility.Show(v));
        v = Shown(); v.ToggledOn = false; Assert.False(Visibility.Show(v));
    }

    [Fact]
    public void DragModeShowsThePanelOverInventoryToggleAndEmpty()
    {
        VisibilityInputs v = Shown();
        v.InventoryOpen = true; v.DragMode = true; v.ToggledOn = false; v.RowCount = 0;
        Assert.True(Visibility.Show(v));
    }

    [Fact]
    public void NeverWithoutAPlayerOrWhenDisabled()
    {
        VisibilityInputs v = Shown(); v.HasPlayer = false; v.DragMode = true; Assert.False(Visibility.Show(v));
        v = Shown(); v.Enabled = false; v.DragMode = true; Assert.False(Visibility.Show(v));
    }
}
