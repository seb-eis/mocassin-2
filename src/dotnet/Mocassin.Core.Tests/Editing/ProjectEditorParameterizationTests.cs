// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditorParameterizationTests
{
    [Fact]
    public void AddUpdateAndRemoveParameterizationTemplate()
    {
        var editor = new ProjectEditor();
        var project = ParameterizationTestModels.CreateProject(
            ImmutableArray<Mocassin.Core.Model.ParameterizationTemplate>.Empty);

        var added = editor.AddParameterizationTemplate(
            project,
            ParameterizationTestModels.CreatePopulatedTemplate());
        Assert.True(added.IsSuccess);
        Assert.Single(added.Project.Parameterizations);

        var updatedTemplate = added.Project.Parameterizations.Single() with { Name = "Renamed parameters" };
        var updated = editor.UpdateParameterizationTemplate(added.Project, updatedTemplate);
        Assert.True(updated.IsSuccess);
        Assert.Equal("Renamed parameters", updated.Project.Parameterizations.Single().Name);

        var removed = editor.RemoveParameterizationTemplate(
            updated.Project,
            updated.Project.Parameterizations.Single().Id);
        Assert.True(removed.IsSuccess);
        Assert.Empty(removed.Project.Parameterizations);
    }

    [Fact]
    public void DuplicateTemplateNameIsAWarningAndStillCommits()
    {
        var editor = new ProjectEditor();
        var first = ParameterizationTestModels.CreatePopulatedTemplate();
        var project = ParameterizationTestModels.CreateProject(first);
        var second = ParameterizationTestModels.CreateEmptyTemplate(
            ParameterizationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82")),
            "CERIA PARAMETERS");

        var result = editor.AddParameterizationTemplate(project, second);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Change);
        Assert.Equal(2, result.Project.Parameterizations.Length);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.DuplicateName &&
                diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void InvalidTemplateIsRejectedAndTheProjectIsUnchanged()
    {
        var editor = new ProjectEditor();
        var project = ParameterizationTestModels.CreateProject(
            ImmutableArray<Mocassin.Core.Model.ParameterizationTemplate>.Empty);
        var invalid = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    [ParameterizationTestModels.Entry(
                        ParameterizationTestModels.ParticleA,
                        ParameterizationTestModels.ParticleB,
                        101.0)]),
            ],
        };

        var result = editor.AddParameterizationTemplate(project, invalid);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Null(result.Change);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidEnergy);
    }

    [Fact]
    public void DuplicateTemplateIdIsRejected()
    {
        var editor = new ProjectEditor();
        var template = ParameterizationTestModels.CreateEmptyTemplate();
        var project = ParameterizationTestModels.CreateProject(template);
        var duplicate = template with { Name = "Other" };

        var result = editor.AddParameterizationTemplate(project, duplicate);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.DuplicateId);
    }

    [Fact]
    public void UpdatingAnUnknownTemplateReturnsAnError()
    {
        var editor = new ProjectEditor();
        var project = ParameterizationTestModels.CreateProject(
            ImmutableArray<Mocassin.Core.Model.ParameterizationTemplate>.Empty);

        var result = editor.UpdateParameterizationTemplate(
            project,
            ParameterizationTestModels.CreateEmptyTemplate());

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void RemovingAnUnknownTemplateReturnsAnError()
    {
        var editor = new ProjectEditor();
        var project = ParameterizationTestModels.CreateProject(
            ImmutableArray<Mocassin.Core.Model.ParameterizationTemplate>.Empty);

        var result = editor.RemoveParameterizationTemplate(project, ParameterizationId.New());

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void RemovingAReferencedTemplateIsRejected()
    {
        var editor = new ProjectEditor();
        var template = ParameterizationTestModels.CreatePopulatedTemplate();
        var job = new Mocassin.Core.Model.Jobs.JobTemplate(JobTemplateId.New(), "Job template");
        var build = new Mocassin.Core.Model.BuildTemplate(
            BuildTemplateId.New(),
            "Build template",
            job.Id,
            template.Id,
            "output");
        var project = new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                ParameterizationTestModels.ProjectIdValue,
                new string('a', 64),
                "Referenced parameterization",
                null,
                ParameterizationTestModels.CreatedUtc,
                ParameterizationTestModels.CreatedUtc),
            new Mocassin.Core.Model.ModelDefinition(),
            [template],
            [job],
            [build],
            Mocassin.Core.Model.ViewSettings.Default);

        var result = editor.RemoveParameterizationTemplate(project, template.Id);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference &&
                diagnostic.Path?.Property == "parameterizationId");
    }
}
