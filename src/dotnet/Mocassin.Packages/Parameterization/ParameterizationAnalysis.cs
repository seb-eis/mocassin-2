// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// The dense applied arrays produced by an apply run.
/// </summary>
/// <param name="PairValues">The pair tables in compiled order.</param>
/// <param name="GroupValues">The cluster tables in compiled order.</param>
/// <param name="RuleFrequencies">The absolute rule frequencies in compiled rule order.</param>
/// <param name="SiteEnergies">The dense site-energy background in position-major order.</param>
internal sealed record ParameterizationTargets(
    double[][] PairValues,
    double[][] GroupValues,
    double[] RuleFrequencies,
    double[] SiteEnergies);

/// <summary>
/// The outcome of a compiled-bound parameterization check.
/// </summary>
/// <param name="Summary">The completeness and effective-value summary.</param>
/// <param name="Diagnostics">The diagnostics in stable severity-first order.</param>
/// <param name="Diff">The mismatch diff when a model-signature mismatch occurred; otherwise <see langword="null"/>.</param>
/// <param name="Targets">The dense applied arrays when requested; otherwise <see langword="null"/>.</param>
internal sealed record ParameterizationAnalysis(
    ParameterizationSummary Summary,
    ImmutableArray<Diagnostic> Diagnostics,
    ParameterizationDiff? Diff,
    ParameterizationTargets? Targets)
{
    /// <summary>
    /// Gets a value indicating whether the analysis contains an error.
    /// </summary>
    public bool HasErrors =>
        Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
