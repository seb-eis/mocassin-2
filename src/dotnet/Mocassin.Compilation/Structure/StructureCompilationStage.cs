// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Compilation.Structure;

/// <summary>
/// Builds the compiled particle table, reference-site table, and canonical P1 position table.
/// </summary>
internal sealed class StructureCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Structure;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Cell is null || context.Encoder is null || context.Particles is null)
        {
            throw new InvalidOperationException("The structure stage requires the resolved compilation context.");
        }

        var bag = new DiagnosticBag();
        if (context.Cache is not null)
        {
            var key = StageCacheKeys.Structure(context);
            if (context.Cache.TryGet<CompiledStructureModel>(key, out var cached) && cached is not null)
            {
                if (cached.PositionCount == context.Encoder.PositionCount &&
                    cached.SiteCount == (context.Project.Structure.Sites.IsDefault ? 0 : context.Project.Structure.Sites.Length))
                {
                    return new CompilationStageOutcome(context with { Structure = cached }, []);
                }

                bag.Add(new Diagnostic(
                    CompilationErrorCodes.CacheIntegrity,
                    DiagnosticSeverity.Warning,
                    "A cached structure entry failed its integrity check and was recomputed."));
            }
        }

        ImmutableArray<CellSite> sites = context.Project.Structure.Sites.IsDefault
            ? ImmutableArray<CellSite>.Empty
            : [.. context.Project.Structure.Sites.OrderBy(static site => site.Id.Value)];
        var positionsBySite = context.Encoder.ExpandedSites
            .GroupBy(static site => site.ReferenceSiteIndex)
            .ToDictionary(static group => group.Key, static group => group.Select(static site => site.P).ToImmutableArray());
        var compiledSites = ImmutableArray.CreateBuilder<CompiledSite>(sites.Length);
        var masks = new ulong[sites.Length];
        for (var siteIndex = 0; siteIndex < sites.Length; siteIndex++)
        {
            var site = sites[siteIndex];
            var mask = BuildMask(context, site, bag);
            masks[siteIndex] = mask;
            compiledSites.Add(new CompiledSite(
                siteIndex,
                site.Id,
                site.Name,
                site.Fractional,
                site.Stability,
                site.ParticleSetId,
                EnvironmentIndex: -1,
                positionsBySite.TryGetValue(siteIndex, out var pIndices) ? pIndices : []));
        }

        if (bag.HasErrors)
        {
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Structure,
                "The compiled site table cannot be built.",
                bag.Sorted);
        }

        var positions = ImmutableArray.CreateBuilder<CompiledPosition>(context.Encoder.PositionCount);
        foreach (var expanded in context.Encoder.ExpandedSites)
        {
            var site = sites[expanded.ReferenceSiteIndex];
            positions.Add(new CompiledPosition(
                expanded.P,
                expanded.Encoding,
                expanded.Fractional,
                context.Cell.ToCartesian(expanded.Fractional),
                expanded.ReferenceSiteIndex,
                site.Id,
                site.Stability,
                masks[expanded.ReferenceSiteIndex],
                EnvironmentIndex: -1,
                expanded.ReferenceSiteIndex,
                [],
                []));
        }

        var structure = new CompiledStructureModel(
            [context.Cell.A, context.Cell.B, context.Cell.C],
            context.Cell.VolumeAngstrom3,
            compiledSites.MoveToImmutable(),
            [],
            positions.MoveToImmutable(),
            [0, 0, 0, 0]);
        context.Cache?.GetOrAdd(key: StageCacheKeys.Structure(context), () => structure);
        return new CompilationStageOutcome(context with { Structure = structure }, bag.Sorted);
    }

    private static ulong BuildMask(CompilationContext context, CellSite site, DiagnosticBag bag)
    {
        if (context.Particles is null)
        {
            return 0;
        }

        var set = context.Project.Find(site.ParticleSetId);
        if (set is null || set.ParticleIds.IsDefault || set.ParticleIds.Length == 0)
        {
            return 0;
        }

        ulong mask = 0;
        foreach (var particleId in set.ParticleIds)
        {
            if (!context.Particles.TryGetIndex(particleId, out var index) || index == 0)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.Structure,
                    DiagnosticSeverity.Error,
                    $"Site '{site.Name}' references an uncompiled particle '{particleId:D}'.",
                    new ModelPath("cellSite", site.Id.Value, "particleSetId")));
                continue;
            }

            mask |= 1UL << index;
        }

        return mask;
    }
}
