// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.Reduction;

namespace Mocassin.Symmetry.SpaceGroups;

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

    /// <summary>
    /// Returns the center stabilizer <c>G0</c> for a site.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="site">The center site.</param>
    /// <returns>The operations that fix the center modulo lattice translation, in catalog order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="setting"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="site"/> cannot be represented on the grid.</exception>
    public static ImmutableArray<SymmetryOperation> GetStabilizer(SpaceGroupSetting setting, Fractional3 site)
    {
        ArgumentNullException.ThrowIfNull(setting);
        return GetStabilizerCore(setting, ToGrid(site, nameof(site)));
    }

    /// <summary>
    /// Decomposes the point group for a center and an ordered geometry sequence.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="center">The center site.</param>
    /// <param name="geometry">The ordered site sequence.</param>
    /// <returns>The exact <c>G0</c>, <c>Gset</c>, <c>Gseq</c>, and induced permutations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="setting"/> or <paramref name="geometry"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// A coordinate is invalid or the geometry contains duplicate sites.
    /// </exception>
    public static PointGroup GetPointGroup(
        SpaceGroupSetting setting,
        Fractional3 center,
        ImmutableArray<Fractional3> geometry)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (geometry.IsDefault)
        {
            throw new ArgumentException("The geometry sequence is required.", nameof(geometry));
        }

        var centerGrid = ToGrid(center, nameof(center));
        var siteGrids = new GridFractional3[geometry.Length];
        var indexByGrid = new Dictionary<GridFractional3, int>();
        for (var index = 0; index < geometry.Length; index++)
        {
            var grid = ToGrid(geometry[index], nameof(geometry));
            if (!indexByGrid.TryAdd(grid, index))
            {
                throw new ArgumentException("The geometry sequence contains duplicate sites.", nameof(geometry));
            }

            siteGrids[index] = grid;
        }

        var centerStabilizer = GetStabilizerCore(setting, centerGrid);
        var setwise = ImmutableArray.CreateBuilder<SymmetryOperation>(centerStabilizer.Length);
        var pointwise = ImmutableArray.CreateBuilder<SymmetryOperation>(centerStabilizer.Length);
        var permutations = new List<SitePermutation>();
        var seenPermutations = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in centerStabilizer)
        {
            var mapping = new int[siteGrids.Length];
            var isSetwise = true;
            for (var index = 0; index < siteGrids.Length; index++)
            {
                var image = operation.Transform(siteGrids[index]).TrimToUnitCell();
                if (!indexByGrid.TryGetValue(image, out var imageIndex))
                {
                    isSetwise = false;
                    break;
                }

                mapping[index] = imageIndex;
            }

            if (!isSetwise)
            {
                continue;
            }

            setwise.Add(operation);
            var permutation = new SitePermutation([.. mapping]);
            if (permutation.IsIdentity)
            {
                pointwise.Add(operation);
            }

            if (seenPermutations.Add(PermutationKey(permutation)))
            {
                permutations.Add(permutation);
            }
        }

        return new PointGroup(
            centerStabilizer,
            setwise.ToImmutable(),
            pointwise.ToImmutable(),
            [.. permutations]);
    }

    /// <summary>
    /// Returns the ordered site orbit <c>G0·S</c>.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="center">The center site.</param>
    /// <param name="geometry">The ordered site sequence.</param>
    /// <returns>Distinct ordered image sequences in catalog-operation order.</returns>
    public static ImmutableArray<ImmutableArray<Fractional3>> GetOrderedOrbit(
        SpaceGroupSetting setting,
        Fractional3 center,
        ImmutableArray<Fractional3> geometry)
    {
        var pointGroup = GetPointGroup(setting, center, geometry);
        var grids = geometry.Select(static site => ToGrid(site, nameof(geometry))).ToArray();
        var sequences = new List<ImmutableArray<Fractional3>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in pointGroup.CenterStabilizer)
        {
            var sequence = new Fractional3[grids.Length];
            for (var index = 0; index < grids.Length; index++)
            {
                sequence[index] = operation.Transform(grids[index]).TrimToUnitCell().ToFractional3();
            }

            var key = SequenceKey(sequence);
            if (seen.Add(key))
            {
                sequences.Add([.. sequence]);
            }
        }

        return [.. sequences];
    }

    /// <summary>
    /// Returns the set orbit <c>G0·{S}</c>.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="center">The center site.</param>
    /// <param name="geometry">The ordered site sequence.</param>
    /// <returns>Distinct unordered image sets, each sorted canonically, in catalog-operation order.</returns>
    public static ImmutableArray<ImmutableArray<Fractional3>> GetSetOrbit(
        SpaceGroupSetting setting,
        Fractional3 center,
        ImmutableArray<Fractional3> geometry)
    {
        var pointGroup = GetPointGroup(setting, center, geometry);
        var grids = geometry.Select(static site => ToGrid(site, nameof(geometry))).ToArray();
        var sequences = new List<ImmutableArray<Fractional3>>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in pointGroup.CenterStabilizer)
        {
            var sequence = grids
                .Select(grid => operation.Transform(grid).TrimToUnitCell())
                .Order()
                .Select(static grid => grid.ToFractional3())
                .ToImmutableArray();
            var key = SequenceKey(sequence);
            if (seen.Add(key))
            {
                sequences.Add(sequence);
            }
        }

        return [.. sequences];
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

    private static ImmutableArray<SymmetryOperation> GetStabilizerCore(
        SpaceGroupSetting setting,
        GridFractional3 center)
    {
        var stabilizer = ImmutableArray.CreateBuilder<SymmetryOperation>();
        foreach (var operation in setting.Operations)
        {
            if (operation.Transform(center).TrimToUnitCell() == center)
            {
                stabilizer.Add(operation);
            }
        }

        return stabilizer.ToImmutable();
    }

    internal static GridFractional3 ToGrid(Fractional3 value, string parameterName)
    {
        if (!value.IsFinite)
        {
            throw new ArgumentException("A symmetry coordinate must be finite.", parameterName);
        }

        var trimmed = value.TrimToUnitCell();
        if (!GridFractional3.TryFromFractional(trimmed, out var grid, out var diagnostic))
        {
            throw new ArgumentException(
                diagnostic?.Message ?? "The coordinate cannot be represented on the exact grid.",
                parameterName);
        }

        return grid;
    }

    private static string PermutationKey(SitePermutation permutation) =>
        string.Join(",", permutation.Mapping);

    private static string SequenceKey(IEnumerable<Fractional3> sequence)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var site in sequence)
        {
            builder.Append(site.A.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(',')
                .Append(site.B.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(',')
                .Append(site.C.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(';');
        }

        return builder.ToString();
    }
}
