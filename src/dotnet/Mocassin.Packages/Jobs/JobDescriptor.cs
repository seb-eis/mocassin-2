// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The deterministic build-time description of one expanded job.
/// </summary>
/// <remarks>
/// A descriptor is build-time data and not a package DTO. Step 11 wraps it together with the generated lattice
/// into a package job. <see cref="JobId"/> is 1-based across the complete template and is the solver job
/// identifier. <see cref="JobIndex"/> is the zero-based duplicate index within one configuration.
/// </remarks>
/// <param name="JobId">The 1-based job identifier across the complete template.</param>
/// <param name="CollectionIndex">The zero-based authored collection index.</param>
/// <param name="ConfigIndex">The zero-based authored configuration index within the collection.</param>
/// <param name="JobIndex">The zero-based duplicate index within the configuration.</param>
/// <param name="CollectionName">The authored collection name.</param>
/// <param name="ConfigName">The authored configuration name.</param>
/// <param name="SimulationId">The stable identifier of the referenced simulation.</param>
/// <param name="SimulationIndex">The dense index of the compiled simulation.</param>
/// <param name="Kind">The compiled simulation kind.</param>
/// <param name="UseDualDofCorrection">
/// The compiled dual degree-of-freedom intent of a kinetic job. It is copied from the compiled simulation
/// intent and may be cleared only by the documented selection-optimizer stage.
/// </param>
/// <param name="Settings">The fully resolved effective runtime settings.</param>
/// <param name="Lattice">The authored supercell request.</param>
/// <param name="Dopings">The requested doping fractions in authored order.</param>
/// <param name="Optimizers">The collection selection optimizers in authored order.</param>
/// <param name="Seed">The 32-byte deterministic job seed.</param>
/// <param name="CompiledOptimizers">
/// The compiled <c>(position, particle)</c> start options removed from the KMC selection pool, sorted by
/// position and particle index. Empty for MMC and for a collection without optimizers.
/// </param>
public sealed record JobDescriptor(
    int JobId,
    int CollectionIndex,
    int ConfigIndex,
    int JobIndex,
    string CollectionName,
    string ConfigName,
    SimulationId SimulationId,
    int SimulationIndex,
    SimulationKind Kind,
    bool UseDualDofCorrection,
    ResolvedJobSettings Settings,
    LatticeRequest Lattice,
    ImmutableArray<DopingValue> Dopings,
    ImmutableArray<SelectionOptimizer> Optimizers,
    ImmutableArray<byte> Seed,
    ImmutableArray<CompiledSelectionOptimizer> CompiledOptimizers = default);
