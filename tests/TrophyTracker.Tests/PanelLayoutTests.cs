using TrophyTracker.Core.Model;
using Xunit;

public class PanelLayoutTests
{
    [Theory]
    [InlineData(PanelAnchor.TopLeft, 0f, 1f)]
    [InlineData(PanelAnchor.TopCenter, 0.5f, 1f)]
    [InlineData(PanelAnchor.TopRight, 1f, 1f)]
    [InlineData(PanelAnchor.MiddleLeft, 0f, 0.5f)]
    [InlineData(PanelAnchor.MiddleCenter, 0.5f, 0.5f)]
    [InlineData(PanelAnchor.MiddleRight, 1f, 0.5f)]
    [InlineData(PanelAnchor.BottomLeft, 0f, 0f)]
    [InlineData(PanelAnchor.BottomCenter, 0.5f, 0f)]
    [InlineData(PanelAnchor.BottomRight, 1f, 0f)]
    public void PivotSitsInTheAnchoredCorner(PanelAnchor a, float x, float y)
    {
        Vec2 p = PanelLayout.Pivot(a);
        Assert.Equal(x, p.X);
        Assert.Equal(y, p.Y);
    }

    [Theory]
    [InlineData(PanelAnchor.TopLeft, 20f, -10f)]
    [InlineData(PanelAnchor.TopRight, -20f, -10f)]
    [InlineData(PanelAnchor.BottomRight, -20f, 10f)]
    [InlineData(PanelAnchor.BottomLeft, 20f, 10f)]
    [InlineData(PanelAnchor.MiddleLeft, 20f, 10f)]
    [InlineData(PanelAnchor.MiddleCenter, 20f, 10f)]
    public void PositiveOffsetPointsInwardFromTheAnchoredEdge(PanelAnchor a, float x, float y)
    {
        Vec2 p = PanelLayout.AnchoredPosition(a, 20f, 10f);
        Assert.Equal(x, p.X);
        Assert.Equal(y, p.Y);
    }

    [Theory]
    [InlineData(PanelAnchor.TopLeft)]
    [InlineData(PanelAnchor.TopRight)]
    [InlineData(PanelAnchor.MiddleCenter)]
    [InlineData(PanelAnchor.BottomRight)]
    public void DraggingMovesThePanelExactlyByTheDragDelta(PanelAnchor a)
    {
        Vec2 before = PanelLayout.AnchoredPosition(a, 20f, 10f);
        Vec2 o = PanelLayout.OffsetAfterDrag(a, 20f, 10f, 35f, -12f);
        Vec2 after = PanelLayout.AnchoredPosition(a, o.X, o.Y);
        Assert.Equal(before.X + 35f, after.X, 3);
        Assert.Equal(before.Y - 12f, after.Y, 3);
    }
}
