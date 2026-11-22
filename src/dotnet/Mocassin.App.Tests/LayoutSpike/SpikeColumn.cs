// SPDX-License-Identifier: MIT

using Avalonia.Controls;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One table column in the spike's shared column policy.
/// </summary>
/// <remarks>
/// The policy is deliberately a plain value: every option applies the same widths to its header and its rows, so
/// the alignment evidence compares the same numbers instead of two independent auto-size passes.
/// </remarks>
/// <param name="Header">The visible header text and sort key.</param>
/// <param name="Width">The grid length applied to the header and every row cell.</param>
internal sealed record SpikeColumn(string Header, GridLength Width)
{
    /// <summary>
    /// Creates a proportional column.
    /// </summary>
    /// <param name="header">The visible header text.</param>
    /// <param name="weight">The star weight.</param>
    /// <returns>The column.</returns>
    public static SpikeColumn Star(string header, double weight = 1.0) =>
        new(header, new GridLength(weight, GridUnitType.Star));

    /// <summary>
    /// Creates a fixed-width column.
    /// </summary>
    /// <param name="header">The visible header text.</param>
    /// <param name="width">The pixel width.</param>
    /// <returns>The column.</returns>
    public static SpikeColumn Fixed(string header, double width) =>
        new(header, new GridLength(width, GridUnitType.Pixel));
}
