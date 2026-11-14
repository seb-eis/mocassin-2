// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// Adds the parameterization-template mutations to the project editor.
/// </summary>
public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds a parameterization template.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="template">The template to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddParameterizationTemplate(ProjectModel project, ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(template);
        return Apply(project, "Add parameterization template", current => current with
        {
            Parameterizations = Append(current.Parameterizations, template),
        });
    }

    /// <summary>
    /// Updates a parameterization template by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="template">The replacement template.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateParameterizationTemplate(ProjectModel project, ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(template);
        if (!Contains(
                project.Parameterizations,
                candidate => candidate is not null && candidate.Id == template.Id))
        {
            return Unknown(
                project,
                "parameterization",
                template.Id.Value,
                $"Parameterization template '{template.Id:D}' does not exist.");
        }

        return Apply(project, "Update parameterization template", current =>
        {
            return TryReplace(
                current.Parameterizations,
                candidate => candidate is not null && candidate.Id == template.Id,
                template,
                out var templates)
                ? current with { Parameterizations = templates }
                : current;
        });
    }

    /// <summary>
    /// Removes a parameterization template by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The template identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveParameterizationTemplate(ProjectModel project, ParameterizationId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Parameterizations, candidate => candidate is not null && candidate.Id == id))
        {
            return Unknown(
                project,
                "parameterization",
                id.Value,
                $"Parameterization template '{id:D}' does not exist.");
        }

        return Apply(project, "Remove parameterization template", current => current with
        {
            Parameterizations = RemoveWhere(
                current.Parameterizations,
                candidate => candidate is not null && candidate.Id == id),
        });
    }
}
