// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine.Reports;

namespace Mocassin.Cli.Build;

/// <summary>
/// The <c>build-preview-v1</c> JSON report emitted by <c>mocassin build --preview</c>.
/// </summary>
/// <remarks>
/// The envelope mirrors the <c>validation-v1</c> report style but carries a preview payload instead of a
/// validation result. The command never writes a package, so the report is the complete observable outcome.
/// </remarks>
internal sealed record BuildPreviewReport(
    int SchemaVersion,
    string Kind,
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    bool Valid,
    int ExitCode,
    BuildPreviewSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    BuildPreviewData? Preview);

/// <summary>
/// The summary counts of one build preview.
/// </summary>
internal sealed record BuildPreviewSummary(
    int Errors,
    int Warnings,
    int Infos,
    int TotalJobs,
    int CollectionCount,
    int ConfigCount);

/// <summary>
/// The preview of one selected job template.
/// </summary>
internal sealed record BuildPreviewData(
    string TemplateId,
    string TemplateName,
    int TotalJobs,
    IReadOnlyList<BuildPreviewCollection> Collections);

/// <summary>
/// The preview of one job collection.
/// </summary>
internal sealed record BuildPreviewCollection(
    string Id,
    string Name,
    string SimulationId,
    string Kind,
    bool UseDualDofCorrection,
    int JobCount,
    int ConfigCount,
    IReadOnlyList<BuildPreviewOptimizer> Optimizers,
    IReadOnlyList<BuildPreviewConfig> Configs);

/// <summary>
/// The preview of one selection optimizer.
/// </summary>
internal sealed record BuildPreviewOptimizer(
    string SiteId,
    string ParticleId,
    int TargetCount,
    int? RemovedStartOptions);

/// <summary>
/// The preview of one lattice request and doping list.
/// </summary>
internal sealed record BuildPreviewConfig(
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
    IReadOnlyList<BuildPreviewDoping> Dopings);

/// <summary>
/// One requested and applied doping in a build preview.
/// </summary>
internal sealed record BuildPreviewDoping(
    string DopingId,
    int Priority,
    double RequestedFraction,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);
