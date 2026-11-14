// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Model;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Generates and checks the checked-in golden parameterization templates, applied values, and canonical
/// projection fixtures with the real compiler and apply path.
/// </summary>
/// <remarks>
/// Run with <c>MOCASSIN_UPDATE_GOLDENS=1</c> after an intentional model or template change, then update the
/// matching <c>contracts/manifest.json</c> signature vectors and regenerate the contract artifacts.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationGoldenFixtureTests
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void ParameterizationFixturesAreCurrent()
    {
        var update = string.Equals(
            Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
            "1",
            StringComparison.Ordinal);
        var projectionDirectory = RepositoryPaths.Combine("tests", "contracts", "projections", "parameterization");
        var appliedDirectory = RepositoryPaths.Combine("tests", "contracts", "parameterizations");
        Directory.CreateDirectory(projectionDirectory);
        Directory.CreateDirectory(appliedDirectory);

        foreach (var fixture in fixtures)
        {
            var project = ParameterizationTestSupport.LoadProject(fixture.Project);
            var model = ParameterizationTestSupport.Compile(project);
            var template = fixture.UseTemplate ? Assert.Single(project.Parameterizations) : null;

            var result = new ParameterizationService().Apply(project, model, template);
            Assert.True(
                result.IsSuccess,
                string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
            var applied = result.Model!;
            Assert.Equal(
                ParameterizationSignatureV1.Compute(applied.ModelSignature, applied.Values),
                applied.Signature);

            var projection = ParameterizationSignatureV1.ComputeProjection(applied.ModelSignature, applied.Values);
            var projectionMetadata = Serialize(
                new
                {
                    fixtureFormat = "canonical-projection-v1",
                    id = fixture.Id,
                    descriptor = "parameterization-v1",
                    modelSignature = applied.ModelSignature,
                    signatureHex = applied.Signature,
                    projectionSha256 = Convert.ToHexStringLower(SHA256.HashData(projection)),
                });
            var appliedValues = Serialize(
                new
                {
                    fixtureFormat = "parameterization-applied-values-v1",
                    id = fixture.Id,
                    source = fixture.Project,
                    templateName = EffectiveTemplateName(model, template),
                    modelSignature = applied.ModelSignature,
                    signatureHex = applied.Signature,
                    pairTables = applied.Values.PairTables,
                    groupSets = applied.Values.GroupSets,
                    siteEnergies = applied.Values.SiteEnergies,
                    transitionRuleFrequencies = applied.Values.TransitionRuleFrequencies,
                });

            var binaryPath = Path.Combine(projectionDirectory, fixture.Id + ".bin");
            var metadataPath = Path.Combine(projectionDirectory, fixture.Id + ".json");
            var appliedPath = Path.Combine(appliedDirectory, fixture.Id + ".json");
            if (update)
            {
                File.WriteAllBytes(binaryPath, projection);
                File.WriteAllText(metadataPath, projectionMetadata);
                File.WriteAllText(appliedPath, appliedValues);
                continue;
            }

            Assert.True(
                File.Exists(binaryPath),
                $"Missing parameterization projection fixture {binaryPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.True(
                File.Exists(metadataPath),
                $"Missing parameterization projection metadata {metadataPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.True(
                File.Exists(appliedPath),
                $"Missing golden applied values {appliedPath}; run with MOCASSIN_UPDATE_GOLDENS=1.");
            Assert.Equal(projection, File.ReadAllBytes(binaryPath));
            Assert.Equal(projectionMetadata, File.ReadAllText(metadataPath));
            Assert.Equal(appliedValues, File.ReadAllText(appliedPath));
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
        if (string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var vectorsPath = RepositoryPaths.Combine("tests", "contracts", "signature-vectors.json");
        using var vectors = JsonDocument.Parse(File.ReadAllText(vectorsPath));
        var directory = RepositoryPaths.Combine("tests", "contracts", "projections", "parameterization");
        var entries = vectors.RootElement.GetProperty("vectors")
            .EnumerateArray()
            .Where(static vector =>
                vector.GetProperty("signature").GetString() == "parameterization")
            .ToArray();

        Assert.Equal(fixtures.Length, entries.Length);
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
    public void GoldenAppliedValuesMatchTheCheckedInTemplates()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_UPDATE_GOLDENS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var directory = RepositoryPaths.Combine("tests", "contracts", "parameterizations");

        foreach (var fixture in fixtures)
        {
            var project = ParameterizationTestSupport.LoadProject(fixture.Project);
            var model = ParameterizationTestSupport.Compile(project);
            var template = fixture.UseTemplate ? Assert.Single(project.Parameterizations) : null;

            var applied = new ParameterizationService().Apply(project, model, template).Model!;
            using var document = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(directory, fixture.Id + ".json")));
            var root = document.RootElement;

            Assert.Equal(fixture.Project, root.GetProperty("source").GetString());
            Assert.Equal(EffectiveTemplateName(model, template), root.GetProperty("templateName").GetString());
            Assert.Equal(applied.ModelSignature, root.GetProperty("modelSignature").GetString());
            Assert.Equal(applied.Signature, root.GetProperty("signatureHex").GetString());
            Assert.Equal(applied.Values.SiteEnergies, ReadNumbers(root.GetProperty("siteEnergies")));
            Assert.Equal(
                applied.Values.TransitionRuleFrequencies,
                ReadNumbers(root.GetProperty("transitionRuleFrequencies")));
            Assert.Equal(
                applied.Values.PairTables.Select(static table => table.ToArray()).ToArray(),
                ReadNestedNumbers(root.GetProperty("pairTables")));
            Assert.Equal(
                applied.Values.GroupSets.Select(static table => table.ToArray()).ToArray(),
                ReadNestedNumbers(root.GetProperty("groupSets")));
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
        Assert.Equal(fixtures.Length, parameterizationBinaries.Length);
        Assert.All(parameterizationBinaries, static path => AssertDescriptor(path, "parameterization-v1"));
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, jsonOptions) + "\n";

    private static string EffectiveTemplateName(CompiledModel model, ParameterizationTemplate? template) =>
        template?.Name ?? ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature).Name;

    private static void AssertDescriptor(string binaryPath, string descriptor)
    {
        var metadataPath = Path.ChangeExtension(binaryPath, ".json");
        Assert.True(File.Exists(metadataPath), $"Missing metadata {metadataPath}.");
        using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
        Assert.Equal(descriptor, document.RootElement.GetProperty("descriptor").GetString());
    }

    private static double[] ReadNumbers(JsonElement element) =>
        [.. element.EnumerateArray().Select(static item => item.GetDouble())];

    private static double[][] ReadNestedNumbers(JsonElement element) =>
        [.. element.EnumerateArray().Select(ReadNumbers)];

    private static readonly (string Id, string Project, bool UseTemplate)[] fixtures =
    [
        ("parameterization-ceria", ParameterizationTestSupport.CeriaParameters, true),
        ("parameterization-ceria-kmc", ParameterizationTestSupport.CeriaKmc, false),
        ("parameterization-barium-zirconate", ParameterizationTestSupport.BariumZirconateParameters, true),
        ("parameterization-chiral-p1", "tests/contracts/project-chiral-p1.mocprj", true),
    ];
}
