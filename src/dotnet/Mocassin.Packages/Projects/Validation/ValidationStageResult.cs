// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Records the outcome and timing of one validation stage.
/// </summary>
/// <param name="Stage">The stage that ran or was skipped.</param>
/// <param name="Status">Whether the stage completed, failed, or was skipped.</param>
/// <param name="DiagnosticCount">The number of stage diagnostics surviving global deduplication.</param>
/// <param name="Elapsed">The measured stage duration, or zero for skipped and caller-owned stages.</param>
public sealed record ValidationStageResult(
    ValidationStage Stage,
    ValidationStageStatus Status,
    int DiagnosticCount,
    TimeSpan Elapsed);
