// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Text.Json;
using MessagePack;
using Mocassin.Contracts.Packages;

namespace Mocassin.Packages.Tests.Packages;

/// <summary>
/// Verifies that the generated package payload schema is the separate, manifest-driven projection of the
/// Contracts payload DTOs, and that the row schema keeps its finalized job columns.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class PackagePayloadSchemaTests
{
    private const string payloadSchemaPath = "contracts/generated/schema/package-payloads-v1.schema.json";
    private const string rowSchemaPath = "contracts/generated/schema/package-v1.schema.json";

    [Fact]
    public void PayloadSchemaIsSeparateFromTheRowSchema()
    {
        using var payloadDocument = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(payloadSchemaPath)));
        using var rowDocument = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(rowSchemaPath)));
        var payloadRoot = payloadDocument.RootElement;
        var rowRoot = rowDocument.RootElement;

        Assert.Equal(
            "https://mocassin.dev/schemas/package-payloads-v1.json",
            payloadRoot.GetProperty("$id").GetString());
        Assert.Equal(
            "https://mocassin.dev/schemas/package-v1.json",
            rowRoot.GetProperty("$id").GetString());
        Assert.Equal(
            ["job", "lattice", "model"],
            payloadRoot.GetProperty("properties").EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.Ordinal));
        Assert.Equal(
            ["job", "lattice", "model"],
            payloadRoot.GetProperty("required").EnumerateArray()
                .Select(static item => item.GetString()!)
                .OrderBy(static name => name, StringComparer.Ordinal));
        Assert.False(payloadRoot.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            ["jobs", "lattices", "models", "schema_info"],
            rowRoot.GetProperty("properties").EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void PayloadObjectsIgnoreUnknownKeysAndTheRowSchemaRejectsThem()
    {
        using var payloadDocument = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(payloadSchemaPath)));
        using var rowDocument = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(rowSchemaPath)));
        var payloadRoot = payloadDocument.RootElement;

        Assert.False(rowDocument.RootElement.GetProperty("additionalProperties").GetBoolean());
        foreach (var kind in new[] { "model", "job", "lattice" })
        {
            Assert.True(
                payloadRoot.GetProperty("properties").GetProperty(kind)
                    .GetProperty("additionalProperties").GetBoolean());
        }
    }

    [Fact]
    public void ModelPayloadSchemaMatchesTheDtoKeys()
    {
        AssertPayloadShape<ModelPayload>("model");
    }

    [Fact]
    public void JobPayloadSchemaMatchesTheDtoKeys()
    {
        AssertPayloadShape<JobPayload>("job");
    }

    [Fact]
    public void LatticePayloadSchemaMatchesTheDtoKeys()
    {
        AssertPayloadShape<LatticePayload>("lattice");
    }

    [Fact]
    public void RowSchemaKeepsWriteOutCountAndDropsTheDerivedColumns()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Combine(rowSchemaPath)));
        var jobs = document.RootElement.GetProperty("properties").GetProperty("jobs").GetProperty("properties");

        Assert.True(jobs.TryGetProperty("write_out_count", out var writeOutCount));
        Assert.Equal("integer", writeOutCount.GetProperty("type").GetString());
        Assert.Equal(0, writeOutCount.GetProperty("minimum").GetInt32());
        Assert.False(jobs.TryGetProperty("norm_factor", out _));
        Assert.False(jobs.TryGetProperty("base_frequency_hz", out _));
    }

    private static void AssertPayloadShape<TPayload>(string kind)
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(payloadSchemaPath)));
        var payload = document.RootElement.GetProperty("properties").GetProperty(kind);
        var expected = MessagePackKeysOf<TPayload>();

        Assert.Equal(
            expected,
            payload.GetProperty("properties").EnumerateObject()
                .Select(static property => property.Name)
                .OrderBy(static name => name, StringComparer.Ordinal));
        Assert.Equal(
            expected,
            payload.GetProperty("required").EnumerateArray()
                .Select(static item => item.GetString()!)
                .OrderBy(static name => name, StringComparer.Ordinal));
        Assert.True(payload.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            1,
            payload.GetProperty("properties").GetProperty("format_version").GetProperty("enum")[0].GetInt32());
    }

    private static string[] MessagePackKeysOf<TPayload>()
    {
        var properties = typeof(TPayload)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.Name != "EqualityContract")
            .ToArray();
        Assert.NotEmpty(properties);
        return properties
            .Select(static property =>
                property.GetCustomAttribute<KeyAttribute>()?.StringKey
                ?? throw new InvalidOperationException(
                    $"{property.DeclaringType?.Name}.{property.Name} has no MessagePack key."))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();
    }
}
