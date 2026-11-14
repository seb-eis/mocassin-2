// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Generates and checks the canonical parameterization projection fixtures.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationProjectionFixtureTests
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task ParameterizationProjectionFixturesAreCurrent()
    {
        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections", "parameterization");
        Directory.CreateDirectory(directory);

        foreach (var (id, sample, kind) in samples)
        {
            var project = await StageTestSupport.LoadProjectAsync(sample);
            if (kind is not null)
            {
                project = StageTestSupport.WithFirstSimulationTransition(project, kind == "kmc");
            }

            var result = new ModelCompiler().Compile(project);
            Assert.True(
                result.IsSuccess,
                string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
            var model = result.Model!;
            var values = ParameterizationTestValues.FromCompiledModel(model);
            var projection = ParameterizationSignatureV1.ComputeProjection(model.ModelSignature, values);
            var signature = ParameterizationSignatureV1.Compute(model.ModelSignature, values);
            var metadata = JsonSerializer.Serialize(
                new
                {
                    fixtureFormat = "canonical-projection-v1",
                    id,
                    descriptor = "parameterization-v1",
                    modelSignature = model.ModelSignature,
                    signatureHex = signature,
                    projectionSha256 = Convert.ToHexStringLower(SHA256.HashData(projection)),
                },
                jsonOptions) + "\n";
            var binaryPath = Path.Combine(directory, id + ".bin");
            var jsonPath = Path.Combine(directory, id + ".json");

            if (update)
            {
                await File.WriteAllBytesAsync(binaryPath, projection);
                await File.WriteAllTextAsync(jsonPath, metadata);
                continue;
            }

            Assert.True(
                File.Exists(binaryPath),
                $"Missing parameterization projection fixture {binaryPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.True(
                File.Exists(jsonPath),
                $"Missing parameterization projection metadata {jsonPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.Equal(projection, await File.ReadAllBytesAsync(binaryPath));
            Assert.Equal(metadata, await File.ReadAllTextAsync(jsonPath));
        }
    }

    [Fact]
    public void ParameterizationDescriptorIsFinalAndBindsTheSixFieldProjection()
    {
        var path = RepositoryPaths.Combine(
            "contracts",
            "generated",
            "signatures",
            "parameterization-v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal("final", root.GetProperty("status").GetString());
        Assert.Equal(
            "ParameterizationSignatureV1",
            root.GetProperty("projectionType").GetString());
        Assert.Equal(
            "mocassin/parameterization-signature/v1",
            root.GetProperty("domainPrefix").GetString());

        var fields = root.GetProperty("fields").EnumerateArray().ToArray();
        Assert.Equal(
            [
                "formatVersion",
                "modelSignature",
                "pairTables",
                "groupSets",
                "siteEnergies",
                "transitionRules",
            ],
            fields.Select(static field => field.GetProperty("name").GetString()));
        Assert.Equal(
            ["uint64", "string", "array", "array", "array", "array"],
            fields.Select(static field => field.GetProperty("type").GetString()));
        Assert.All(fields, static field => Assert.True(field.GetProperty("required").GetBoolean()));

        foreach (var index in new[] { 2, 3 })
        {
            var nested = fields[index].GetProperty("itemItems");
            Assert.Equal("array", nested.GetProperty("type").GetString());
            Assert.Equal("float64", nested.GetProperty("items").GetString());
        }

        foreach (var index in new[] { 4, 5 })
        {
            Assert.Equal("float64", fields[index].GetProperty("items").GetString());
        }
    }

    [Fact]
    public void SignatureVectorEntriesMatchTheProjectionFixtures()
    {
        var vectorsPath = RepositoryPaths.Combine("tests", "contracts", "signature-vectors.json");
        using var vectors = JsonDocument.Parse(File.ReadAllText(vectorsPath));
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections", "parameterization");
        var entries = vectors.RootElement.GetProperty("vectors")
            .EnumerateArray()
            .Where(static vector =>
                vector.GetProperty("signature").GetString() == "parameterization")
            .ToArray();

        Assert.Equal(samples.Length, entries.Length);
        foreach (var vector in entries)
        {
            var id = vector.GetProperty("id").GetString();
            var metadataPath = Path.Combine(directory, id + ".json");
            Assert.True(File.Exists(metadataPath), $"Missing fixture metadata {metadataPath}.");
            using var metadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = metadata.RootElement;
            Assert.Equal(
                vector.GetProperty("signatureHex").GetString(),
                root.GetProperty("signatureHex").GetString());
            Assert.Equal(
                vector.GetProperty("projectionSha256").GetString(),
                root.GetProperty("projectionSha256").GetString());
            Assert.Equal(
                vector.GetProperty("modelSignature").GetString(),
                root.GetProperty("modelSignature").GetString());
        }
    }

    [Fact]
    public void ModelProjectionGlobDoesNotConsumeParameterizationFixtures()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var projections = RepositoryPaths.Combine("tests", "contracts", "projections");
        var parameterization = Path.Combine(projections, "parameterization");
        Assert.True(Directory.Exists(parameterization));

        var modelBinaries = Directory.GetFiles(projections, "*.bin");
        Assert.NotEmpty(modelBinaries);
        Assert.All(modelBinaries, static path => AssertDescriptor(path, "model-v1"));

        var parameterizationBinaries = Directory.GetFiles(parameterization, "*.bin");
        Assert.NotEmpty(parameterizationBinaries);
        Assert.All(parameterizationBinaries, static path => AssertDescriptor(path, "parameterization-v1"));
    }

    private static void AssertDescriptor(string binaryPath, string descriptor)
    {
        var metadataPath = Path.ChangeExtension(binaryPath, ".json");
        Assert.True(File.Exists(metadataPath), $"Missing metadata {metadataPath}.");
        using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
        Assert.Equal(descriptor, document.RootElement.GetProperty("descriptor").GetString());
    }

    private static readonly (string Id, string Sample, string? Kind)[] samples =
    [
        ("parameterization-ceria", "samples/ceria/ceria.mocprj", null),
        ("parameterization-ceria-kmc", "samples/simulations/ceria-kmc.mocprj", "kmc"),
        ("parameterization-barium-zirconate", "samples/barium-zirconate/barium-zirconate.mocprj", null),
    ];
}
