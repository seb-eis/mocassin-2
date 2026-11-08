// SPDX-License-Identifier: MIT

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Symmetry.SpaceGroups;

using Mocassin.Symmetry.Diagnostics;
namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// Maps exact fractional coordinates, canonical P1 positions, and <see cref="Vector4I"/> encodings.
/// </summary>
/// <remarks>
/// <para>
/// The encoder owns the canonical P1 position list: every reference site is expanded to its P1 orbit,
/// trimmed to <c>[0, 1)</c>, deduplicated on the exact grid, sorted by fractional <c>(a, b, c)</c> with
/// <c>a</c> most significant, and assigned <c>P = 0..Pcount-1</c> in that order.
/// </para>
/// <para>
/// Linear indices are signed 32-bit integers. The total lattice position count is computed in 64-bit
/// arithmetic and must not exceed <see cref="int.MaxValue"/>; oversized lattices are rejected rather than
/// wrapped or truncated. Negative cell offsets are relative-path coordinates and must be converted with
/// <see cref="ToSupercellOffset"/> before linearization.
/// </para>
/// </remarks>
public sealed class UnitCellVectorEncoder
{
    /// <summary>
    /// Gets the v1 P1 position budget above which a warning is emitted.
    /// </summary>
    public const int MaxP1Positions = 10_000;

    private readonly FrozenDictionary<GridFractional3, int> indexByGrid;

    private UnitCellVectorEncoder(
        SpaceGroupSetting setting,
        ImmutableArray<GridFractional3> grids,
        ImmutableArray<ExpandedSite> expandedSites,
        ImmutableArray<SiteOrbitInfo> orbits)
    {
        Setting = setting;
        ExpandedSites = expandedSites;
        Orbits = orbits;
        Positions = expandedSites.Select(static site => site.Fractional).ToImmutableArray();
        indexByGrid = grids
            .Select((grid, index) => (grid, index))
            .ToFrozenDictionary(static entry => entry.grid, static entry => entry.index);
    }

    /// <summary>
    /// Gets the resolved space-group setting used for expansion.
    /// </summary>
    public SpaceGroupSetting Setting { get; }

    /// <summary>
    /// Gets the canonical P1 positions in ascending <c>P</c> order.
    /// </summary>
    public ImmutableArray<Fractional3> Positions { get; }

    /// <summary>
    /// Gets the expanded site records in ascending <c>P</c> order.
    /// </summary>
    public ImmutableArray<ExpandedSite> ExpandedSites { get; }

    /// <summary>
    /// Gets the per-reference-site orbit information.
    /// </summary>
    public ImmutableArray<SiteOrbitInfo> Orbits { get; }

    /// <summary>
    /// Gets the number of distinct P1 positions per unit cell.
    /// </summary>
    public int PositionCount => Positions.Length;

    /// <summary>
    /// Builds an encoder from reference positions without stable site identifiers.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="referencePositions">The authored reference positions.</param>
    /// <param name="encoder">The encoder when creation succeeds.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns><see langword="true"/> when no error was produced.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="setting"/> or <paramref name="referencePositions"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryCreate(
        SpaceGroupSetting setting,
        IEnumerable<Fractional3> referencePositions,
        out UnitCellVectorEncoder? encoder,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(referencePositions);
        var references = referencePositions
            .Select(static position => new StructureSiteReference(default, position))
            .ToArray();
        return TryCreateCore(setting, references, out encoder, out _, out diagnostics);
    }

    /// <summary>
    /// Builds an encoder from stable reference sites and reports structured orbit overlaps.
    /// </summary>
    /// <param name="setting">The resolved setting.</param>
    /// <param name="referenceSites">The authored reference sites.</param>
    /// <param name="encoder">The encoder when creation succeeds.</param>
    /// <param name="overlaps">The structured orbit overlaps, sorted deterministically.</param>
    /// <param name="diagnostics">The structured validation diagnostics.</param>
    /// <returns><see langword="true"/> when no error was produced.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="setting"/> or <paramref name="referenceSites"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryCreate(
        SpaceGroupSetting setting,
        IEnumerable<StructureSiteReference> referenceSites,
        out UnitCellVectorEncoder? encoder,
        out ImmutableArray<OrbitOverlap> overlaps,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(referenceSites);
        return TryCreateCore(setting, referenceSites.ToArray(), out encoder, out overlaps, out diagnostics);
    }

    private static bool TryCreateCore(
        SpaceGroupSetting setting,
        StructureSiteReference[] references,
        out UnitCellVectorEncoder? encoder,
        out ImmutableArray<OrbitOverlap> overlaps,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(setting);
        encoder = null;

        var bag = new DiagnosticBag();
        var entries = new List<(GridFractional3 Grid, int ReferenceIndex, int OrbitMemberIndex)>();
        var ownerByGrid = new Dictionary<GridFractional3, (int ReferenceIndex, CellSiteId SiteId)>();
        var overlapEntries = new List<OrbitOverlap>();

        for (var referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
        {
            var reference = references[referenceIndex];
            var site = reference.Position;
            if (!site.IsFinite)
            {
                bag.Add(new Diagnostic(
                    SymmetryDiagnosticCodes.NonFiniteCoordinate,
                    DiagnosticSeverity.Error,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Reference site {referenceIndex} has a non-finite fractional coordinate.")));
                continue;
            }

            var trimmed = site.TrimToUnitCell();
            if (!GridFractional3.TryFromFractional(trimmed, out var grid, out var diagnostic))
            {
                if (diagnostic is not null)
                {
                    bag.Add(diagnostic);
                }

                continue;
            }

            if (diagnostic is not null)
            {
                bag.Add(diagnostic);
            }

            var orbit = SpaceGroupOperations.ExpandGridOrbit(setting, grid);
            for (var memberIndex = 0; memberIndex < orbit.Length; memberIndex++)
            {
                var position = orbit[memberIndex];
                if (ownerByGrid.TryGetValue(position, out var owner))
                {
                    if (owner.ReferenceIndex != referenceIndex)
                    {
                        var path = reference.Id == default
                            ? null
                            : new ModelPath("cellSite", reference.Id.Value);
                        var related = owner.SiteId == default
                            ? default
                            : ImmutableArray.Create(new ModelPath("cellSite", owner.SiteId.Value));
                        bag.Add(new Diagnostic(
                            StructureDiagnosticCodes.SiteOrbitOverlap,
                            DiagnosticSeverity.Error,
                            string.Create(
                                CultureInfo.InvariantCulture,
                                $"Reference sites {owner.ReferenceIndex} and {referenceIndex} expand to the same P1 position."),
                            path,
                            related: related));
                        overlapEntries.Add(new OrbitOverlap(
                            owner.SiteId,
                            reference.Id,
                            position.ToFractional3()));
                    }

                    continue;
                }

                ownerByGrid[position] = (referenceIndex, reference.Id);
                entries.Add((position, referenceIndex, memberIndex));
            }
        }

        overlaps = SortOverlaps(overlapEntries);
        if (bag.HasErrors)
        {
            diagnostics = bag.Sorted;
            return false;
        }

        entries.Sort(static (left, right) => left.Grid.CompareTo(right.Grid));
        if (entries.Count > MaxP1Positions)
        {
            bag.Add(new Diagnostic(
                StructureDiagnosticCodes.P1PositionBudgetExceeded,
                DiagnosticSeverity.Warning,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The structure expands to {entries.Count} P1 positions, above the budget of {MaxP1Positions}.")));
        }

        var grids = ImmutableArray.CreateBuilder<GridFractional3>(entries.Count);
        var expandedSites = ImmutableArray.CreateBuilder<ExpandedSite>(entries.Count);
        for (var p = 0; p < entries.Count; p++)
        {
            var entry = entries[p];
            grids.Add(entry.Grid);
            expandedSites.Add(new ExpandedSite(
                p,
                new Vector4I(0, 0, 0, p),
                entry.Grid.ToFractional3(),
                entry.ReferenceIndex,
                entry.OrbitMemberIndex));
        }

        var orbits = expandedSites
            .GroupBy(static site => site.ReferenceSiteIndex)
            .OrderBy(static group => group.Key)
            .Select(static group =>
            {
                var positions = group.Select(static site => site.Fractional).ToImmutableArray();
                return new SiteOrbitInfo(
                    group.Key,
                    positions[0],
                    positions,
                    group.Select(static site => site.P).ToImmutableArray());
            })
            .ToImmutableArray();

        encoder = new UnitCellVectorEncoder(setting, grids.MoveToImmutable(), expandedSites.MoveToImmutable(), orbits);
        diagnostics = bag.Sorted;
        return true;
    }

    private static ImmutableArray<OrbitOverlap> SortOverlaps(List<OrbitOverlap> overlaps) =>
    [
        .. overlaps
            .OrderBy(static overlap => overlap.FirstSiteId.Value)
            .ThenBy(static overlap => overlap.SecondSiteId.Value)
            .ThenBy(static overlap => overlap.Position.A)
            .ThenBy(static overlap => overlap.Position.B)
            .ThenBy(static overlap => overlap.Position.C),
    ];

    /// <summary>
    /// Gets the canonical position at a P index.
    /// </summary>
    /// <param name="p">The position index.</param>
    /// <returns>The fractional position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="p"/> is outside the position list.</exception>
    public Fractional3 GetPosition(int p)
    {
        ValidateP(p);
        return Positions[p];
    }

    /// <summary>
    /// Finds the canonical P index for a fractional coordinate.
    /// </summary>
    /// <param name="fractional">The fractional coordinate.</param>
    /// <param name="p">The matching index when found.</param>
    /// <returns><see langword="true"/> when the coordinate maps to a canonical position.</returns>
    public bool TryIndexOf(Fractional3 fractional, out int p)
    {
        p = -1;
        if (!fractional.IsFinite)
        {
            return false;
        }

        var trimmed = fractional.TrimToUnitCell();
        if (!GridFractional3.TryFromFractional(trimmed, out var grid, out _))
        {
            return false;
        }

        return indexByGrid.TryGetValue(grid, out p);
    }

    /// <summary>
    /// Encodes a canonical fractional coordinate as a base-cell <see cref="Vector4I"/>.
    /// </summary>
    /// <param name="fractional">The fractional coordinate.</param>
    /// <returns>The base-cell encoding <c>(0, 0, 0, P)</c>.</returns>
    /// <exception cref="ArgumentException">The coordinate is not a canonical P1 position.</exception>
    public Vector4I Encode(Fractional3 fractional) =>
        TryIndexOf(fractional, out var p)
            ? new Vector4I(0, 0, 0, p)
            : throw new ArgumentException("The fractional coordinate is not a canonical P1 position.", nameof(fractional));

    /// <summary>
    /// Decodes a <see cref="Vector4I"/> to a fractional coordinate, including any cell offset.
    /// </summary>
    /// <param name="position">The encoding.</param>
    /// <returns>The fractional coordinate <c>P + (cellA, cellB, cellC)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The P index is outside the position list.</exception>
    public Fractional3 Decode(Vector4I position)
    {
        ValidateP(position.P);
        var p = Positions[position.P];
        return new Fractional3(p.A + position.A, p.B + position.B, p.C + position.C);
    }

    /// <summary>
    /// Converts negative or repeated relative cell offsets to an explicit in-supercell offset.
    /// </summary>
    /// <param name="relative">The relative encoding.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <returns>The normalized in-supercell encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive or P is invalid.</exception>
    public Vector4I ToSupercellOffset(Vector4I relative, int cellCountA, int cellCountB, int cellCountC)
    {
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);
        ValidateP(relative.P);
        return new Vector4I(
            Modulo(relative.A, cellCountA),
            Modulo(relative.B, cellCountB),
            Modulo(relative.C, cellCountC),
            relative.P);
    }

    /// <summary>
    /// Computes the row-major linear lattice index.
    /// </summary>
    /// <param name="position">The in-supercell encoding.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <returns>The signed 32-bit linear index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The P index, a cell offset, or the total lattice size is invalid.
    /// </exception>
    public int GetLinearIndex(Vector4I position, int cellCountA, int cellCountB, int cellCountC)
    {
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);
        ValidateP(position.P);
        ValidateCellOffset(position.A, cellCountA, nameof(position));
        ValidateCellOffset(position.B, cellCountB, nameof(position));
        ValidateCellOffset(position.C, cellCountC, nameof(position));

        var total = GetLatticePositionCount(cellCountA, cellCountB, cellCountC, PositionCount);
        if (total > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellCountA),
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The lattice contains {total} positions, above the signed 32-bit limit."));
        }

        var index =
            position.P
            + ((long)position.C * PositionCount)
            + ((long)position.B * PositionCount * cellCountC)
            + ((long)position.A * PositionCount * cellCountC * cellCountB);
        return checked((int)index);
    }

    /// <summary>
    /// Decodes a linear lattice index.
    /// </summary>
    /// <param name="linearIndex">The signed 32-bit linear index.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <returns>The in-supercell encoding.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The index, a cell count, or the total lattice size is invalid.
    /// </exception>
    public Vector4I DecodeLinearIndex(int linearIndex, int cellCountA, int cellCountB, int cellCountC)
    {
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);
        if (PositionCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(linearIndex), "The encoder has no positions.");
        }

        var total = GetLatticePositionCount(cellCountA, cellCountB, cellCountC, PositionCount);
        if (linearIndex < 0 || linearIndex >= total || total > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(linearIndex),
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The linear index must be in [0, {Math.Min(total, int.MaxValue + 1L)})."));
        }

        var p = (int)(linearIndex % PositionCount);
        var remainder = linearIndex / PositionCount;
        var cellC = (int)(remainder % cellCountC);
        remainder /= cellCountC;
        var cellB = (int)(remainder % cellCountB);
        var cellA = (int)(remainder / cellCountB);
        return new Vector4I(cellA, cellB, cellC, p);
    }

    /// <summary>
    /// Computes the total lattice position count with saturation instead of overflow.
    /// </summary>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <param name="positionCount">The P1 positions per cell.</param>
    /// <returns>
    /// <c>A·B·C·P</c>, or <see cref="long.MaxValue"/> when the exact value would overflow 64-bit arithmetic.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive or P is negative.</exception>
    public static long GetLatticePositionCount(
        int cellCountA,
        int cellCountB,
        int cellCountC,
        int positionCount)
    {
        ValidateCellCounts(cellCountA, cellCountB, cellCountC);
        ArgumentOutOfRangeException.ThrowIfNegative(positionCount);

        var count = (long)cellCountA * cellCountB;
        if (count > long.MaxValue / cellCountC)
        {
            return long.MaxValue;
        }

        count *= cellCountC;
        if (positionCount == 0)
        {
            return 0;
        }

        return count > long.MaxValue / positionCount ? long.MaxValue : count * positionCount;
    }

    /// <summary>
    /// Determines whether a lattice fits the signed 32-bit linear-index contract.
    /// </summary>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <param name="positionCount">The P1 positions per cell.</param>
    /// <returns><see langword="true"/> when the total count is at most <see cref="int.MaxValue"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A cell count is non-positive or P is negative.</exception>
    public static bool IsLatticeSizeSupported(
        int cellCountA,
        int cellCountB,
        int cellCountC,
        int positionCount) =>
        GetLatticePositionCount(cellCountA, cellCountB, cellCountC, positionCount) <= int.MaxValue;

    /// <summary>
    /// Finds the canonical P index for an exact grid position.
    /// </summary>
    /// <param name="grid">The exact grid position.</param>
    /// <param name="p">The matching index when found.</param>
    /// <returns><see langword="true"/> when the grid position is canonical.</returns>
    internal bool TryGetPositionIndex(GridFractional3 grid, out int p) => indexByGrid.TryGetValue(grid, out p);

    private void ValidateP(int p)
    {
        if (p < 0 || p >= PositionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(p));
        }
    }

    private static void ValidateCellCounts(int cellCountA, int cellCountB, int cellCountC)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountA);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountB);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellCountC);
    }

    private static void ValidateCellOffset(int offset, int count, string parameterName)
    {
        if (offset < 0 || offset >= count)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static int Modulo(int value, int modulus)
    {
        var remainder = value % modulus;
        return remainder < 0 ? remainder + modulus : remainder;
    }
}
