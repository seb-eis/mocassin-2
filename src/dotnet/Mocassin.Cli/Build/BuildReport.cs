// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine.Reports;

namespace Mocassin.Cli.Build;

/// <summary>
/// The <c>build-v1</c> JSON report emitted by <c>mocassin build</c> and <c>mocassin build --preview</c>.
/// </summary>
/// <remarks>
/// One schema covers both modes. The envelope fields come first; the command payload follows the data
/// contract. On a usage error every payload field is present with its null, empty, or default value, so the
/// report validates against the same schema as a completed run.
/// </remarks>
internal sealed record BuildReport(
    int SchemaVersion,
    string Kind,
    bool Ok,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    string Mode,
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    string? ModelSignature,
    string? ParameterizationSignature,
    BuildReportSelection? Selection,
    BuildReportJobSelection? JobSelection,
    IReadOnlyList<BuildReportTiming> Timings,
    BuildReportPackage? Package,
    BuildReportPreview? Preview,
    string Verification);

/// <summary>
/// The effective recipe in the build report.
/// </summary>
/// <param name="BuildTemplate">The selected build template, or <see langword="null"/>.</param>
/// <param name="JobTemplate">The effective job template.</param>
/// <param name="Parameterization">The effective parameterization, or <see langword="null"/> on a usage error.</param>
/// <param name="OutputName">The effective output stem, or <see langword="null"/> when <c>-o</c> was explicit.</param>
internal sealed record BuildReportSelection(
    BuildReportTemplate? BuildTemplate,
    BuildReportTemplate JobTemplate,
    BuildReportParameterization? Parameterization,
    string? OutputName);

/// <summary>
/// One template identity in the build report.
/// </summary>
/// <param name="Id">The canonical UUID.</param>
/// <param name="Name">The template name.</param>
internal sealed record BuildReportTemplate(string Id, string? Name);

/// <summary>
/// The effective parameterization in the build report.
/// </summary>
/// <param name="Id">The canonical UUID, or <see langword="null"/> for the implicit default.</param>
/// <param name="Name">The template name, or <see langword="null"/> for the implicit default.</param>
/// <param name="ImplicitDefault">Whether the Step 09 implicit default applies.</param>
internal sealed record BuildReportParameterization(string? Id, string? Name, bool ImplicitDefault);

/// <summary>
/// The normalized job selection in the build report.
/// </summary>
/// <param name="Expression">The canonical expression, or <see langword="null"/> on a usage error.</param>
/// <param name="ExpandedCount">The complete template expansion count.</param>
/// <param name="SelectedCount">The selected job count.</param>
internal sealed record BuildReportJobSelection(string? Expression, int ExpandedCount, int SelectedCount);

/// <summary>
/// One timing entry in the build report.
/// </summary>
/// <param name="Stage">The lower-case stage name.</param>
/// <param name="Ms">The elapsed milliseconds.</param>
internal sealed record BuildReportTiming(string Stage, double Ms);

/// <summary>
/// The published package summary in the build report.
/// </summary>
/// <param name="Path">The published package path.</param>
/// <param name="Sha256">The published SHA-256.</param>
/// <param name="SizeBytes">The published size in bytes.</param>
/// <param name="JobCount">The job row count.</param>
/// <param name="ModelCount">The model row count.</param>
/// <param name="LatticeCount">The distinct lattice count.</param>
/// <param name="LatticeReuseFactor">The job/lattice reuse factor.</param>
/// <param name="Published">Whether the package is published.</param>
internal sealed record BuildReportPackage(
    string Path,
    string Sha256,
    long SizeBytes,
    int JobCount,
    int ModelCount,
    int LatticeCount,
    double LatticeReuseFactor,
    bool Published);

/// <summary>
/// The typed preview projection in the build report.
/// </summary>
/// <param name="JobTemplateId">The effective job-template identity.</param>
/// <param name="JobTemplateName">The effective job-template name.</param>
/// <param name="TotalJobs">The selected previewed job count.</param>
/// <param name="Collections">The selected collections.</param>
internal sealed record BuildReportPreview(
    string JobTemplateId,
    string JobTemplateName,
    int TotalJobs,
    IReadOnlyList<BuildReportCollection> Collections);

/// <summary>
/// One previewed collection in the build report.
/// </summary>
/// <param name="Id">The authored collection identity.</param>
/// <param name="Name">The authored collection name.</param>
/// <param name="SimulationId">The referenced simulation identity.</param>
/// <param name="Kind">The lower-case simulation kind.</param>
/// <param name="UseDualDofCorrection">Whether the dual-DOF correction is active.</param>
/// <param name="JobCount">The selected jobs of the collection.</param>
/// <param name="ConfigCount">The selected configuration count.</param>
/// <param name="Optimizers">The resolved selection optimizers.</param>
/// <param name="Configs">The selected configurations.</param>
internal sealed record BuildReportCollection(
    string Id,
    string Name,
    string SimulationId,
    string Kind,
    bool UseDualDofCorrection,
    int JobCount,
    int ConfigCount,
    IReadOnlyList<BuildReportOptimizer> Optimizers,
    IReadOnlyList<BuildReportConfig> Configs);

/// <summary>
/// One resolved selection optimizer in the build report.
/// </summary>
/// <param name="SiteId">The optimizer site identity.</param>
/// <param name="ParticleId">The optimizer particle identity.</param>
/// <param name="TargetCount">The number of removed start options.</param>
/// <param name="RemovedStartOptions">The authored removal count, or <see langword="null"/>.</param>
internal sealed record BuildReportOptimizer(
    string SiteId,
    string ParticleId,
    int TargetCount,
    int? RemovedStartOptions);

/// <summary>
/// One previewed configuration in the build report.
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
/// <param name="Dopings">The applied dopings.</param>
internal sealed record BuildReportConfig(
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
    IReadOnlyList<BuildReportDoping> Dopings);

/// <summary>
/// One applied doping in the build report.
/// </summary>
/// <param name="DopingId">The doping identity.</param>
/// <param name="Priority">The authored priority.</param>
/// <param name="RequestedFraction">The requested fraction.</param>
/// <param name="AvailablePrimary">The available primary positions.</param>
/// <param name="RequestedPrimary">The requested primary count before truncation.</param>
/// <param name="AppliedPrimary">The applied primary count.</param>
/// <param name="AppliedDependent">The applied dependent count.</param>
/// <param name="EffectiveFraction">The effective applied fraction.</param>
internal sealed record BuildReportDoping(
    string DopingId,
    int Priority,
    double RequestedFraction,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);
