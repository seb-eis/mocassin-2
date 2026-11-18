// SPDX-License-Identifier: MIT

using System.Text.Json.Nodes;
using Mocassin.Core.Ids;
using Mocassin.Packages.Build;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildSelectionResolverTests
{
    [Fact]
    public async Task UsesTheOnlyJobTemplateAndTheImplicitDefaultParameterization()
    {
        using var test = await TestProject.CreateAsync();

        var result = BuildSelectionResolver.Resolve(test.Project, new BuildRequest { ProjectPath = test.Path });

        Assert.True(result.IsSuccess);
        var selection = result.Selection!;
        Assert.Null(selection.BuildTemplate);
        Assert.Equal(test.Project.JobTemplates[0].Id, selection.JobTemplate.Id);
        Assert.Null(selection.Parameterization);
        Assert.True(selection.ImplicitDefaultParameterization);
        Assert.Null(selection.OutputName);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task SelectsTheOnlyBuildTemplateAndItsRecipe()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(
                root,
                "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01",
                "Build",
                JobTemplateId(root),
                "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01",
                "ceria-out");
        });

        var result = BuildSelectionResolver.Resolve(test.Project, new BuildRequest { ProjectPath = test.Path });

        Assert.True(result.IsSuccess);
        var selection = result.Selection!;
        Assert.NotNull(selection.BuildTemplate);
        Assert.Equal("Build", selection.BuildTemplate.Name);
        Assert.False(selection.ImplicitDefaultParameterization);
        Assert.Equal("ceria-out", selection.OutputName);
    }

    [Fact]
    public async Task SeveralBuildTemplatesWithoutASelectionAreAnAmbiguity()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01", "First", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "one");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b02", "Second", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "two");
        });

        var result = BuildSelectionResolver.Resolve(test.Project, new BuildRequest { ProjectPath = test.Path });

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task ExplicitBuildTemplateSelectsTheRecipe()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01", "First", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "one");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b02", "Second", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "two");
        });
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            BuildTemplateId = BuildTemplateId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b02")),
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Second", result.Selection!.BuildTemplate!.Name);
    }

    [Fact]
    public async Task UnknownBuildTemplateIsAProj010Reference()
    {
        using var test = await TestProject.CreateAsync();
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            BuildTemplateId = BuildTemplateId.Create(Guid.NewGuid()),
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ010");
    }

    [Fact]
    public async Task PassingAJobTemplateIdAsTheBuildTemplateIsAWrongKindSelection()
    {
        using var test = await TestProject.CreateAsync();
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            BuildTemplateId = BuildTemplateId.Create(test.Project.JobTemplates[0].Id.Value),
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task ExplicitJobTemplateOverridesTheBuildTemplate()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01", "Build", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "out");
            AddSecondJobTemplate(root);
        });
        var second = test.Project.JobTemplates.OrderBy(static template => template.Id.Value).Last();
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            JobTemplateId = second.Id,
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(second.Id, result.Selection!.JobTemplate.Id);
    }

    [Fact]
    public async Task SeveralParameterizationsWithoutASelectionAreAnAmbiguity()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "First");
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a02", "Second");
        });

        var result = BuildSelectionResolver.Resolve(test.Project, new BuildRequest { ProjectPath = test.Path });

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task ExplicitParameterizationSelectsIt()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "First");
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a02", "Second");
        });
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            ParameterizationId = ParameterizationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a02")),
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Second", result.Selection!.Parameterization!.Name);
        Assert.False(result.Selection.ImplicitDefaultParameterization);
    }

    [Fact]
    public async Task UnknownParameterizationIsAProj010Reference()
    {
        using var test = await TestProject.CreateAsync();
        var request = new BuildRequest
        {
            ProjectPath = test.Path,
            ParameterizationId = ParameterizationId.Create(Guid.NewGuid()),
        };

        var result = BuildSelectionResolver.Resolve(test.Project, request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "PROJ010");
    }

    [Fact]
    public async Task OutputPathUsesTheProjectStemByDefault()
    {
        using var test = await TestProject.CreateAsync();

        var resolved = BuildPathResolver.TryResolve(test.Path, targetPath: null, buildTemplate: null, out var path, out var error);

        Assert.True(resolved, error);
        Assert.Equal(System.IO.Path.Combine(test.Directory, "project.msl"), path);
    }

    [Fact]
    public async Task OutputPathUsesTheBuildTemplateStemAndAppendsMsl()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01", "Build", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "named-output");
        });
        var template = test.Project.BuildTemplates[0];

        var resolved = BuildPathResolver.TryResolve(test.Path, targetPath: null, template, out var path, out var error);

        Assert.True(resolved, error);
        Assert.Equal(System.IO.Path.Combine(test.Directory, "named-output.msl"), path);
    }

    [Fact]
    public async Task OutputNameWithASeparatorOrParentReferenceIsRejected()
    {
        using var test = await TestProject.CreateAsync(root =>
        {
            AddParameterization(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "Params");
            AddBuildTemplate(root, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01", "Build", JobTemplateId(root), "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01", "../escape");
        });
        var template = test.Project.BuildTemplates[0];

        var resolved = BuildPathResolver.TryResolve(test.Path, targetPath: null, template, out var path, out var error);

        Assert.False(resolved);
        Assert.Null(path);
        Assert.NotNull(error);
    }

    [Fact]
    public void ExplicitTargetPathWins()
    {
        var target = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "explicit.msl");

        var resolved = BuildPathResolver.TryResolve("project.mocprj", target, buildTemplate: null, out var path, out var error);

        Assert.True(resolved, error);
        Assert.Equal(System.IO.Path.GetFullPath(target), path);
    }

    private static string JobTemplateId(JsonObject root) =>
        root["jobTemplates"]!.AsArray()[0]!["id"]!.GetValue<string>();

    private static void AddParameterization(JsonObject root, string id, string name)
    {
        var array = root["parameterizations"] as JsonArray ?? [];
        root["parameterizations"] = array;
        array.Add(new JsonObject
        {
            ["id"] = id,
            ["name"] = name,
            ["modelSignature"] = new string('0', 64),
            ["createdUtc"] = "2026-11-17T12:00:00.0000000Z",
            ["stablePairSets"] = new JsonArray(),
            ["unstablePairSets"] = new JsonArray(),
            ["groupSets"] = new JsonArray(),
            ["siteEnergyOverrides"] = new JsonArray(),
            ["transitionRuleSets"] = new JsonArray(),
        });
    }

    private static void AddBuildTemplate(
        JsonObject root,
        string id,
        string name,
        string jobTemplateId,
        string parameterizationId,
        string outputName)
    {
        var array = root["buildTemplates"] as JsonArray ?? [];
        root["buildTemplates"] = array;
        array.Add(new JsonObject
        {
            ["id"] = id,
            ["name"] = name,
            ["jobTemplateId"] = jobTemplateId,
            ["parameterizationId"] = parameterizationId,
            ["outputName"] = outputName,
        });
    }

    private static void AddSecondJobTemplate(JsonObject root)
    {
        var templates = root["jobTemplates"]!.AsArray();
        var second = JsonNode.Parse(templates[0]!.ToJsonString())!.AsObject();
        second["id"] = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c99";
        second["name"] = "Second job template";
        templates.Add(second);
    }
}
