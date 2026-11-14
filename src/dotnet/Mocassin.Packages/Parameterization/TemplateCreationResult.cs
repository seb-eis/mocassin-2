// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// The outcome of creating a parameterization template from a compiled model.
/// </summary>
/// <param name="Template">The new template with set headers and no explicit values.</param>
/// <param name="Summary">The summary of the new template for the supplied project and compiled model.</param>
/// <param name="Diagnostics">Creation diagnostics, for example a case-insensitive duplicate template name.</param>
/// <remarks>
/// The factory never mutates the project. The caller commits the template through the project editor and
/// re-validates it there.
/// </remarks>
public sealed record TemplateCreationResult(
    ParameterizationTemplate Template,
    ParameterizationSummary Summary,
    ImmutableArray<Diagnostic> Diagnostics);
