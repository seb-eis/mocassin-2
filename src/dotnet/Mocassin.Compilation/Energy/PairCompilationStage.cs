// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Geometry;
using Mocassin.Symmetry.Energy;
using Mocassin.Symmetry.Geometry;

using Mocassin.Compilation.Diagnostics;

namespace Mocassin.Compilation.Energy;

/// <summary>
/// Samples pair images with the shared Step 05 kernel and allocates canonical pair tables and links.
/// </summary>
internal sealed class PairCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Pairs;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Cell is null || context.Encoder is null || context.Particles is null)
        {
            throw new InvalidOperationException("The pair stage requires the compiled structure.");
        }

        var sampler = new EnergyGeometrySampler(context.Catalog);
        if (!sampler.TrySamplePairs(context.Project.Model, 1, 1, 1, out var previews, out var diagnostics))
        {
            return CompilationFailure.FromSource(
                context,
                CompilationDiagnosticCodes.EnergyGeometry,
                "The pair geometries cannot be sampled.",
                diagnostics);
        }

        var siteIndexById = context.Structure.Sites
            .Select(static (site, index) => (site.Id, index))
            .ToDictionary(static entry => entry.Id, static entry => entry.index);
        var periodic = new PeriodicGeometry(context.Cell, context.Encoder);
        var candidates = new List<PairCandidate>(previews.Length);
        foreach (var preview in previews)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!siteIndexById.TryGetValue(preview.SiteAId, out var centerSiteIndex) ||
                !siteIndexById.TryGetValue(preview.SiteBId, out var partnerSiteIndex))
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"A sampled pair references an unknown site ('{preview.SiteAId:D}', '{preview.SiteBId:D}').");
            }

            var image = periodic.GetMinimumImage(
                new Vector4I(0, 0, 0, preview.CenterP),
                new Vector4I(0, 0, 0, preview.PartnerP));
            var bucket = checked((int)Math.Round(
                preview.DistanceAngstrom / GeometryTolerances.PairDistanceBucket,
                MidpointRounding.AwayFromZero));
            candidates.Add(new PairCandidate(
                preview,
                centerSiteIndex,
                partnerSiteIndex,
                image.Offset,
                bucket));
        }

        candidates.Sort(Compare);
        var uniqueEnergyIds = candidates.Select(static candidate => candidate.Preview.EnergyId).Distinct(StringComparer.Ordinal).LongCount();
        var estimatedBytes = SaturatingMath.Multiply(
            SaturatingMath.Multiply(
                SaturatingMath.Multiply(uniqueEnergyIds, context.Particles.Count),
                context.Particles.Count),
            sizeof(double));
        if (estimatedBytes > context.Options.MaxEnergyTableBytes)
        {
            return CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.CompileLimit,
                $"The estimated pair-table size {estimatedBytes} bytes exceeds the hard limit " +
                $"{context.Options.MaxEnergyTableBytes}.");
        }

        var tableByEnergyId = new Dictionary<string, int>(StringComparer.Ordinal);
        var tables = ImmutableArray.CreateBuilder<CompiledPairTable>();
        var geometries = ImmutableArray.CreateBuilder<CompiledPairGeometry>(candidates.Count);
        var links = Enumerable.Range(0, context.Structure.PositionCount)
            .Select(static _ => new List<CompiledPairLink>())
            .ToArray();
        foreach (var candidate in candidates)
        {
            if (!tableByEnergyId.TryGetValue(candidate.Preview.EnergyId, out var tableIndex))
            {
                tableIndex = tables.Count;
                tableByEnergyId.Add(candidate.Preview.EnergyId, tableIndex);
                tables.Add(new CompiledPairTable(
                    tableIndex,
                    candidate.Preview.EnergyId,
                    context.Particles.Count,
                    [.. new double[context.Particles.Count * context.Particles.Count]]));
            }

            geometries.Add(new CompiledPairGeometry(
                candidate.Preview.GeometryId,
                candidate.Preview.EnergyId,
                tableIndex,
                candidate.CenterSiteIndex,
                candidate.Preview.SiteAId,
                candidate.Preview.CenterP,
                candidate.PartnerSiteIndex,
                candidate.Preview.SiteBId,
                candidate.Preview.PartnerP,
                candidate.Offset,
                candidate.DistanceBucketFm,
                candidate.Preview.DistanceAngstrom,
                candidate.Preview.IsChiral,
                candidate.Preview.ChiralPartnerGeometryId));
            links[candidate.Preview.CenterP].Add(new CompiledPairLink(
                tableIndex,
                candidate.Preview.PartnerP,
                candidate.Offset,
                candidate.DistanceBucketFm));
        }

        var positions = context.Structure.Positions
            .Select(position => position with
            {
                PairLinks = SortLinks(links[position.P]),
            })
            .ToImmutableArray();
        var structure = context.Structure with { Positions = positions };
        var energy = new CompiledEnergyModel(geometries.MoveToImmutable(), tables.ToImmutable(), []);
        return new CompilationStageOutcome(context with { Structure = structure, Energy = energy }, diagnostics);
    }

    private static int Compare(PairCandidate left, PairCandidate right)
    {
        var comparison = left.DistanceBucketFm.CompareTo(right.DistanceBucketFm);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Preview.PartnerP.CompareTo(right.Preview.PartnerP);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Offset.A.CompareTo(right.Offset.A);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Offset.B.CompareTo(right.Offset.B);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Offset.C.CompareTo(right.Offset.C);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.CenterSiteIndex.CompareTo(right.CenterSiteIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.PartnerSiteIndex.CompareTo(right.PartnerSiteIndex);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Preview.CenterP.CompareTo(right.Preview.CenterP);
        return comparison != 0
            ? comparison
            : string.CompareOrdinal(left.Preview.GeometryId, right.Preview.GeometryId);
    }

    private static ImmutableArray<CompiledPairLink> SortLinks(List<CompiledPairLink> links) =>
    [
        .. links
            .OrderBy(static link => link.DistanceBucketFm)
            .ThenBy(static link => link.TargetP)
            .ThenBy(static link => link.Offset.A)
            .ThenBy(static link => link.Offset.B)
            .ThenBy(static link => link.Offset.C)
            .ThenBy(static link => link.PairTableIndex),
    ];

    private sealed record PairCandidate(
        PairInteractionPreview Preview,
        int CenterSiteIndex,
        int PartnerSiteIndex,
        Vector4I Offset,
        int DistanceBucketFm);
}
