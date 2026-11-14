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
        "tests/contracts/project-lattice-minimal.mocprj",
        "tests/contracts/project-lattice-populated.mocprj",
        "tests/contracts/project-simulations-minimal.mocprj",
        "tests/contracts/project-simulations-populated.mocprj",
        "tests/contracts/project-parameterizations-populated.mocprj",
        "tests/contracts/project-job-templates-populated.mocprj",
        "tests/contracts/project-chiral-p4mm.mocprj",
        "tests/contracts/project-chiral-p1.mocprj",
        "samples/blank/blank.mocprj",
        "samples/ceria/ceria.mocprj",
        "samples/parameterizations/ceria-parameters.mocprj",
        "samples/parameterizations/barium-zirconate-parameters.mocprj",
        "samples/lattice/ceria-reduction.mocprj",
        "samples/lattice/ceria-hydration.mocprj",
        "samples/lattice/bzo-doping.mocprj",
        "samples/simulations/ceria-kmc.mocprj",
        "samples/simulations/ceria-mmc.mocprj",
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
    public void LatticeFixturesCoverEmptyAndPopulatedModels()
    {
        var minimal = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-lattice-minimal.mocprj")));
        var populated = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-lattice-populated.mocprj")));
        var minimalModel = minimal.RootElement.GetProperty("model");
        var populatedModel = populated.RootElement.GetProperty("model");

        Assert.False(minimalModel.TryGetProperty("occupationExchanges", out _));
        Assert.False(minimalModel.TryGetProperty("dopings", out _));

        Assert.True(populatedModel.TryGetProperty("occupationExchanges", out _));
        Assert.True(populatedModel.TryGetProperty("dopings", out _));
    }

    [Fact]
    public void SimulationFixturesCoverEmptyAndPopulatedModels()
    {
        var minimal = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-simulations-minimal.mocprj")));
        var populated = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj")));
        var minimalModel = minimal.RootElement.GetProperty("model");
        var populatedModel = populated.RootElement.GetProperty("model");

        Assert.False(minimalModel.TryGetProperty("simulations", out _));
        var simulations = populatedModel.GetProperty("simulations");
        Assert.Equal(2, simulations.GetArrayLength());
        Assert.Equal("kmc", simulations[0].GetProperty("kind").GetString());
        Assert.Equal("mmc", simulations[1].GetProperty("kind").GetString());
    }

    [Fact]
    public void SchemaRejectsUnknownLatticeField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-lattice-populated.mocprj")))!.AsObject();
        root["model"]!["occupationExchanges"]![0]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaRejectsUnknownSimulationField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj")))!.AsObject();
        root["model"]!["simulations"]![0]!["settings"]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaRejectsUnknownParameterizationField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-parameterizations-populated.mocprj")))!.AsObject();
        root["parameterizations"]![0]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaRejectsUnknownParameterizationEntryField()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("samples", "parameterizations", "ceria-parameters.mocprj")))!.AsObject();
        root["parameterizations"]![0]!["stablePairSets"]![0]!["entries"]![0]!["unexpected"] = true;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SchemaAcceptsMissingParameterizationValueArrays()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-parameterizations-populated.mocprj")))!.AsObject();
        var template = root["parameterizations"]![0]!.AsObject();
        template.Remove("stablePairSets");
        template.Remove("unstablePairSets");
        template.Remove("groupSets");
        template.Remove("siteEnergyOverrides");
        template.Remove("transitionRuleSets");

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.True(result.IsValid, result.ToString());
    }

    [Fact]
    public void ParameterizationFixturesCoverEveryValueKind()
    {
        var ceria = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("samples", "parameterizations", "ceria-parameters.mocprj")));
        var ceriaTemplate = ceria.RootElement.GetProperty("parameterizations")[0];
        Assert.True(ceriaTemplate.GetProperty("stablePairSets").GetArrayLength() > 0);
        Assert.True(ceriaTemplate.GetProperty("unstablePairSets").GetArrayLength() > 0);
        Assert.True(ceriaTemplate.GetProperty("groupSets").GetArrayLength() > 0);
        Assert.True(ceriaTemplate.GetProperty("siteEnergyOverrides").GetArrayLength() > 0);

        var barium = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(
                "samples",
                "parameterizations",
                "barium-zirconate-parameters.mocprj")));
        var bariumSets = barium.RootElement.GetProperty("parameterizations")[0].GetProperty("stablePairSets");
        Assert.True(bariumSets.GetArrayLength() > 0);
        Assert.Contains(
            bariumSets.EnumerateArray(),
            static set => set.GetProperty("isChiral").GetBoolean());

        var kmc = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(
                "tests",
                "contracts",
                "project-parameterizations-populated.mocprj")));
        var ruleSets = kmc.RootElement.GetProperty("parameterizations")[0].GetProperty("transitionRuleSets");
        Assert.True(ruleSets.GetArrayLength() > 0);
        Assert.True(ruleSets[0].GetProperty("rules").GetArrayLength() > 0);
    }

    [Fact]
    public void SchemaAcceptsNullSimulationSettings()
    {
        var root = JsonNode.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj")))!.AsObject();
        var settings = root["model"]!["simulations"]![0]!["settings"]!.AsObject();
        settings["timeLimitSeconds"] = null;
        settings["minSuccessRate"] = null;
        settings["rngSeed"] = null;
        settings["instruction"] = null;
        settings["normalizationEnergyEv"] = null;
        settings["maxAttemptFrequencyHz"] = null;

        using var document = JsonDocument.Parse(root.ToJsonString());
        var result = projectSchema.Evaluate(document.RootElement);

        Assert.True(result.IsValid, result.ToString());
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
