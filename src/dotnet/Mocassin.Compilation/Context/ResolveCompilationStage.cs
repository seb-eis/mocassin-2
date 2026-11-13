// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;

namespace Mocassin.Compilation.Context;

/// <summary>
/// Resolves the catalog setting, cell geometry, canonical P1 encoder, particle table, and sorted simulations.
/// </summary>
internal sealed class ResolveCompilationStage : ICompilationStage
{
    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Resolve;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var catalog = SpaceGroupCatalog.Default;
        var versions = SymmetryVersionInfo.FromCatalog(catalog);
        var preview = new StructurePreviewService(catalog, versions);
        var bag = new DiagnosticBag();

        var setting = preview.TryResolve(context.Project.Structure.SpaceGroup, out var resolveDiagnostics);
        bag.AddRange(resolveDiagnostics);
        if (setting is null)
        {
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Precondition,
                "The structure space group cannot be resolved for compilation.",
                bag.Sorted);
        }

        if (!CellGeometry.TryCreate(context.Project.Structure.Cell, setting, out var cell, out var cellDiagnostics)
            || cell is null)
        {
            bag.AddRange(cellDiagnostics);
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Structure,
                "The structure cell cannot be built for compilation.",
                bag.Sorted);
        }

        bag.AddRange(cellDiagnostics);
        var sites = context.Project.Structure.Sites.IsDefault
            ? ImmutableArray<CellSite>.Empty
            : context.Project.Structure.Sites;
        var sortedSites = sites.OrderBy(static site => site.Id.Value).ToArray();
        var references = sortedSites
            .Select(static site => new StructureSiteReference(site.Id, site.Fractional))
            .ToArray();
        if (!UnitCellVectorEncoder.TryCreate(
                setting,
                references,
                out var encoder,
                out _,
                out var encoderDiagnostics)
            || encoder is null)
        {
            bag.AddRange(encoderDiagnostics);
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Structure,
                "The structure cannot be expanded into canonical P1 positions.",
                bag.Sorted);
        }

        bag.AddRange(encoderDiagnostics);
        var particleTable = BuildParticles(context, bag);
        if (particleTable is null)
        {
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Precondition,
                "The particle model cannot be compiled.",
                bag.Sorted);
        }

        ImmutableArray<Core.Model.Simulations.Simulation> simulations = context.Project.Simulations.IsDefault
            ? ImmutableArray<Core.Model.Simulations.Simulation>.Empty
            : [.. context.Project.Simulations.OrderBy(static simulation => simulation.Id.Value)];
        var resolved = context with
        {
            Catalog = catalog,
            Versions = versions,
            Setting = setting,
            Cell = cell,
            Encoder = encoder,
            Particles = particleTable,
            Simulations = simulations,
        };
        return new CompilationStageOutcome(resolved, bag.Sorted);
    }

    private static CompiledParticleTable? BuildParticles(CompilationContext context, DiagnosticBag bag)
    {
        if (context.Project.Particles.IsDefault)
        {
            return new CompiledParticleTable(
                [new CompiledParticle(0, ParticleId.Void, "Void", "∅", 0.0, IsVacancy: false)]);
        }

        var particles = new List<CompiledParticle>
        {
            new(0, ParticleId.Void, "Void", "∅", 0.0, IsVacancy: false),
        };
        foreach (var particle in context.Project.Particles.OrderBy(static particle => particle.Id.Value))
        {
            if (particles.Count > 63)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.CompileLimit,
                    DiagnosticSeverity.Error,
                    "The compiled particle table exceeds the 6-bit occupation-code limit."));
                return null;
            }

            if (particle.Id == ParticleId.Void)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.Precondition,
                    DiagnosticSeverity.Error,
                    "An authored particle reuses the reserved implicit void identifier."));
                return null;
            }

            particles.Add(new CompiledParticle(
                particles.Count,
                particle.Id,
                particle.Name,
                particle.Symbol,
                particle.Charge.Value,
                particle.IsVacancy));
        }

        return new CompiledParticleTable([.. particles]);
    }
}
