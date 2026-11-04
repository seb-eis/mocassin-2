// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// Result of applying one validated project mutation.
/// </summary>
public sealed record ProjectEditResult
{
    /// <summary>
    /// Initializes an edit result.
    /// </summary>
    /// <param name="project">The resulting project, or the unchanged input when validation failed.</param>
    /// <param name="change">The undoable change, or <see langword="null"/> when nothing changed.</param>
    /// <param name="diagnostics">Diagnostics produced while applying and validating the edit.</param>
    public ProjectEditResult(
        ProjectModel project,
        ProjectChange? change,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Change = change;
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the resulting project, or the unchanged input when validation failed.
    /// </summary>
    public ProjectModel Project { get; }

    /// <summary>
    /// Gets the undoable change, or <see langword="null"/> when nothing changed.
    /// </summary>
    public ProjectChange? Change { get; }

    /// <summary>
    /// Gets the diagnostics produced while applying and validating the edit.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the edit completed without errors.
    /// </summary>
    public bool IsSuccess => !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
