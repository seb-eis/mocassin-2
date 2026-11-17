// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Packages.Build;

#pragma warning disable CA1711 // The public report contract names each preview group a collection.

/// <summary>
/// The single typed preview payload of a no-write build.
/// </summary>
/// <remarks>
/// The pipeline evaluates the selected configurations after expansion and job filtering through the shared
/// preview builder. The CLI and Step 26's view models map this payload to their own views; neither reloads the
/// project nor recompiles the model, and no second preview implementation exists. <see cref="Elapsed"/> is the
/// preview evaluation time; it is not a pipeline stage.
/// </remarks>
/// <param name="JobTemplateId">The effective job-template identity.</param>
/// <param name="JobTemplateName">The effective job-template name.</param>
/// <param name="TotalJobs">The selected previewed job count.</param>
/// <param name="Collections">The selected collections in authored order.</param>
/// <param name="Elapsed">The preview evaluation time.</param>
public sealed record BuildPreview(
    string JobTemplateId,
    string JobTemplateName,
    int TotalJobs,
    ImmutableArray<BuildPreviewCollection> Collections,
    TimeSpan Elapsed);

/// <summary>
/// The preview of one selected job collection.
/// </summary>
/// <param name="Id">The authored collection identity.</param>
/// <param name="Name">The authored collection name.</param>
/// <param name="SimulationId">The referenced simulation identity.</param>
/// <param name="Kind">The lower-case simulation kind.</param>
/// <param name="UseDualDofCorrection">Whether the compiled dual-DOF correction is active.</param>
/// <param name="JobCount">The selected jobs of the collection.</param>
/// <param name="ConfigCount">The selected configuration count.</param>
/// <param name="Optimizers">The resolved selection optimizers.</param>
/// <param name="Configs">The selected configurations.</param>
public sealed record BuildPreviewCollection(
    string Id,
    string Name,
    string SimulationId,
    string Kind,
    bool UseDualDofCorrection,
    int JobCount,
    int ConfigCount,
    ImmutableArray<BuildPreviewOptimizer> Optimizers,
    ImmutableArray<BuildPreviewConfig> Configs);

/// <summary>
/// The preview of one resolved selection optimizer.
/// </summary>
/// <param name="SiteId">The optimizer site identity.</param>
/// <param name="ParticleId">The optimizer particle identity.</param>
/// <param name="TargetCount">The number of removed start options.</param>
/// <param name="RemovedStartOptions">The authored removal count, or <see langword="null"/> when unrestricted.</param>
public sealed record BuildPreviewOptimizer(
    string SiteId,
    string ParticleId,
    int TargetCount,
    int? RemovedStartOptions);

/// <summary>
/// The preview of one selected lattice request and doping list.
/// </summary>
/// <param name="Id">The authored configuration identity.</param>
/// <param name="Name">The authored configuration name.</param>
/// <param name="JobCount">The selected jobs of the configuration.</param>
/// <param name="CellA">The supercell count along a.</param>
/// <param name="CellB">The supercell count along b.</param>
/// <param name="CellC">The supercell count along c.</param>
/// <param name="PositionCount">The total generated positions.</param>
/// <param name="TotalChargeBefore">The total charge before doping.</param>
/// <param name="TotalChargeAfter">The total charge after doping and compensation.</param>
/// <param name="IsChargeNeutral">Whether the doped lattice is charge neutral.</param>
/// <param name="Dopings">The applied dopings in priority order.</param>
public sealed record BuildPreviewConfig(
    string Id,
    string Name,
    int JobCount,
    int CellA,
    int CellB,
    int CellC,
    int PositionCount,
    double TotalChargeBefore,
    double TotalChargeAfter,
    bool IsChargeNeutral,
    ImmutableArray<BuildPreviewDoping> Dopings);

/// <summary>
/// One requested and applied doping in a build preview.
/// </summary>
/// <param name="DopingId">The doping identity.</param>
/// <param name="Priority">The authored priority.</param>
/// <param name="RequestedFraction">The requested fraction.</param>
/// <param name="AvailablePrimary">The available primary positions.</param>
/// <param name="RequestedPrimary">The requested primary count before truncation.</param>
/// <param name="AppliedPrimary">The applied primary count.</param>
/// <param name="AppliedDependent">The applied dependent count.</param>
/// <param name="EffectiveFraction">The effective applied fraction.</param>
public sealed record BuildPreviewDoping(
    string DopingId,
    int Priority,
    double RequestedFraction,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);

#pragma warning restore CA1711
