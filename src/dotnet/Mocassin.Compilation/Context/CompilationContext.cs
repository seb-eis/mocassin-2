// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Indexing;
using Mocassin.Compilation.Caching;
using Mocassin.Compilation.Transitions;
using Mocassin.Compilation.Simulations;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Compilation.Context;

/// <summary>
/// Immutable input passed from one compilation stage to the next.
/// </summary>
/// <param name="Project">The validated project snapshot.</param>
/// <param name="Options">The effective compilation options.</param>
internal sealed record CompilationContext(Project Project, CompilationOptions Options)
{
    /// <summary>
    /// Gets the embedded space-group catalog.
    /// </summary>
    public SpaceGroupCatalog Catalog { get; init; } = SpaceGroupCatalog.Default;

    /// <summary>
    /// Gets the catalog and symmetry-contract versions.
    /// </summary>
    public SymmetryVersionInfo Versions { get; init; } = SymmetryVersionInfo.FromCatalog(SpaceGroupCatalog.Default);

    /// <summary>
    /// Gets the resolved space-group setting.
    /// </summary>
    public SpaceGroupSetting? Setting { get; init; }

    /// <summary>
    /// Gets the validated cell geometry.
    /// </summary>
    public CellGeometry? Cell { get; init; }

    /// <summary>
    /// Gets the canonical P1 encoder.
    /// </summary>
    public UnitCellVectorEncoder? Encoder { get; init; }

    /// <summary>
    /// Gets the compiled particle table.
    /// </summary>
    public CompiledParticleTable? Particles { get; init; }

    /// <summary>
    /// Gets the compiled structure.
    /// </summary>
    public CompiledStructureModel? Structure { get; init; }

    /// <summary>
    /// Gets the compiled pair-energy model.
    /// </summary>
    public CompiledEnergyModel? Energy { get; init; }

    /// <summary>
    /// Gets the authored simulations in ascending identifier order.
    /// </summary>
    public ImmutableArray<Simulation> Simulations { get; init; } = [];

    /// <summary>
    /// Gets the compiled transition model.
    /// </summary>
    public CompiledTransitionModel? Transitions { get; init; }

    /// <summary>
    /// Gets the compiled simulations.
    /// </summary>
    public ImmutableArray<CompiledSimulation> CompiledSimulations { get; init; } = [];

    /// <summary>
    /// Gets the inspection-only index-to-stable-ID maps.
    /// </summary>
    public CompiledIndexMaps? IndexMaps { get; init; }

    /// <summary>
    /// Gets the optional stage cache used by this run.
    /// </summary>
    public ICompilationCache? Cache { get; init; }
}
