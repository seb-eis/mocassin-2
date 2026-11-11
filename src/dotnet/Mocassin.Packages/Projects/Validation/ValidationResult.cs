// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// The immutable result of one validation run.
/// </summary>
/// <param name="ProjectId">The project identity, or <see langword="null"/> when the document did not load.</param>
/// <param name="ModelHash">The canonical model hash, or <see langword="null"/> when the document did not load.</param>
/// <param name="DocumentHash">The canonical document hash, or <see langword="null"/> when the document did not load.</param>
/// <param name="LoadFailure">The structured load-failure kind, or <see langword="null"/> when the document loaded.</param>
/// <param name="IsValid">Whether no error remains after policy promotion.</param>
/// <param name="Summary">The raw diagnostic counts before policy promotion.</param>
/// <param name="Diagnostics">The deterministically sorted and deduplicated diagnostics.</param>
/// <param name="Statistics">The requested model statistics, or <see langword="null"/> when unavailable.</param>
/// <param name="Stages">The per-stage execution results.</param>
public sealed record ValidationResult(
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    ProjectLoadFailureKind? LoadFailure,
    bool IsValid,
    DiagnosticSummary Summary,
    ImmutableArray<Diagnostic> Diagnostics,
    ModelStatistics? Statistics,
    ImmutableArray<ValidationStageResult> Stages);
