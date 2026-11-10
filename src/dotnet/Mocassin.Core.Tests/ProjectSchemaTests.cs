// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Mocassin.Core.Project;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class ProjectSchemaTests
{
    private static readonly string[] fixturePaths =
    [
        "tests/contracts/project-minimal.mocprj",
        "tests/contracts/project-ceria.mocprj",
        "tests/contracts/project-barium-zirconate.mocprj",
        "tests/contracts/project-structure.mocprj",
        "tests/contracts/project-symmetry.mocprj",
        "tests/contracts/project-symmetry-partial.mocprj",
        "tests/contracts/project-symmetry-mismatch.mocprj",
        "tests/contracts/project-transitions-minimal.mocprj",
        "tests/contracts/project-transitions-populated.mocprj",
        "tests/contracts/project-energy-minimal.mocprj",
        "tests/contracts/project-energy-populated.mocprj",
        "samples/blank/blank.mocprj",
        "samples/ceria/ceria.mocprj",
    ];

    private static readonly JsonSchema projectSchema = JsonSchema.FromText(
        File.ReadAllText(RepositoryPaths.Combine("contracts", "generated", "schema", "project-v1.schema.json")));

    public static TheoryData<string> Fixtures => [.. fixturePaths];

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void GeneratedSchemaValidatesFixture(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        var fixturePath = RepositoryPaths.Combine(relativePath.Split('/'));
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));

        var result = projectSchema.Evaluate(document.RootElement);

        Assert.True(result.IsValid, result.ToString());
    }

    [Theory]
    [InlineData("samples/blank/blank.mocprj", "tests/contracts/project-minimal.mocprj")]
    [InlineData("samples/ceria/ceria.mocprj", "tests/contracts/project-ceria.mocprj")]
    [InlineData(
        "samples/barium-zirconate/barium-zirconate.mocprj",
        "tests/contracts/project-barium-zirconate.mocprj")]
    public void SampleAndContractFixtureAreIdentical(string samplePath, string fixturePath)
    {
        ArgumentNullException.ThrowIfNull(samplePath);
        ArgumentNullException.ThrowIfNull(fixturePath);
        Assert.Equal(
            File.ReadAllBytes(RepositoryPaths.Combine(samplePath.Split('/'))),
            File.ReadAllBytes(RepositoryPaths.Combine(fixturePath.Split('/'))));
    }

    [Fact]
    public void EnergyFixturesCoverEmptyAndPopulatedModels()
    {
        var minimal = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-energy-minimal.mocprj")));
        var populated = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-energy-populated.mocprj")));
        var minimalModel = minimal.RootElement.GetProperty("model");
        var populatedModel = populated.RootElement.GetProperty("model");

        Assert.False(minimalModel.TryGetProperty("stableEnvironment", out _));
        Assert.False(minimalModel.TryGetProperty("unstableEnvironments", out _));
        Assert.False(minimalModel.TryGetProperty("clusters", out _));
        Assert.False(minimalModel.TryGetProperty("siteEnergies", out _));

        Assert.True(populatedModel.TryGetProperty("stableEnvironment", out _));
        Assert.True(populatedModel.TryGetProperty("unstableEnvironments", out _));
        Assert.True(populatedModel.TryGetProperty("clusters", out _));
        Assert.True(populatedModel.TryGetProperty("siteEnergies", out _));
    }

    [Fact]
    public void SchemaRejectsUnknownEnergyField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-energy-populated.mocprj")))!.AsObject();
        root["model"]!["stableEnvironment"]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaRejectsUnknownTransitionField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-transitions-minimal.mocprj")))!.AsObject();
        root["model"]!["stateChanges"]![0]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaAcceptsOptionalViewFieldsMissing()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine(fixturePaths[0].Split('/'))))!.AsObject();
        var view = root["viewSettings"]!.AsObject();
        view["camera"]!.AsObject().Remove("projection");
        view["renderBox"]!.AsObject().Remove("visible");
        view.Remove("objectStyles");

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void SchemaRejectsNumericProjectionString()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine(fixturePaths[0].Split('/'))))!.AsObject();
        root["viewSettings"]!["camera"]!["projection"] = "1";

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void FixturesRoundTripThroughProjectJson()
    {
        var schemaPath = RepositoryPaths.Combine("contracts", "generated", "schema", "project-v1.schema.json");
        Assert.True(File.Exists(schemaPath));

        foreach (var relativePath in fixturePaths)
        {
            var bytes = File.ReadAllBytes(RepositoryPaths.Combine(relativePath.Split('/')));
            var project = ProjectJson.Deserialize(bytes);
            var loaded = ProjectJson.Deserialize(ProjectCanonicalizer.ToCanonicalUtf8(project));

            Assert.Equal(
                ProjectCanonicalizer.ToCanonicalJson(project),
                ProjectCanonicalizer.ToCanonicalJson(loaded));
            Assert.Empty(ProjectValidator.Validate(project));
        }
    }
}
