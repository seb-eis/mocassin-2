// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Reduction;
using Mocassin.Symmetry.Transitions;

using Mocassin.Compilation.Diagnostics;

namespace Mocassin.Compilation.Energy;

/// <summary>
/// Reduces authored clusters with the shared Step 02/05 kernel and allocates canonical cluster tables.
/// </summary>
internal sealed class ClusterCompilationStage : ICompilationStage
{
    private const string clusterDomain = "mocassin/cluster-energy-id/v1";

    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Clusters;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Encoder is null || context.Setting is null || context.Energy is null)
        {
            throw new InvalidOperationException("The cluster stage requires the compiled structure and pair model.");
        }

        if (context.Project.Clusters.IsDefault || context.Project.Clusters.Length == 0)
        {
            return CompilationStageOutcome.Completed(context);
        }

        if (!TransitionPathResolver.TryCreate(context.Project.Structure, context.Catalog, out var resolver, out var resolverDiagnostics)
            || resolver is null)
        {
            return CompilationFailure.FromSource(
                context,
                CompilationDiagnosticCodes.EnergyGeometry,
                "The cluster geometry cannot be resolved against the structure.",
                resolverDiagnostics);
        }

        var siteById = context.Structure.Sites.ToDictionary(static site => site.Id);
        var tables = ImmutableArray.CreateBuilder<CompiledClusterTable>(context.Project.Clusters.Length);
        var links = Enumerable.Range(0, context.Structure.PositionCount)
            .Select(static _ => new List<CompiledClusterLink>())
            .ToArray();
        foreach (var cluster in context.Project.Clusters.OrderBy(static cluster => cluster.Id.Value))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!siteById.TryGetValue(cluster.CenterSiteId, out var centerSite))
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"Cluster '{cluster.Name}' references an unknown centre site.",
                    new ModelPath("cluster", cluster.Id.Value, "centerSiteId"));
            }

            var centrePoint = centerSite.Fractional;
            if (!resolver.TryResolvePoint(centrePoint, out var centrePosition, out var centreDiagnostics)
                || centrePosition is null)
            {
                return CompilationFailure.FromSource(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"Cluster '{cluster.Name}' cannot resolve its centre position.",
                    centreDiagnostics);
            }

            var surroundingPoints = ImmutableArray.CreateBuilder<Fractional3>(cluster.OffsetVectors.Length);
            var surroundingSiteIndices = ImmutableArray.CreateBuilder<int>(cluster.OffsetVectors.Length);
            var geometryEncodings = ImmutableArray.CreateBuilder<Vector4I>(cluster.OffsetVectors.Length + 1);
            var sitePIndices = ImmutableArray.CreateBuilder<int>(cluster.OffsetVectors.Length + 1);
            geometryEncodings.Add(centrePosition.Encoding);
            sitePIndices.Add(centrePosition.P);
            foreach (var offset in cluster.OffsetVectors)
            {
                var point = new Fractional3(
                    centrePoint.A + offset.A,
                    centrePoint.B + offset.B,
                    centrePoint.C + offset.C);
                if (!resolver.TryResolvePoint(point, out var resolved, out var diagnostics) || resolved is null)
                {
                    return CompilationFailure.FromSource(
                        context,
                        CompilationDiagnosticCodes.EnergyGeometry,
                        $"Cluster '{cluster.Name}' cannot resolve a surrounding position.",
                        diagnostics);
                }

                if (resolved.Stability != SiteStability.Stable)
                {
                    return CompilationFailure.FromMessage(
                        context,
                        CompilationDiagnosticCodes.EnergyGeometry,
                        $"Cluster '{cluster.Name}' resolves a surrounding position to an unstable site.",
                        new ModelPath("cluster", cluster.Id.Value, "offsetVectors"));
                }

                surroundingPoints.Add(encoder(context, resolved.P));
                surroundingSiteIndices.Add(resolved.SiteId == default ? -1 : siteById[resolved.SiteId].Index);
                geometryEncodings.Add(resolved.Encoding);
                sitePIndices.Add(resolved.P);
            }

            if (surroundingSiteIndices.Any(static index => index < 0))
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"Cluster '{cluster.Name}' references an unknown surrounding site.");
            }

            var centerAllowed = MaskIndices(centerSite, context.Structure);
            var surroundingAllowed = new ImmutableArray<int>[surroundingSiteIndices.Count];
            for (var index = 0; index < surroundingSiteIndices.Count; index++)
            {
                surroundingAllowed[index] = MaskIndices(
                    context.Structure.Sites[surroundingSiteIndices[index]],
                    context.Structure);
            }

            var rawCount = RawCount(centerAllowed, surroundingAllowed);
            var hardLimit = Math.Min(context.Options.MaxClusterTupleCount, ClusterReduction.MaxTupleCount);
            if (rawCount > hardLimit)
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.CompileLimit,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Cluster '{cluster.Name}' needs {rawCount} occupation tuples, above the limit {hardLimit}."),
                    new ModelPath("cluster", cluster.Id.Value, "offsetVectors"));
            }

            ClusterReductionResult reduction;
            try
            {
                reduction = ClusterReduction.Reduce(
                    context.Setting,
                    centrePoint,
                    surroundingPoints.MoveToImmutable(),
                    [.. surroundingAllowed]);
            }
            catch (ArgumentException exception)
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"Cluster '{cluster.Name}' has invalid geometry: {exception.Message}",
                    new ModelPath("cluster", cluster.Id.Value));
            }
            catch (InvalidOperationException exception)
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.CompileLimit,
                    $"Cluster '{cluster.Name}' exceeds the bounded occupation enumeration: {exception.Message}",
                    new ModelPath("cluster", cluster.Id.Value));
            }

            var energyId = ComputeEnergyId(cluster, geometryEncodings, context.Setting);
            var occupations = new List<CompiledClusterOccupation>(reduction.Clusters.Length * centerAllowed.Length);
            foreach (var centerParticle in centerAllowed)
            {
                foreach (var occupation in reduction.Clusters)
                {
                    var canonicalTupleBuilder = ImmutableArray.CreateBuilder<int>(occupation.CanonicalTuple.Length + 1);
                    canonicalTupleBuilder.Add(centerParticle);
                    canonicalTupleBuilder.AddRange(occupation.CanonicalTuple);
                    var canonicalTuple = canonicalTupleBuilder.MoveToImmutable();
                    var expansions = occupation.Expansions
                        .Select(expansion =>
                        {
                            var tuple = ClusterReduction.DecodeOccupation(
                                expansion.OccupationCode,
                                occupation.CanonicalTuple.Length);
                            var full = ImmutableArray.CreateBuilder<int>(tuple.Length + 1);
                            full.Add(centerParticle);
                            full.AddRange(tuple);
                            return new CompiledClusterExpansion(
                                ClusterReduction.EncodeOccupation(full.MoveToImmutable()),
                                expansion.PermutationIndex);
                        })
                        .ToImmutableArray();
                    occupations.Add(new CompiledClusterOccupation(
                        ClusterReduction.EncodeOccupation(canonicalTuple),
                        canonicalTuple,
                        expansions));
                }
            }

            occupations.Sort(static (left, right) => left.CanonicalCode.CompareTo(right.CanonicalCode));
            var sitePIndexArray = sitePIndices.MoveToImmutable();
            var geometryEncodingArray = geometryEncodings.MoveToImmutable();
            var tableIndex = tables.Count;
            tables.Add(new CompiledClusterTable(
                tableIndex,
                energyId,
                cluster.Id,
                centerSite.Index,
                centrePosition.P,
                sitePIndexArray,
                geometryEncodingArray,
                [.. occupations],
                [.. new double[occupations.Count]],
                centerAllowed));

            foreach (var p in context.Structure.Sites[centerSite.Index].PositionIndices)
            {
                links[p].Add(new CompiledClusterLink(tableIndex, 0));
            }

            for (var slot = 0; slot < surroundingSiteIndices.Count; slot++)
            {
                links[sitePIndexArray[slot + 1]].Add(new CompiledClusterLink(tableIndex, slot + 1));
            }
        }

        var positions = context.Structure.Positions
            .Select(position => position with
            {
                ClusterLinks = SortLinks(links[position.P]),
            })
            .ToImmutableArray();
        var structure = context.Structure with { Positions = positions };
        var energy = context.Energy with { ClusterTables = tables.MoveToImmutable() };
        return new CompilationStageOutcome(context with { Structure = structure, Energy = energy }, []);
    }

    private static Fractional3 encoder(CompilationContext context, int p) => context.Encoder!.GetPosition(p);

    private static ImmutableArray<int> MaskIndices(CompiledSite site, CompiledStructureModel structure)
    {
        var mask = structure.Positions
            .Where(position => position.SiteIndex == site.Index)
            .Aggregate(0UL, static (current, position) => current | position.AllowedParticleMask);
        var builder = ImmutableArray.CreateBuilder<int>();
        for (var index = 1; index < 64; index++)
        {
            if ((mask & (1UL << index)) != 0)
            {
                builder.Add(index);
            }
        }

        return builder.ToImmutable();
    }

    private static long RawCount(ImmutableArray<int> center, ImmutableArray<int>[] surrounding)
    {
        long count = center.Length;
        foreach (var set in surrounding)
        {
            if (set.Length == 0)
            {
                return 0;
            }

            if (count > long.MaxValue / set.Length)
            {
                return long.MaxValue;
            }

            count *= set.Length;
        }

        return count;
    }

    private static string ComputeEnergyId(
        Cluster cluster,
        ImmutableArray<Vector4I>.Builder geometry,
        Mocassin.Symmetry.SpaceGroups.SpaceGroupSetting setting)
    {
        var builder = new StringBuilder();
        builder.Append(clusterDomain)
            .Append('|')
            .Append(cluster.Id.Value.ToString("D", CultureInfo.InvariantCulture))
            .Append('|')
            .Append(cluster.CenterSiteId.Value.ToString("D", CultureInfo.InvariantCulture));
        foreach (var encoding in geometry)
        {
            builder.Append('|')
                .Append(encoding.A.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.B.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.C.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(encoding.P.ToString(CultureInfo.InvariantCulture));
        }

        builder.Append('|').Append(setting.Number.ToString(CultureInfo.InvariantCulture));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static ImmutableArray<CompiledClusterLink> SortLinks(List<CompiledClusterLink> links) =>
    [
        .. links
            .OrderBy(static link => link.ClusterTableIndex)
            .ThenBy(static link => link.SiteSlot),
    ];
}
