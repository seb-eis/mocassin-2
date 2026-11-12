// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Validation;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-fast")]
public sealed class ValidationFixtureExpectationsTests
{
    [Fact]
    public void FixturesMatchTheirRecordedDiagnosticSets()
    {
        using var expectations = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("tests", "contracts", "validation-expectations.json")));
        foreach (var expectation in expectations.RootElement.EnumerateObject())
        {
            var project = ProjectJson.Deserialize(
                File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", expectation.Name)));
            var result = new ProjectValidationPipeline().Validate(project);
            var expected = expectation.Value;
            var expectedCodes = expected.GetProperty("codes")
                .EnumerateArray()
                .Select(static item => item.GetString()!)
                .ToArray();

            Assert.Equal(expected.GetProperty("valid").GetBoolean(), result.IsValid);
            Assert.Equal(expected.GetProperty("errors").GetInt32(), result.Summary.Errors);
            Assert.Equal(expectedCodes, result.Diagnostics.Select(static item => item.Code).ToArray());
        }
    }

    [Fact]
    public void CeriaIsValidWithNoDiagnostics()
    {
        var project = Load("project-ceria.mocprj");

        var result = new ProjectValidationPipeline().Validate(project);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Diagnostics.Where(static item => item.Severity == DiagnosticSeverity.Error));
    }

    private static ProjectModel Load(string name) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", name)));
}
