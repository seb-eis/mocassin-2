// SPDX-License-Identifier: MIT

using System.Text.Json;
using Json.Schema;
using Mocassin.Core.Project;

namespace Mocassin.Core.Tests;

public sealed class ProjectSchemaTests
{
    private static readonly string[] fixturePaths =
    [
        "tests/contracts/project-minimal.mocprj",
        "tests/contracts/project-ceria.mocprj",
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
    public void SampleAndContractFixtureAreIdentical(string samplePath, string fixturePath)
    {
        ArgumentNullException.ThrowIfNull(samplePath);
        ArgumentNullException.ThrowIfNull(fixturePath);
        Assert.Equal(
            File.ReadAllBytes(RepositoryPaths.Combine(samplePath.Split('/'))),
            File.ReadAllBytes(RepositoryPaths.Combine(fixturePath.Split('/'))));
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
