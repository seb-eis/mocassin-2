// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;

namespace Mocassin.Packages.Build;

/// <summary>
/// The effective recipe of one build.
/// </summary>
/// <remarks>
/// The report records the effective identities, not the authored recipe, so a build is reproducible from the
/// report. <see cref="ImplicitDefaultParameterization"/> is true only when no parameterization template is
/// selected and the Step 09 implicit default applies.
/// </remarks>
/// <param name="BuildTemplate">The selected build template, or <see langword="null"/>.</param>
/// <param name="JobTemplate">The effective job template.</param>
/// <param name="Parameterization">The effective parameterization template, or <see langword="null"/>.</param>
/// <param name="ImplicitDefaultParameterization">Whether the implicit default parameterization applies.</param>
/// <param name="OutputName">The effective output-name stem, or <see langword="null"/> when the default applies.</param>
public sealed record BuildSelection(
    BuildTemplate? BuildTemplate,
    JobTemplate JobTemplate,
    ParameterizationTemplate? Parameterization,
    bool ImplicitDefaultParameterization,
    string? OutputName);

/// <summary>
/// The outcome of resolving a build request against a project.
/// </summary>
/// <param name="Selection">The effective selection, or <see langword="null"/> on failure.</param>
/// <param name="Diagnostics">The reference diagnostics; empty for an ambiguity or wrong-kind selection.</param>
public sealed record BuildSelectionResolveResult(
    BuildSelection? Selection,
    ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>Gets a value indicating whether a selection was resolved.</summary>
    public bool IsSuccess => Selection is not null;
}
