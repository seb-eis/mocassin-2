// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Core.Project;

/// <summary>
/// Validates project-level invariants and cross-references without throwing for user data.
/// </summary>
public static class ProjectValidator
{
    /// <summary>
    /// Validates the project metadata, view settings, identifiers, and cross-references.
    /// </summary>
    /// <param name="project">The project to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var bag = new DiagnosticBag();
        ValidateMetadata(project, bag);
        ValidateTemplates(project, bag);
        ValidateReferences(project, bag);
        ValidateViewSettings(project, bag);
        return bag.Sorted;
    }

    private static void ValidateMetadata(Project project, DiagnosticBag bag)
    {
        var metadata = project.Metadata;
        var path = new ModelPath("project", metadata.ProjectId);

        if (metadata.ProjectId == Guid.Empty)
        {
            bag.Add(Error(ProjectDiagnosticCodes.InvalidMetadata, "Project ID must not be empty.", path));
        }

        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            bag.Add(Error(ProjectDiagnosticCodes.InvalidMetadata, "Project name must not be empty.", path));
        }

        if (!ProjectMetadata.IsValidMasterSeed(metadata.MasterSeed))
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidMetadata,
                "Master seed must be a 32-byte lowercase hexadecimal value.",
                new ModelPath("project", metadata.ProjectId, "masterSeed")));
        }

        if (metadata.ModifiedUtc < metadata.CreatedUtc)
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidMetadata,
                "Modified timestamp must not be earlier than the created timestamp.",
                new ModelPath("project", metadata.ProjectId, "modifiedUtc")));
        }
    }

    private static void ValidateTemplates(Project project, DiagnosticBag bag)
    {
        var parameterizationIds = CollectIds(
            project.Parameterizations.Select(static item => item.Id.Value),
            "parameterization",
            bag);
        var jobTemplateIds = CollectIds(
            project.JobTemplates.Select(static item => item.Id.Value),
            "jobTemplate",
            bag);
        var buildTemplateIds = CollectIds(
            project.BuildTemplates.Select(static item => item.Id.Value),
            "buildTemplate",
            bag);

        foreach (var template in project.Parameterizations)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.InvalidMetadata,
                    "Parameterization template name must not be empty.",
                    new ModelPath("parameterization", template.Id.Value, "name")));
            }

            if (string.IsNullOrWhiteSpace(template.ModelSignature))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.InvalidMetadata,
                    "Parameterization template must declare a model signature.",
                    new ModelPath("parameterization", template.Id.Value, "modelSignature")));
            }
        }

        foreach (var template in project.JobTemplates)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.InvalidMetadata,
                    "Job template name must not be empty.",
                    new ModelPath("jobTemplate", template.Id.Value, "name")));
            }
        }

        foreach (var template in project.BuildTemplates)
        {
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.InvalidMetadata,
                    "Build template name must not be empty.",
                    new ModelPath("buildTemplate", template.Id.Value, "name")));
            }

            if (string.IsNullOrWhiteSpace(template.OutputName))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.InvalidMetadata,
                    "Build template output name must not be empty.",
                    new ModelPath("buildTemplate", template.Id.Value, "outputName")));
            }

            ValidateReference(
                template.JobTemplateId.Value,
                "jobTemplate",
                jobTemplateIds,
                parameterizationIds,
                buildTemplateIds,
                new ModelPath("buildTemplate", template.Id.Value, "jobTemplateId"),
                bag);
            ValidateReference(
                template.ParameterizationId.Value,
                "parameterization",
                parameterizationIds,
                jobTemplateIds,
                buildTemplateIds,
                new ModelPath("buildTemplate", template.Id.Value, "parameterizationId"),
                bag);
        }
    }

    private static void ValidateReferences(Project project, DiagnosticBag bag)
    {
        var all = new Dictionary<Guid, string>();
        AddDomain(all, project.Parameterizations.Select(static item => item.Id.Value), "parameterization", bag);
        AddDomain(all, project.JobTemplates.Select(static item => item.Id.Value), "jobTemplate", bag);
        AddDomain(all, project.BuildTemplates.Select(static item => item.Id.Value), "buildTemplate", bag);
    }

    private static void ValidateViewSettings(Project project, DiagnosticBag bag)
    {
        var view = project.ViewSettings;
        var path = new ModelPath("project", project.Metadata.ProjectId, "viewSettings");

        ValidateVector(view.Camera.Position, "camera.position", path, bag);
        ValidateVector(view.Camera.Target, "camera.target", path, bag);
        ValidateVector(view.Camera.Up, "camera.up", path, bag);
        ValidateVector(view.RenderBox.Min, "renderBox.min", path, bag);
        ValidateVector(view.RenderBox.Max, "renderBox.max", path, bag);

        if (!double.IsFinite(view.Camera.FieldOfViewDegrees) ||
            view.Camera.FieldOfViewDegrees <= 0.0 ||
            view.Camera.FieldOfViewDegrees >= 180.0)
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidViewSettings,
                "Camera field of view must be finite and between 0 and 180 degrees.",
                new ModelPath("project", project.Metadata.ProjectId, "viewSettings.camera.fieldOfViewDegrees")));
        }

        if (!double.IsFinite(view.Camera.NearPlane) || view.Camera.NearPlane <= 0.0)
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidViewSettings,
                "Camera near plane must be finite and positive.",
                new ModelPath("project", project.Metadata.ProjectId, "viewSettings.camera.nearPlane")));
        }

        if (!double.IsFinite(view.Camera.FarPlane) || view.Camera.FarPlane <= view.Camera.NearPlane)
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidViewSettings,
                "Camera far plane must be finite and greater than the near plane.",
                new ModelPath("project", project.Metadata.ProjectId, "viewSettings.camera.farPlane")));
        }

        var styleIds = new HashSet<Guid>();
        foreach (var style in view.ObjectStyles)
        {
            if (!styleIds.Add(style.ObjectId))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.DuplicateId,
                    $"Object style for {style.ObjectId:D} is defined more than once.",
                    new ModelPath("objectStyle", style.ObjectId)));
            }

            var stylePath = new ModelPath("objectStyle", style.ObjectId, "additionalProperties");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in style.AdditionalProperties)
            {
                if (!keys.Add(property.Key))
                {
                    bag.Add(Error(
                        ProjectDiagnosticCodes.InvalidViewSettings,
                        $"Additional style property '{property.Key}' is defined more than once.",
                        stylePath));
                }
            }
        }
    }

    private static void ValidateVector(
        ImmutableArray<double> vector,
        string property,
        ModelPath projectPath,
        DiagnosticBag bag)
    {
        if (vector.Length != 3 || vector.Any(static value => !double.IsFinite(value)))
        {
            bag.Add(Error(
                ProjectDiagnosticCodes.InvalidViewSettings,
                $"{property} must contain three finite numbers.",
                new ModelPath(projectPath.Domain, projectPath.ObjectId, $"viewSettings.{property}")));
        }
    }

    private static HashSet<Guid> CollectIds(IEnumerable<Guid> ids, string domain, DiagnosticBag bag)
    {
        var seen = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (!seen.Add(id))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.DuplicateId,
                    $"Identifier {id:D} is used more than once in {domain}.",
                    new ModelPath(domain, id)));
            }
        }

        return seen;
    }

    private static void AddDomain(
        Dictionary<Guid, string> all,
        IEnumerable<Guid> ids,
        string domain,
        DiagnosticBag bag)
    {
        foreach (var id in ids)
        {
            if (all.TryGetValue(id, out var existing))
            {
                bag.Add(Error(
                    ProjectDiagnosticCodes.DuplicateId,
                    $"Identifier {id:D} is used in both {existing} and {domain}.",
                    new ModelPath(domain, id)));
                continue;
            }

            all.Add(id, domain);
        }
    }

    private static void ValidateReference(
        Guid id,
        string expectedDomain,
        HashSet<Guid> expectedIds,
        HashSet<Guid> otherDomainOne,
        HashSet<Guid> otherDomainTwo,
        ModelPath path,
        DiagnosticBag bag)
    {
        if (expectedIds.Contains(id))
        {
            return;
        }

        var wrongDomain = otherDomainOne.Contains(id) || otherDomainTwo.Contains(id);
        bag.Add(Error(
            wrongDomain ? ProjectDiagnosticCodes.WrongDomainReference : ProjectDiagnosticCodes.UnknownReference,
            wrongDomain
                ? $"Reference {id:D} points to an object outside the {expectedDomain} domain."
                : $"Reference {id:D} does not exist in the {expectedDomain} domain.",
            path));
    }

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path);
}
