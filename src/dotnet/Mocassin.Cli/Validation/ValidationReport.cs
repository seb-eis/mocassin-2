// SPDX-License-Identifier: MIT

using Mocassin.Cli.CommandLine.Reports;

namespace Mocassin.Cli.Validation;

/// <summary>
/// The <c>validation-v1</c> JSON report.
/// </summary>
internal sealed record ValidationReport(
    int SchemaVersion,
    string Kind,
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    bool Valid,
    int ExitCode,
    ReportSummary Summary,
    IReadOnlyList<ReportDiagnostic> Diagnostics,
    ValidationReportStatistics? Statistics);

/// <summary>
/// Model statistics in the validation report.
/// </summary>
internal sealed record ValidationReportStatistics(
    int ParticleCount,
    int ParticleSetCount,
    int ReferenceSiteCount,
    long P1PositionCount,
    long StablePositionCount,
    long UnstablePositionCount,
    int KineticTransitionCount,
    int MetropolisTransitionCount,
    int ClusterCount,
    long EstimatedPairInteractionCount,
    long EstimatedUniqueClusterPermutations,
    long EstimatedRuleCount,
    long EstimatedMemoryBytes,
    int CellCountA,
    int CellCountB,
    int CellCountC,
    long EstimatedPositions);
