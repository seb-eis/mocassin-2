// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The outcome of expanding one job template into deterministic job descriptors.
/// </summary>
/// <remarks>
/// Invalid user data returns diagnostics and an empty <see cref="Jobs"/> array; expansion never exposes a
/// partially generated list. Warnings do not prevent descriptors.
/// </remarks>
/// <param name="Jobs">The expanded descriptors in template order, or an empty array on failure.</param>
/// <param name="Diagnostics">The diagnostics in stable severity-first order.</param>
public sealed record JobExpansionResult(
    ImmutableArray<JobDescriptor> Jobs,
    ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether expansion produced descriptors without any error diagnostic.
    /// </summary>
    public bool IsSuccess =>
        Jobs.Length > 0 &&
        !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Creates a failed result with an empty descriptor list.
    /// </summary>
    /// <param name="diagnostics">The diagnostics that explain the failure.</param>
    /// <returns>The failed result.</returns>
    public static JobExpansionResult Failed(ImmutableArray<Diagnostic> diagnostics) => new([], diagnostics);
}
