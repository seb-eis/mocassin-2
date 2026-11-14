// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;

namespace Mocassin.Core.Editing;

/// <summary>
/// Result of one pure parameterization-template edit.
/// </summary>
public sealed record ParameterizationTemplateEditResult
{
    /// <summary>
    /// Initializes an edit result.
    /// </summary>
    /// <param name="template">The resulting template, or the unchanged input when the edit could not be applied.</param>
    /// <param name="diagnostics">Diagnostics produced by the edit and the structural validation of the candidate.</param>
    public ParameterizationTemplateEditResult(
        ParameterizationTemplate template,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the resulting template, or the unchanged input when the edit could not be applied.
    /// </summary>
    public ParameterizationTemplate Template { get; }

    /// <summary>
    /// Gets the diagnostics produced by the edit and the structural validation of the candidate.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the edit produced no errors.
    /// </summary>
    public bool IsSuccess => !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
