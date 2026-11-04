// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// Applies one mutation to an immutable project and validates the candidate before committing it.
/// </summary>
public sealed class ProjectEditor
{
    private readonly Func<ProjectModel, ImmutableArray<Diagnostic>> validator;

    /// <summary>
    /// Initializes a project editor.
    /// </summary>
    /// <param name="validator">Optional validator override. The default is <see cref="ProjectValidator.Validate"/>.</param>
    public ProjectEditor(Func<ProjectModel, ImmutableArray<Diagnostic>>? validator = null)
    {
        this.validator = validator ?? ProjectValidator.Validate;
    }

    /// <summary>
    /// Applies a mutation and returns the candidate only when validation has no errors.
    /// </summary>
    /// <param name="project">The current project value.</param>
    /// <param name="description">The user-visible change description.</param>
    /// <param name="mutation">The pure mutation that returns the candidate project.</param>
    /// <returns>The edit result. The input is returned unchanged on validation errors.</returns>
    public ProjectEditResult Apply(
        ProjectModel project,
        string description,
        Func<ProjectModel, ProjectModel> mutation)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(mutation);

        var candidate = mutation(project)
            ?? throw new InvalidOperationException("A project mutation returned null.");

        var diagnostics = validator(candidate);
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new ProjectEditResult(project, null, diagnostics);
        }

        if (ReferenceEquals(candidate, project) ||
            string.Equals(
                ProjectHasher.ComputeDocument(candidate),
                ProjectHasher.ComputeDocument(project),
                StringComparison.Ordinal))
        {
            return new ProjectEditResult(project, null, diagnostics);
        }

        return new ProjectEditResult(
            candidate,
            new ProjectChange(description, project, candidate),
            diagnostics);
    }
}
