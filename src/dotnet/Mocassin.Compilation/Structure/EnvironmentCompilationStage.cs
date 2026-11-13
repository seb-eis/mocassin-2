// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Geometry;

using Mocassin.Compilation.Diagnostics;

namespace Mocassin.Compilation.Structure;

/// <summary>
/// Builds the stable and effective unstable interaction environments and the MMC interaction range.
/// </summary>
internal sealed class EnvironmentCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Environments;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Cell is null || context.Particles is null)
        {
            throw new InvalidOperationException("The environment stage requires the compiled structure.");
        }

        var sites = context.Structure.Sites;
        var siteIndexById = sites
            .Select(static (site, index) => (site.Id, index))
            .ToDictionary(static entry => entry.Id, static entry => entry.index);
        var stable = context.Project.StableEnvironment;
        var authored = context.Project.UnstableEnvironments.IsDefault
            ? ImmutableArray<UnstableEnvironment>.Empty
            : context.Project.UnstableEnvironments;
        var authoredBySite = authored.ToDictionary(static environment => environment.SiteId);
        var unstable = sites
            .Where(static site => site.Stability == SiteStability.Unstable)
            .Select(site => authoredBySite.TryGetValue(site.Id, out var environment)
                ? environment
                : new UnstableEnvironment(site.Id, stable.Cutoff, []))
            .OrderBy(static environment => environment.SiteId.Value)
            .ToArray();
        var diagnostics = ImmutableArray<Mocassin.Core.Diagnostics.Diagnostic>.Empty;

        var environmentIndexBySite = new int[sites.Length];
        var environments = ImmutableArray.CreateBuilder<CompiledEnvironment>(unstable.Length + 1);
        ulong stableMask = 0;
        foreach (var site in sites.Where(static site => site.Stability == SiteStability.Stable))
        {
            stableMask |= context.Structure.Positions
                .Where(position => position.SiteIndex == site.Index)
                .Aggregate(0UL, static (mask, position) => mask | position.AllowedParticleMask);
        }

        environments.Add(new CompiledEnvironment(
            0,
            SiteIndex: -1,
            SiteStability.Stable,
            stableMask,
            Indices(stableMask)));
        for (var index = 0; index < sites.Length; index++)
        {
            environmentIndexBySite[index] = sites[index].Stability == SiteStability.Stable ? 0 : -1;
        }

        for (var index = 0; index < unstable.Length; index++)
        {
            var environment = unstable[index];
            if (!siteIndexById.TryGetValue(environment.SiteId, out var siteIndex))
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationDiagnosticCodes.EnergyGeometry,
                    $"An unstable environment references an unknown site '{environment.SiteId:D}'.");
            }

            var mask = context.Structure.Positions
                .Where(position => position.SiteIndex == siteIndex)
                .Aggregate(0UL, static (current, position) => current | position.AllowedParticleMask);
            environmentIndexBySite[siteIndex] = index + 1;
            environments.Add(new CompiledEnvironment(
                index + 1,
                siteIndex,
                SiteStability.Unstable,
                mask,
                Indices(mask)));
        }

        var updatedSites = sites
            .Select(site => site with { EnvironmentIndex = environmentIndexBySite[site.Index] })
            .ToImmutableArray();
        var updatedPositions = context.Structure.Positions
            .Select(position => position with
            {
                EnvironmentIndex = environmentIndexBySite[position.SiteIndex],
                AllowedParticleMask = position.AllowedParticleMask,
            })
            .ToImmutableArray();
        var maxCutoff = unstable.Length == 0
            ? context.Project.StableEnvironment.Cutoff.Value
            : Math.Max(
                context.Project.StableEnvironment.Cutoff.Value,
                unstable.Max(static environment => environment.Cutoff.Value));
        var structure = context.Structure with
        {
            Sites = updatedSites,
            Environments = environments.MoveToImmutable(),
            Positions = updatedPositions,
            InteractionRange = ComputeInteractionRange(context.Cell, maxCutoff),
        };
        return new CompilationStageOutcome(context with { Structure = structure }, diagnostics);
    }

    private static ImmutableArray<int> Indices(ulong mask)
    {
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

    private static ImmutableArray<int> ComputeInteractionRange(CellGeometry cell, double cutoff)
    {
        if (!double.IsFinite(cutoff) || cutoff <= 0.0)
        {
            return [0, 0, 0, 0];
        }

        var volume = cell.VolumeAngstrom3;
        var da = volume / cell.B.Cross(cell.C).Length;
        var db = volume / cell.C.Cross(cell.A).Length;
        var dc = volume / cell.A.Cross(cell.B).Length;
        return
        [
            Cells(cutoff, da),
            Cells(cutoff, db),
            Cells(cutoff, dc),
            0,
        ];
    }

    private static int Cells(double cutoff, double spacing) =>
        !double.IsFinite(spacing) || spacing <= 0.0
            ? 1
            : checked((int)Math.Ceiling(cutoff / spacing) + 1);
}
