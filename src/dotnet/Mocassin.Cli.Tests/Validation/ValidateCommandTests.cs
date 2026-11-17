// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;

namespace Mocassin.Cli.Tests.Validation;

[Trait("Tier", "PR-fast")]
public sealed class ValidateCommandTests
{
    [Fact]
    public async Task CeriaJsonReportSatisfiesTheValidationSchema()
    {
        var (exitCode, stdout, stderr) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj"),
            "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        AssertSchema(stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("validation", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("valid").GetBoolean());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("statistics").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("projectId").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("modelHash").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, root.GetProperty("documentHash").ValueKind);
    }

    [Fact]
    public async Task StatisticsAreOptInAndDoNotChangeTheResult()
    {
        var (plainExit, plainJson, _) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj"),
            "--json");
        var (statsExit, statsJson, _) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj"),
            "--json",
            "--stats");

        Assert.Equal(plainExit, statsExit);
        using var plain = JsonDocument.Parse(plainJson);
        using var stats = JsonDocument.Parse(statsJson);
        Assert.Equal(
            plain.RootElement.GetProperty("valid").GetBoolean(),
            stats.RootElement.GetProperty("valid").GetBoolean());
        Assert.Equal(
            plain.RootElement.GetProperty("summary").GetRawText(),
            stats.RootElement.GetProperty("summary").GetRawText());
        Assert.Equal(
            plain.RootElement.GetProperty("diagnostics").GetRawText(),
            stats.RootElement.GetProperty("diagnostics").GetRawText());
        Assert.Equal(JsonValueKind.Null, plain.RootElement.GetProperty("statistics").ValueKind);
        var statistics = stats.RootElement.GetProperty("statistics");
        Assert.Equal(JsonValueKind.Object, statistics.ValueKind);
        Assert.True(statistics.GetProperty("p1PositionCount").GetInt64() > 0);
    }

    [Fact]
    public async Task WarningsAsErrorsChangesValidityAndExitCode()
    {
        var (relaxedExit, relaxedJson, _) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj"),
            "--json");
        var (strictExit, strictJson, _) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj"),
            "--json",
            "--warnings-as-errors");

        Assert.Equal(0, relaxedExit);
        Assert.Equal(1, strictExit);
        using var relaxed = JsonDocument.Parse(relaxedJson);
        using var strict = JsonDocument.Parse(strictJson);
        Assert.True(relaxed.RootElement.GetProperty("valid").GetBoolean());
        Assert.False(strict.RootElement.GetProperty("valid").GetBoolean());
        Assert.True(strict.RootElement.GetProperty("summary").GetProperty("warnings").GetInt32() > 0);
    }

    [Fact]
    public async Task MissingFileReturnsThreeWithNullIdentity()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await Run("validate", directory.File("missing.mocprj"), "--json");

        Assert.Equal(3, exitCode);
        AssertSchema(stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Null, root.GetProperty("projectId").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("modelHash").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("documentHash").ValueKind);
        Assert.Equal(3, root.GetProperty("exitCode").GetInt32());
        Assert.False(root.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task MalformedJsonReturnsOne()
    {
        using var directory = new TempDirectory();
        var path = directory.File("malformed.mocprj");
        await File.WriteAllTextAsync(path, "{ not json");

        var (exitCode, stdout, _) = await Run("validate", path, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(1, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("projectId").ValueKind);
    }

    [Fact]
    public async Task MissingArgumentReturnsTwoAndExactlyOneReportWithCli001()
    {
        var (exitCode, stdout, _) = await Run("validate", "--json");

        Assert.Equal(2, exitCode);
        AssertSchema(stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal(2, root.GetProperty("exitCode").GetInt32());
        Assert.False(root.GetProperty("valid").GetBoolean());
        var diagnostics = root.GetProperty("diagnostics");
        Assert.Equal(1, diagnostics.GetArrayLength());
        Assert.Equal("CLI001", diagnostics[0].GetProperty("code").GetString());
        Assert.Contains("project", diagnostics[0].GetProperty("message").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownOptionReturnsTwoWithCli001AndNoReportWithoutACommand()
    {
        var (exitCode, stdout, _) = await Run("--json");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
    }

    [Fact]
    public async Task HumanOutputIsATableWithASummary()
    {
        var (exitCode, stdout, _) = await Run(
            "validate",
            RepositoryPaths.Combine("tests", "contracts", "project-simulations-populated.mocprj"));

        Assert.Equal(0, exitCode);
        Assert.Contains("SEVERITY", stdout, StringComparison.Ordinal);
        Assert.Contains("CODE", stdout, StringComparison.Ordinal);
        Assert.Contains("ENM018", stdout, StringComparison.Ordinal);
        Assert.Contains("Summary:", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: valid", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("\"schemaVersion\"", stdout, StringComparison.Ordinal);
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> Run(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);

    private static void AssertSchema(string json)
    {
        var schema = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(
                "contracts",
                "generated",
                "schema",
                "validation-v1.schema.json")));
        using var document = JsonDocument.Parse(json);
        var errors = new List<string>();
        ValidateSchema(schema.RootElement, document.RootElement, "$", errors);
        Assert.True(errors.Count == 0, string.Join(" | ", errors));
    }

    private static void ValidateSchema(JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        if (schema.TryGetProperty("type", out var type))
        {
            if (!MatchesType(type, value))
            {
                errors.Add($"{path}: expected {type.GetRawText()}, got {value.ValueKind}");
                return;
            }
        }

        if (schema.TryGetProperty("enum", out var enumValues) &&
            !enumValues.EnumerateArray().Any(item => JsonElement.DeepEquals(item, value)))
        {
            errors.Add($"{path}: value {value.GetRawText()} is not in the schema enum");
        }

        if (value.ValueKind == JsonValueKind.Object &&
            schema.TryGetProperty("properties", out var properties))
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (value.TryGetProperty(property.Name, out var child))
                {
                    ValidateSchema(property.Value, child, $"{path}.{property.Name}", errors);
                }
            }

            if (schema.TryGetProperty("required", out var required))
            {
                foreach (var name in required.EnumerateArray())
                {
                    if (!value.TryGetProperty(name.GetString()!, out _))
                    {
                        errors.Add($"{path}: missing required property {name.GetString()}");
                    }
                }
            }

            if (schema.TryGetProperty("additionalProperties", out var additional) &&
                additional.ValueKind == JsonValueKind.False)
            {
                foreach (var property in value.EnumerateObject())
                {
                    if (!properties.TryGetProperty(property.Name, out _))
                    {
                        errors.Add($"{path}: unexpected property {property.Name}");
                    }
                }
            }
        }

        if (value.ValueKind == JsonValueKind.Array &&
            schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                ValidateSchema(items, item, $"{path}[{index}]", errors);
                index++;
            }
        }
    }

    private static bool MatchesType(JsonElement type, JsonElement value)
    {
        if (type.ValueKind == JsonValueKind.Array)
        {
            return type.EnumerateArray().Any(item => MatchesType(item, value));
        }

        return type.GetString() switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "number" => value.ValueKind == JsonValueKind.Number,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => false,
        };
    }
}
