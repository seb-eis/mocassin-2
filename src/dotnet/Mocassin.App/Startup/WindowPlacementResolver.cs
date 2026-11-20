// SPDX-License-Identifier: MIT

namespace Mocassin.App.Startup;

/// <summary>
/// Keeps a restored window placement on a visible screen. The resolver is pure so it can be tested headlessly.
/// </summary>
internal static class WindowPlacementResolver
{
    /// <summary>
    /// A rectangle in virtual-screen coordinates.
    /// </summary>
    /// <param name="X">The left coordinate.</param>
    /// <param name="Y">The top coordinate.</param>
    /// <param name="Width">The width.</param>
    /// <param name="Height">The height.</param>
    public readonly record struct Bounds(double X, double Y, double Width, double Height)
    {
        /// <summary>
        /// Gets the right coordinate.
        /// </summary>
        public double Right => X + Width;

        /// <summary>
        /// Gets the bottom coordinate.
        /// </summary>
        public double Bottom => Y + Height;
    }

    /// <summary>
    /// Chooses a visible placement for a saved window rectangle.
    /// </summary>
    /// <param name="saved">The saved placement.</param>
    /// <param name="screens">The current screens, with the primary screen first.</param>
    /// <param name="minimumVisible">The minimum visible width and height, in device-independent pixels.</param>
    /// <returns>The placement to use.</returns>
    public static Bounds EnsureVisible(
        Bounds saved,
        IReadOnlyList<Bounds> screens,
        double minimumVisible = 64)
    {
        if (screens.Count == 0)
        {
            return saved;
        }

        foreach (var screen in screens)
        {
            if (Intersects(saved, screen, minimumVisible))
            {
                return saved;
            }
        }

        var primary = screens[0];
        var width = Math.Min(saved.Width, primary.Width);
        var height = Math.Min(saved.Height, primary.Height);
        return new Bounds(
            primary.X + ((primary.Width - width) / 2),
            primary.Y + ((primary.Height - height) / 2),
            width,
            height);
    }

    private static bool Intersects(Bounds saved, Bounds screen, double minimumVisible)
    {
        var overlapWidth = Math.Min(saved.Right, screen.Right) - Math.Max(saved.X, screen.X);
        var overlapHeight = Math.Min(saved.Bottom, screen.Bottom) - Math.Max(saved.Y, screen.Y);
        return overlapWidth >= minimumVisible && overlapHeight >= minimumVisible;
    }
}
