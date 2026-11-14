// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobTemplatePersistenceTests
{
    private static readonly DateTimeOffset timestamp = DateTimeOffset.Parse(
        "2026-11-14T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void CanonicalJobTemplateSortsExtensionsAndOmitsEmptyCollections()
    {
        var project = CreateProject(CreateTemplate());

        var root = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(project)).RootElement;
        var template = root.GetProperty("jobTemplates")[0];
        var collection = template.GetProperty("collections")[0];

        Assert.Equal(
            ["a", "z"],
            collection.GetProperty("baseSettings").GetProperty("extensions")
                .EnumerateArray()
                .Select(static extension => extension.GetProperty("id").GetString()));
        Assert.Equal(
            "{\"a\":1,\"b\":2}",
            collection.GetProperty("baseSettings").GetProperty("extensions")[1]
                .GetProperty("config").GetRawText());
        Assert.Equal(
            [Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e81"), Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82")],
            collection.GetProperty("configs")[0].GetProperty("dopings")
                .EnumerateArray()
                .Select(static doping => Guid.Parse(doping.GetProperty("dopingId").GetString()!)));

        var empty = CreateProject(new JobTemplate(JobTemplateId.New(), "Empty"));
        var emptyTemplate = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(empty))
            .RootElement.GetProperty("jobTemplates")[0];
        Assert.False(emptyTemplate.TryGetProperty("collections", out _));
    }

    [Fact]
    public void RoundTripPreservesJobTemplateSettingsAndDopings()
    {
        var project = CreateProject(CreateTemplate());
        var json = ProjectJson.Serialize(project);
        var loaded = ProjectJson.Deserialize(json);
        var template = loaded.JobTemplates.Single();
        var collection = template.Collections!.Value.Single();
        var config = collection.Configs!.Value.Single();

        Assert.Equal("KMC temperature series", template.Name);
        Assert.Equal(773.0, collection.BaseSettings!.TemperatureK);
        Assert.Equal(4, collection.JobCountPerConfig);
        Assert.Equal("collection-seed", collection.RngSeed);
        Assert.Equal(10, config.Lattice.A);
        Assert.Equal(200, config.Overrides!.TargetMcsp);
        Assert.Equal(2, collection.Optimizers!.Value.Length);
        Assert.Equal(2, config.Dopings!.Value.Length);
        Assert.Equal("a", collection.BaseSettings.Extensions!.Value[0].Id);
        Assert.Equal("z", collection.BaseSettings.Extensions.Value[1].Id);
    }

    [Fact]
    public void CanonicalDocumentIsIndependentOfTemplateAndExtensionInputOrder()
    {
        var first = CreateProject(CreateTemplate());
        var second = CreateProject(CreateTemplate(reverse: true));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(first),
            ProjectCanonicalizer.ToCanonicalJson(second));
    }

    [Fact]
    public void ChangingExtensionConfigChangesDocumentHash()
    {
        var project = CreateProject(CreateTemplate());
        var template = project.JobTemplates.Single();
        var collection = template.Collections!.Value.Single();
        var settings = collection.BaseSettings!;
        var changedSettings = settings with
        {
            Extensions =
            [
                new JobExtension("a", JsonDocument.Parse("{\"c\":30,\"d\":40}").RootElement.Clone()),
                new JobExtension("z", JsonDocument.Parse("{\"a\":1,\"b\":2}").RootElement.Clone()),
            ],
        };
        var changed = project with
        {
            JobTemplates =
            [
                template with
                {
                    Collections = [collection with { BaseSettings = changedSettings }],
                },
            ],
        };

        Assert.NotEqual(ProjectHasher.ComputeDocument(project), ProjectHasher.ComputeDocument(changed));
    }

    [Fact]
    public void UnknownJobTemplatePropertyIsRejected()
    {
        var json = ProjectJson.Serialize(CreateProject(CreateTemplate()));
        var root = JsonNode.Parse(json)!.AsObject();
        root["jobTemplates"]!.AsArray()[0]!.AsObject()["unexpected"] = 1;

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    private static ProjectModel CreateProject(JobTemplate template) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "Job template fixture",
                null,
                timestamp,
                timestamp),
            ModelDefinition.Empty,
            [],
            [template],
            [],
            ViewSettings.Default);

    private static JobTemplate CreateTemplate(bool reverse = false)
    {
        var firstDoping = new DopingValue(
            DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e81")),
            0.01);
        var secondDoping = new DopingValue(
            DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82")),
            0.02);
        var firstExtension = new JobExtension(
            "z",
            JsonDocument.Parse("{\"b\":2,\"a\":1.0}").RootElement.Clone());
        var secondExtension = new JobExtension(
            "a",
            JsonDocument.Parse("{\"d\":4,\"c\":3}").RootElement.Clone());
        var optimizers = new[]
        {
            new SelectionOptimizer(CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83")), ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e84"))),
            new SelectionOptimizer(CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e85")), ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e86"))),
        };
        var extensions = reverse
            ? ImmutableArray.Create(firstExtension, secondExtension)
            : ImmutableArray.Create(secondExtension, firstExtension);
        var settings = new JobSettings
        {
            TemperatureK = 773.0,
            TargetMcsp = 200,
            Extensions = extensions,
        };
        var dopings = reverse
            ? ImmutableArray.Create(firstDoping, secondDoping)
            : ImmutableArray.Create(secondDoping, firstDoping);
        var config = new JobConfig(
            JobConfigId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e87")),
            "x001",
            new LatticeRequest(10, 10, 10),
            new JobSettings { TargetMcsp = 200 },
            dopings);
        var collection = new JobCollection(
            JobCollectionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e88")),
            "T773",
            SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e89")),
            settings,
            4,
            "collection-seed",
            [.. optimizers],
            [config]);

        return new JobTemplate(
            JobTemplateId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e71")),
            "KMC temperature series")
        {
            Collections = [collection],
        };
    }
}
