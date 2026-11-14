// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// The outcome of applying a parameterization template to a compiled model.
/// </summary>
/// <param name="Model">
/// The parameterized model, or <see langword="null"/> when the apply failed. A signature mismatch and every
/// error condition produce <see langword="null"/>; warnings still produce a model.
/// </param>
/// <param name="Diagnostics">The apply diagnostics in stable severity-first order.</param>
/// <param name="Summary">The completeness and effective-value summary for the supplied template and model.</param>
/// <param name="Diff">
/// The comparison against the current compiled model when a model-signature mismatch produced <c>PRM001</c>;
/// otherwise <see langword="null"/>.
/// </param>
/// <remarks>
/// Warnings do not fail an apply. Errors do, and the result carries no partially applied dense tables. The
/// summary is always present so a caller can report what would have been applied.
/// </remarks>
public sealed record ParameterizationResult(
    ParameterizedModel? Model,
    ImmutableArray<Diagnostic> Diagnostics,
    ParameterizationSummary Summary,
    ParameterizationDiff? Diff)
{
    /// <summary>
    /// Gets a value indicating whether the apply produced a model and no error diagnostics.
    /// </summary>
    public bool IsSuccess =>
        Model is not null &&
        !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
