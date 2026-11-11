// SPDX-License-Identifier: MIT

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
    ValidationReportSummary Summary,
    IReadOnlyList<ValidationReportDiagnostic> Diagnostics,
    ValidationReportStatistics? Statistics);

/// <summary>
/// Raw diagnostic counts in the validation report.
/// </summary>
internal sealed record ValidationReportSummary(int Errors, int Warnings, int Infos);

/// <summary>
/// One diagnostic in the validation report.
/// </summary>
internal sealed record ValidationReportDiagnostic(
    string Code,
    string Severity,
    string Message,
    ValidationReportPath? Path,
    string? Hint,
    IReadOnlyList<ValidationReportPath> Related);

/// <summary>
/// A navigable model path in the validation report.
/// </summary>
internal sealed record ValidationReportPath(string Domain, string ObjectId, string? Property);

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
