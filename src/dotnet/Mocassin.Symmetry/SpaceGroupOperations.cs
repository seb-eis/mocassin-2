// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry;

/// <summary>
/// Exact orbit operations on a resolved space-group setting.
/// </summary>
/// <remarks>
/// These helpers use the exact <c>24 × 10^12</c> grid for trimming, deduplication, and ordering. Invalid user
/// coordinates produce exceptions here; validation-facing callers use the diagnostic-returning encoder.
/// </remarks>
public static class SpaceGroupOperations
{
    /// <summary>
    /// Expands one fractional site under every operation of a setting.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="site">The fractional site coordinate.</param>
    /// <returns>The trimmed, deduplicated orbit in canonical fractional order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="site"/> is not finite or cannot be represented on the grid.</exception>
    public static ImmutableArray<Fractional3> ExpandOrbit(SpaceGroupSetting setting, Fractional3 site)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (!site.IsFinite)
        {
            throw new ArgumentException("A symmetry site must be finite.", nameof(site));
        }

        var trimmed = site.TrimToUnitCell();
        if (!GridFractional3.TryFromFractional(trimmed, out var grid, out var diagnostic))
        {
            throw new ArgumentException(diagnostic?.Message ?? "The site cannot be represented on the grid.", nameof(site));
        }

        return ExpandGridOrbit(setting, grid)
            .Select(static position => position.ToFractional3())
            .ToImmutableArray();
    }

    internal static ImmutableArray<GridFractional3> ExpandGridOrbit(
        SpaceGroupSetting setting,
        GridFractional3 grid)
    {
        if (setting.Operations.Length == 0)
        {
            throw new ArgumentException("A space-group setting must contain at least one operation.", nameof(setting));
        }

        var positions = new SortedSet<GridFractional3>();
        foreach (var operation in setting.Operations)
        {
            positions.Add(operation.Transform(grid).TrimToUnitCell());
        }

        return positions.ToImmutableArray();
    }
}
