// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Symmetry.Transitions;

/// <summary>
/// Matches kinetic reference paths to canonical P1 positions and their owning reference sites.
/// </summary>
/// <remarks>
/// Each component uses the nearest integer cell shift, with ties resolved toward zero. A point must match
/// exactly one <c>(P, cell-offset)</c> pair within <see cref="GeometryTolerances.PathMatchTolerance"/>. The
/// authored path point is never wrapped or rewritten.
/// </remarks>
public sealed class TransitionPathResolver
{
    private readonly UnitCellVectorEncoder encoder;
    private readonly ImmutableArray<CellSite> referenceSites;

    private TransitionPathResolver(UnitCellVectorEncoder encoder, ImmutableArray<CellSite> referenceSites)
    {
        this.encoder = encoder;
        this.referenceSites = referenceSites;
    }

    /// <summary>
    /// Gets the encoder built from the resolved space group and authored reference sites.
    /// </summary>
    public UnitCellVectorEncoder Encoder => encoder;

    /// <summary>
    /// Builds a resolver for a structure.
    /// </summary>
    /// <param name="structure">The authored structure.</param>
    /// <param name="catalog">The optional catalog override.</param>
    /// <param name="resolver">The resolver when creation succeeds.</param>
    /// <param name="diagnostics">The structure-resolution diagnostics.</param>
    /// <returns><see langword="true"/> when the encoder could be built without errors.</returns>
    public static bool TryCreate(
        StructureModel structure,
        SpaceGroupCatalog? catalog,
        out TransitionPathResolver? resolver,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(structure);
        resolver = null;
        var bag = new DiagnosticBag();
        var setting = SpaceGroupReferenceResolver.Resolve(
            catalog ?? SpaceGroupCatalog.Default,
            structure.SpaceGroup,
            bag);
        if (setting is null)
        {
            diagnostics = bag.Sorted;
            return false;
        }

        var sites = structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : structure.Sites;
        var references = sites
            .Select(static site => new StructureSiteReference(site.Id, site.Fractional))
            .ToArray();
        if (!UnitCellVectorEncoder.TryCreate(
                setting,
                references,
                out var encoder,
                out _,
                out var encoderDiagnostics) ||
            encoder is null)
        {
            bag.AddRange(encoderDiagnostics);
            diagnostics = bag.Sorted;
            return false;
        }

        resolver = new TransitionPathResolver(encoder, sites);
        diagnostics = bag.Sorted;
        return true;
    }

    /// <summary>
    /// Resolves one fractional point to its unique canonical P1 position and cell offset.
    /// </summary>
    /// <param name="point">The fractional point to resolve.</param>
    /// <param name="position">The resolved position when successful.</param>
    /// <param name="diagnostics">The resolution diagnostics.</param>
    /// <returns><see langword="true"/> when exactly one canonical position matched.</returns>
    /// <remarks>
    /// The component shift uses the nearest integer cell offset with ties resolved toward zero. The point is
    /// never wrapped or rewritten. This is the public point-level boundary used by cluster and transition
    /// compilation; it does not duplicate the private nearest-image or tie rule.
    /// </remarks>
    public bool TryResolvePoint(
        Fractional3 point,
        out ResolvedPathPosition? position,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        position = null;
        var bag = new DiagnosticBag();
        if (!point.IsFinite)
        {
            bag.Add(PointError("The fractional point is not finite."));
            diagnostics = bag.Sorted;
            return false;
        }

        var matches = FindMatches(point);
        if (matches.Count != 1)
        {
            var competing = matches.Count == 0
                ? "no canonical P1 position"
                : $"multiple canonical P1 positions ({string.Join(", ", matches.Select(static match => match.P))})";
            bag.Add(PointError($"The fractional point matches {competing} within the path-match tolerance."));
            diagnostics = bag.Sorted;
            return false;
        }

        var match = matches[0];
        var expanded = encoder.ExpandedSites[match.P];
        var site = referenceSites[expanded.ReferenceSiteIndex];
        position = new ResolvedPathPosition(
            site.Id,
            match.P,
            match.Encoding,
            site.Stability,
            encoder.Positions[match.P]);
        diagnostics = bag.Sorted;
        return true;
    }

    /// <summary>
    /// Resolves an ordered fractional path to its canonical P1 positions and cell offsets.
    /// </summary>
    /// <param name="points">The fractional points in path order.</param>
    /// <param name="positions">The resolved positions when successful.</param>
    /// <param name="diagnostics">The resolution diagnostics.</param>
    /// <returns><see langword="true"/> when every point resolved and no position repeated.</returns>
    /// <remarks>
    /// Repeated encoded positions are rejected because a transition path must visit distinct positions. The
    /// point is never wrapped or rewritten.
    /// </remarks>
    public bool TryResolvePath(
        ImmutableArray<Fractional3> points,
        out ImmutableArray<ResolvedPathPosition> positions,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var bag = new DiagnosticBag();
        var resolved = ImmutableArray.CreateBuilder<ResolvedPathPosition>(points.IsDefault ? 0 : points.Length);
        var seen = new Dictionary<Vector4I, int>();
        var path = points.IsDefault ? ImmutableArray<Fractional3>.Empty : points;
        for (var index = 0; index < path.Length; index++)
        {
            if (!TryResolvePoint(path[index], out var position, out var pointDiagnostics) || position is null)
            {
                bag.AddRange(pointDiagnostics);
                continue;
            }

            if (seen.TryGetValue(position.Encoding, out var firstIndex))
            {
                bag.Add(PointError($"Path point {index} repeats the encoded position of point {firstIndex}."));
                continue;
            }

            seen.Add(position.Encoding, index);
            resolved.Add(position);
        }

        positions = resolved.ToImmutable();
        diagnostics = bag.Sorted;
        return !bag.HasErrors;
    }

    /// <summary>
    /// Resolves every point of a kinetic reference path.
    /// </summary>
    /// <param name="transition">The kinetic transition whose path is resolved.</param>
    /// <param name="diagnostics">The path diagnostics.</param>
    /// <returns>The resolved path, or <see langword="null"/> when an error was produced.</returns>
    public ResolvedTransitionPath? TryResolve(
        KineticTransition transition,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(transition);
        var path = transition.ReferencePath.IsDefault
            ? ImmutableArray<Fractional3>.Empty
            : transition.ReferencePath;
        var bag = new DiagnosticBag();
        var positions = ImmutableArray.CreateBuilder<ResolvedPathPosition>(path.Length);
        var encodingIndices = new Dictionary<Vector4I, int>();

        for (var index = 0; index < path.Length; index++)
        {
            var point = path[index];
            if (!point.IsFinite)
            {
                bag.Add(PathError(
                    TransitionDiagnosticCodes.UnmatchedPathPosition,
                    $"Reference path point {index} is not finite.",
                    transition.Id.Value,
                    index));
                continue;
            }

            var matches = FindMatches(point);
            if (matches.Count != 1)
            {
                var competing = matches.Count == 0
                    ? "no canonical P1 position"
                    : $"multiple canonical P1 positions ({string.Join(", ", matches.Select(static match => match.P))})";
                bag.Add(PathError(
                    TransitionDiagnosticCodes.UnmatchedPathPosition,
                    $"Reference path point {index} matches {competing} within the path-match tolerance.",
                    transition.Id.Value,
                    index,
                    matches.Select(static match => new ModelPath(
                        "candidatePosition",
                        Guid.Empty,
                        string.Create(CultureInfo.InvariantCulture, $"p{match.P}")))));
                continue;
            }

            var match = matches[0];
            var expanded = encoder.ExpandedSites[match.P];
            var site = referenceSites[expanded.ReferenceSiteIndex];
            if (encodingIndices.TryGetValue(match.Encoding, out var firstIndex))
            {
                bag.Add(PathError(
                    TransitionDiagnosticCodes.RepeatedPathPosition,
                    $"Reference path point {index} repeats the encoded position of point {firstIndex}.",
                    transition.Id.Value,
                    index));
            }
            else
            {
                encodingIndices.Add(match.Encoding, index);
            }

            positions.Add(new ResolvedPathPosition(
                site.Id,
                match.P,
                match.Encoding,
                site.Stability,
                encoder.Positions[match.P]));
        }

        diagnostics = bag.Sorted;
        return bag.HasErrors ? null : new ResolvedTransitionPath(positions.MoveToImmutable());
    }

    private List<PathMatch> FindMatches(Fractional3 point)
    {
        var matches = new List<PathMatch>();
        for (var p = 0; p < encoder.PositionCount; p++)
        {
            var canonical = encoder.Positions[p];
            if (TryGetComponentShift(point.A, canonical.A, out var a) &&
                TryGetComponentShift(point.B, canonical.B, out var b) &&
                TryGetComponentShift(point.C, canonical.C, out var c))
            {
                matches.Add(new PathMatch(p, new Vector4I(a, b, c, p)));
            }
        }

        return matches;
    }

    private static bool TryGetComponentShift(double coordinate, double canonical, out int shift)
    {
        var delta = coordinate - canonical;
        var rounded = Math.Round(delta, MidpointRounding.AwayFromZero);
        if (Math.Abs(Math.Abs(delta - rounded) - 0.5) <= 1e-12)
        {
            rounded = Math.Truncate(delta);
        }

        if (rounded < int.MinValue || rounded > int.MaxValue ||
            Math.Abs(delta - rounded) > GeometryTolerances.PathMatchTolerance)
        {
            shift = 0;
            return false;
        }

        shift = (int)rounded;
        return true;
    }

    private static Diagnostic PointError(string message) =>
        new(
            TransitionDiagnosticCodes.UnmatchedPathPosition,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("pathPoint", Guid.Empty),
            "Place the point on a canonical P1 position within 1e-6 fractional units.");

    private static Diagnostic PathError(
        string code,
        string message,
        Guid transitionId,
        int index,
        IEnumerable<ModelPath>? candidates = null)
    {
        var related = ImmutableArray.CreateBuilder<ModelPath>();
        related.Add(new ModelPath(
            "referencePathPosition",
            Guid.Empty,
            index.ToString(CultureInfo.InvariantCulture)));
        if (candidates is not null)
        {
            related.AddRange(candidates);
        }

        return new Diagnostic(
            code,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("kineticTransition", transitionId, "referencePath"),
            "Place the point on a canonical P1 position within 1e-6 fractional units.",
            related.ToImmutable());
    }

    private sealed record PathMatch(int P, Vector4I Encoding);
}
