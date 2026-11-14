// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Project;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationPersistenceTests
{
    [Fact]
    public async Task CheckedInCeriaTemplateProjectRoundTripsThroughTheStore()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-parameterization-");
        try
        {
            var store = new ProjectStore();
            var loaded = await store.LoadAsync(
                RepositoryPaths.Combine("samples", "parameterizations", "ceria-parameters.mocprj"));
            Assert.True(loaded.IsSuccess, Describe(loaded.Diagnostics));

            var template = loaded.Project!.Parameterizations.Single();
            Assert.Equal("Ceria parameters", template.Name);
            Assert.Equal(12, template.StablePairSets.Length);
            Assert.Equal(24, template.UnstablePairSets.Length);
            Assert.Single(template.GroupSets);
            Assert.Single(template.SiteEnergyOverrides);
            Assert.Empty(template.TransitionRuleSets);

            var target = Path.Combine(directory.FullName, "ceria-parameters.mocprj");
            var saved = await store.SaveAsync(loaded.Project, target);
            Assert.True(saved.IsSuccess, Describe(saved.Diagnostics));

            var reloaded = await store.LoadAsync(target);
            Assert.True(reloaded.IsSuccess, Describe(reloaded.Diagnostics));
            Assert.Equal(
                ParameterizationJson(loaded.Project!),
                ParameterizationJson(reloaded.Project!));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CheckedInBariumTemplateUsesOneSharedChiralValuePerSet()
    {
        var store = new ProjectStore();
        var loaded = await store.LoadAsync(
            RepositoryPaths.Combine(
                "samples",
                "parameterizations",
                "barium-zirconate-parameters.mocprj"));
        Assert.True(loaded.IsSuccess, Describe(loaded.Diagnostics));

        var template = loaded.Project!.Parameterizations.Single();
        var chiralSets = template.StablePairSets.Where(static set => set.IsChiral).ToArray();
        Assert.Equal(12, chiralSets.Length);
        Assert.All(
            chiralSets,
            static set => Assert.All(
                set.Entries,
                static entry => Assert.True(entry.CenterParticleId.Value.CompareTo(entry.PartnerParticleId.Value) <= 0)));
        Assert.All(template.StablePairSets, static set => Assert.Single(set.Entries));
    }

    [Fact]
    public async Task MissingTemplateValueArraysLoadAsEmpty()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-parameterization-");
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(
                RepositoryPaths.Combine("tests", "contracts", "project-parameterizations-populated.mocprj")))!.AsObject();
            var template = root["parameterizations"]![0]!.AsObject();
            template.Remove("stablePairSets");
            template.Remove("unstablePairSets");
            template.Remove("groupSets");
            template.Remove("siteEnergyOverrides");
            template.Remove("transitionRuleSets");

            var path = Path.Combine(directory.FullName, "missing-arrays.mocprj");
            File.WriteAllText(path, root.ToJsonString());
            var loaded = await new ProjectStore().LoadAsync(path);

            Assert.True(loaded.IsSuccess, Describe(loaded.Diagnostics));
            var loadedTemplate = loaded.Project!.Parameterizations.Single();
            Assert.Empty(loadedTemplate.StablePairSets);
            Assert.Empty(loadedTemplate.UnstablePairSets);
            Assert.Empty(loadedTemplate.GroupSets);
            Assert.Empty(loadedTemplate.SiteEnergyOverrides);
            Assert.Empty(loadedTemplate.TransitionRuleSets);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task UnknownTemplateFieldFailsClosedOnLoad()
    {
        var directory = Directory.CreateTempSubdirectory("mocassin-parameterization-");
        try
        {
            var root = JsonNode.Parse(File.ReadAllText(
                RepositoryPaths.Combine("tests", "contracts", "project-parameterizations-populated.mocprj")))!.AsObject();
            root["parameterizations"]![0]!["unexpected"] = true;

            var path = Path.Combine(directory.FullName, "unknown-field.mocprj");
            File.WriteAllText(path, root.ToJsonString());
            var loaded = await new ProjectStore().LoadAsync(path);

            Assert.False(loaded.IsSuccess);
            Assert.Null(loaded.Project);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string ParameterizationJson(ProjectModel project)
    {
        using var document = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(project));
        return document.RootElement.GetProperty("parameterizations").GetRawText();
    }

    private static string Describe(System.Collections.Immutable.ImmutableArray<Mocassin.Core.Diagnostics.Diagnostic> diagnostics) =>
        string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString()));
}
