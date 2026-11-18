// SPDX-License-Identifier: MIT

using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Packages.Tests.Projects.Templates;

[Trait("Tier", "PR-fast")]
public sealed class ProjectTemplateTests
{
    [Fact]
    public void BothStartersAreEmbedded()
    {
        Assert.Equal(
            [ProjectTemplateKind.Blank, ProjectTemplateKind.Ceria],
            ProjectTemplateFactory.All.Select(static template => template.Kind));
        Assert.Equal("Blank", ProjectTemplateFactory.Get(ProjectTemplateKind.Blank).DisplayName);
        Assert.Contains("Ceria", ProjectTemplateFactory.Get(ProjectTemplateKind.Ceria).DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmbeddedCeriaModelMatchesTheAuthoredSample()
    {
        var samplePath = RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj");
        var sample = ProjectJson.Deserialize(await File.ReadAllTextAsync(samplePath));
        var embedded = ProjectTemplateFactory.Get(ProjectTemplateKind.Ceria).Project;

        Assert.Equal(ProjectHasher.Compute(sample), ProjectHasher.Compute(embedded));
    }

    [Fact]
    public void BlankTemplateIsAnEmptyValidProject()
    {
        var blank = ProjectTemplateFactory.Get(ProjectTemplateKind.Blank).Project;
        var validation = new ProjectValidationPipeline().Validate(
            blank,
            options: new ValidationOptions { RunBuildStage = false });

        Assert.True(validation.IsValid);
        Assert.Empty(blank.Model.Particles);
        Assert.Empty(blank.Model.Structure.Sites);
        Assert.Empty(blank.JobTemplates);
    }

    [Fact]
    public void CeriaTemplateIsAModelStarterWithoutAJobTemplate()
    {
        var ceria = ProjectTemplateFactory.Get(ProjectTemplateKind.Ceria).Project;
        var validation = new ProjectValidationPipeline().Validate(
            ceria,
            options: new ValidationOptions { RunBuildStage = false });

        Assert.True(validation.IsValid);
        Assert.NotEmpty(ceria.Model.Particles);
        Assert.Empty(ceria.JobTemplates);
    }

    [Fact]
    public void CreateProjectChangesIdentitySeedNameAndTimestampsButNotTheModel()
    {
        var template = ProjectTemplateFactory.Get(ProjectTemplateKind.Ceria).Project;
        var timestamp = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

        var first = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Ceria, "First", timestamp);
        var second = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Ceria, "Second", timestamp);

        Assert.NotEqual(template.Metadata.ProjectId, first.Metadata.ProjectId);
        Assert.NotEqual(template.Metadata.MasterSeed, first.Metadata.MasterSeed);
        Assert.NotEqual(first.Metadata.MasterSeed, second.Metadata.MasterSeed);
        Assert.Equal("First", first.Metadata.Name);
        Assert.Equal(timestamp, first.Metadata.CreatedUtc);
        Assert.Equal(timestamp, first.Metadata.ModifiedUtc);
        Assert.Equal(ProjectHasher.Compute(template), ProjectHasher.Compute(first));
        Assert.Equal(ProjectHasher.Compute(first), ProjectHasher.Compute(second));
    }

    [Fact]
    public void CreateProjectRejectsAMissingName()
    {
        var template = ProjectTemplateFactory.Get(ProjectTemplateKind.Blank);

        Assert.Throws<ArgumentNullException>(() => template.CreateProject(null!, DateTimeOffset.UnixEpoch));
    }
}
