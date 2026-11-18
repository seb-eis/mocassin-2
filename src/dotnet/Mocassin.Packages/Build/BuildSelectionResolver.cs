// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Build;

/// <summary>
/// Resolves the effective build template, job template, and parameterization of a build request.
/// </summary>
/// <remarks>
/// Precedence: an explicit build template, then the job template from the build template or
/// <see cref="BuildRequest.JobTemplateId"/>, then exactly one project job template. The parameterization comes
/// from <see cref="BuildRequest.ParameterizationId"/>, then the build template, then exactly one project
/// template, otherwise the Step 09 implicit default when the project has none. Unknown references carry a
/// <c>PROJ010</c> diagnostic; an ambiguity or a wrong-kind id carries none, because the CLI composes
/// <c>CLI004</c> and the GUI reports its own message.
/// </remarks>
public static class BuildSelectionResolver
{
    /// <summary>
    /// Resolves the effective selection.
    /// </summary>
    /// <param name="project">The loaded project.</param>
    /// <param name="request">The build request.</param>
    /// <returns>The selection or its reference diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="project"/> or <paramref name="request"/> is <see langword="null"/>.</exception>
    public static BuildSelectionResolveResult Resolve(Project project, BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new DiagnosticBag();
        var buildTemplates = project.BuildTemplates.IsDefault
            ? ImmutableArray<BuildTemplate>.Empty
            : project.BuildTemplates.Where(static template => template is not null).ToImmutableArray();
        var jobTemplates = project.JobTemplates.IsDefault
            ? ImmutableArray<JobTemplate>.Empty
            : project.JobTemplates.Where(static template => template is not null).ToImmutableArray();
        var parameterizations = project.Parameterizations.IsDefault
            ? ImmutableArray<ParameterizationTemplate>.Empty
            : project.Parameterizations.Where(static template => template is not null).ToImmutableArray();

        BuildTemplate? buildTemplate = null;
        if (request.BuildTemplateId is { } buildTemplateId)
        {
            buildTemplate = buildTemplates.FirstOrDefault(template => template.Id == buildTemplateId);
            if (buildTemplate is null)
            {
                var wrongKind = jobTemplates.Any(template => template.Id.Value == buildTemplateId.Value);
                if (!wrongKind)
                {
                    diagnostics.Add(UnknownReference(
                        project,
                        $"The project does not contain the build template '{buildTemplateId.Value:D}'.",
                        "buildTemplates"));
                }

                return new BuildSelectionResolveResult(null, diagnostics.Sorted);
            }
        }
        else if (buildTemplates.Length == 1)
        {
            buildTemplate = buildTemplates[0];
        }
        else if (buildTemplates.Length > 1)
        {
            return new BuildSelectionResolveResult(null, diagnostics.Sorted);
        }

        JobTemplate? jobTemplate;
        if (request.JobTemplateId is { } jobTemplateId)
        {
            jobTemplate = jobTemplates.FirstOrDefault(template => template.Id == jobTemplateId);
            if (jobTemplate is null)
            {
                diagnostics.Add(UnknownReference(
                    project,
                    $"The project does not contain the job template '{jobTemplateId.Value:D}'.",
                    "jobTemplates"));
                return new BuildSelectionResolveResult(null, diagnostics.Sorted);
            }
        }
        else if (buildTemplate is not null)
        {
            jobTemplate = jobTemplates.FirstOrDefault(template => template.Id == buildTemplate.JobTemplateId);
            if (jobTemplate is null)
            {
                diagnostics.Add(UnknownReference(
                    project,
                    $"The build template '{buildTemplate.Name}' references the missing job template " +
                    $"'{buildTemplate.JobTemplateId.Value:D}'.",
                    "buildTemplates"));
                return new BuildSelectionResolveResult(null, diagnostics.Sorted);
            }
        }
        else if (jobTemplates.Length == 1)
        {
            jobTemplate = jobTemplates[0];
        }
        else
        {
            return new BuildSelectionResolveResult(null, diagnostics.Sorted);
        }

        ParameterizationTemplate? parameterization;
        if (request.ParameterizationId is { } parameterizationId)
        {
            parameterization = parameterizations.FirstOrDefault(
                template => template.Id == parameterizationId);
            if (parameterization is null)
            {
                diagnostics.Add(UnknownReference(
                    project,
                    $"The project does not contain the parameterization template '{parameterizationId.Value:D}'.",
                    "parameterizations"));
                return new BuildSelectionResolveResult(null, diagnostics.Sorted);
            }
        }
        else if (buildTemplate is not null)
        {
            parameterization = parameterizations.FirstOrDefault(
                template => template.Id == buildTemplate.ParameterizationId);
            if (parameterization is null)
            {
                diagnostics.Add(UnknownReference(
                    project,
                    $"The build template '{buildTemplate.Name}' references the missing parameterization template " +
                    $"'{buildTemplate.ParameterizationId.Value:D}'.",
                    "buildTemplates"));
                return new BuildSelectionResolveResult(null, diagnostics.Sorted);
            }
        }
        else if (parameterizations.Length == 1)
        {
            parameterization = parameterizations[0];
        }
        else if (parameterizations.Length == 0)
        {
            parameterization = null;
        }
        else
        {
            return new BuildSelectionResolveResult(null, diagnostics.Sorted);
        }

        var selection = new BuildSelection(
            buildTemplate,
            jobTemplate,
            parameterization,
            ImplicitDefaultParameterization: parameterization is null,
            buildTemplate?.OutputName);
        return new BuildSelectionResolveResult(selection, diagnostics.Sorted);
    }

    private static Diagnostic UnknownReference(Project project, string message, string property) =>
        new(
            ProjectDiagnosticCodes.UnknownReference,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("project", project.Metadata.ProjectId, property));
}
