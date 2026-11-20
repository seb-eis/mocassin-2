// SPDX-License-Identifier: MIT

using Mocassin.App.Startup;

namespace Mocassin.App.Tests.Startup;

/// <summary>
/// Verifies the pure window-placement recovery rules.
/// </summary>
public sealed class WindowPlacementResolverTests
{
    private static readonly WindowPlacementResolver.Bounds primary = new(0, 0, 1920, 1080);

    [Fact]
    public void APlacementOnAScreenIsKept()
    {
        var saved = new WindowPlacementResolver.Bounds(100, 80, 1200, 800);

        var resolved = WindowPlacementResolver.EnsureVisible(saved, [primary]);

        Assert.Equal(saved, resolved);
    }

    [Fact]
    public void APlacementThatTouchesNoScreenFallsBackToTheprimaryScreen()
    {
        var saved = new WindowPlacementResolver.Bounds(5000, 4000, 1200, 800);

        var resolved = WindowPlacementResolver.EnsureVisible(saved, [primary]);

        Assert.Equal(360, resolved.X);
        Assert.Equal(140, resolved.Y);
        Assert.Equal(1200, resolved.Width);
        Assert.Equal(800, resolved.Height);
    }

    [Fact]
    public void APartiallyVisiblePlacementIsKept()
    {
        var saved = new WindowPlacementResolver.Bounds(-1100, 0, 1200, 800);

        var resolved = WindowPlacementResolver.EnsureVisible(saved, [primary]);

        Assert.Equal(saved, resolved);
    }

    [Fact]
    public void AnOversizedPlacementIsClampedToTheprimaryScreen()
    {
        var saved = new WindowPlacementResolver.Bounds(5000, 4000, 3000, 2000);

        var resolved = WindowPlacementResolver.EnsureVisible(saved, [primary]);

        Assert.Equal(0, resolved.X);
        Assert.Equal(0, resolved.Y);
        Assert.Equal(1920, resolved.Width);
        Assert.Equal(1080, resolved.Height);
    }

    [Fact]
    public void ASavedPlacementWithoutScreensIsReturnedUnchanged()
    {
        var saved = new WindowPlacementResolver.Bounds(100, 80, 1200, 800);

        var resolved = WindowPlacementResolver.EnsureVisible(saved, []);

        Assert.Equal(saved, resolved);
    }
}
