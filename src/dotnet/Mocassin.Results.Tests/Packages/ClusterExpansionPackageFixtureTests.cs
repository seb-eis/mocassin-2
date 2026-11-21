// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using MessagePack;
using Mocassin.Contracts.Packages;
using Mocassin.Results.Packages;
using Mocassin.Results.Packages.Internal;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Reads, verifies, and regenerates the checked-in Step 11 amendment-2 cluster-expansion fixture.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterExpansionPackageFixtureTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine(ClusterExpansionPackageFixtureBuilder.DirectoryName);

    [Fact]
    public async Task FixtureVerifiesAndMatchesTheCanonicalSummary()
    {
        var path = Path.Combine(FixtureDirectory, ClusterExpansionPackageFixtureBuilder.PackageFileName);
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(1, report.Inspection.LatticeCount);
        Assert.Equal(1, report.Inspection.JobCount);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, ClusterExpansionPackageFixtureBuilder.SummaryFileName)));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The cluster-expansion fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public void FixtureHashMatchesItsProvenance()
    {
        var path = Path.Combine(FixtureDirectory, ClusterExpansionPackageFixtureBuilder.PackageFileName);
        var recorded = File.ReadAllText(
                Path.Combine(FixtureDirectory, ClusterExpansionPackageFixtureBuilder.HashFileName))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void ExtractedPayloadsMatchTheirIndex()
    {
        var payloadDirectory = Path.Combine(
            FixtureDirectory,
            ClusterExpansionPackageFixtureBuilder.PayloadDirectoryName);
        using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(payloadDirectory, "index.json")));
        var entries = index.RootElement.GetProperty("payloads").EnumerateArray().ToArray();

        Assert.Equal(3, entries.Length);
        Assert.Contains(entries, static entry => entry.GetProperty("kind").GetString() == "model");
        foreach (var entry in entries)
        {
            var path = Path.Combine(payloadDirectory, entry.GetProperty("path").GetString()!);
            var bytes = File.ReadAllBytes(path);
            Assert.Equal(entry.GetProperty("rawBytes").GetInt32(), bytes.Length);
            Assert.Equal(
                entry.GetProperty("rawSha256").GetString(),
                Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }
    }

    [Fact]
    public void ModelPayloadCarriesTheExpectedEightToSixExpansionMap()
    {
        var model = DecodeModel("step-11-amend-2");
        var table = Assert.Single(model.Energy.ClusterTables);
        var expansions = Assert.IsType<CodeExpansionPayload>(table.CodeExpansions);

        using var expectation = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, "cluster-expansions-v1.json")));
        var root = expectation.RootElement;

        Assert.Equal(
            root.GetProperty("occupationCodes").EnumerateArray().Select(static value => value.GetUInt64()),
            table.OccupationCodes);
        Assert.Equal(
            root.GetProperty("expandedCodes").EnumerateArray().Select(static value => value.GetUInt64()),
            expansions.ExpandedCodes);
        Assert.Equal(
            root.GetProperty("canonicalIndices").EnumerateArray().Select(static value => value.GetUInt32()),
            expansions.CanonicalIndices);

        Assert.Equal(8, expansions.ExpandedCodes.Length);
        Assert.Equal(6, table.OccupationCodes.Length);
        Assert.Equal(
            new ulong[] { 8322, 8323, 8386, 8387, 12418, 12419, 12482, 12483 },
            expansions.ExpandedCodes.ToArray());
        Assert.Equal(
            new uint[] { 0, 1, 2, 3, 2, 3, 4, 5 },
            expansions.CanonicalIndices.ToArray());

        foreach (var mapping in root.GetProperty("canonicalMappings").EnumerateArray())
        {
            var rawCode = mapping.GetProperty("rawCode").GetUInt64();
            var canonicalCode = mapping.GetProperty("canonicalCode").GetUInt64();
            var canonicalIndex = mapping.GetProperty("canonicalIndex").GetUInt32();

            var position = expansions.ExpandedCodes.IndexOf(rawCode);
            Assert.InRange(position, 0, expansions.ExpandedCodes.Length - 1);
            Assert.Equal(canonicalIndex, expansions.CanonicalIndices[position]);
            Assert.Equal(canonicalCode, table.OccupationCodes[(int)canonicalIndex]);
        }

        for (var index = 0; index < table.OccupationCodes.Length; index++)
        {
            var position = expansions.ExpandedCodes.IndexOf(table.OccupationCodes[index]);
            Assert.InRange(position, 0, expansions.ExpandedCodes.Length - 1);
            Assert.Equal((uint)index, expansions.CanonicalIndices[position]);
        }
    }

    [Fact]
    public void SingletonClusterTableEmitsNull()
    {
        var model = DecodeModel("step-13-populated");
        var table = Assert.Single(model.Energy.ClusterTables);
        Assert.Null(table.CodeExpansions);
        Assert.Equal(12, table.OccupationCodes.Length);
    }

    [Fact]
    public async Task PreMapPackageLoadsWithAbsentExpansionsAndTheRecordedSummary()
    {
        var directory = RepositoryPaths.Combine("tests/contracts/packages/amend-2-pre-map");
        var path = Path.Combine(directory, "amend-2-pre-map.msl");
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());
        Assert.True(report.IsValid);

        var read = await PackageReader.ReadAsync(path, new PackageVerificationOptions(), CancellationToken.None);
        var model = Assert.IsType<ModelPayload>(Assert.Single(read.Models).Decoded);
        var table = Assert.Single(model.Energy.ClusterTables);
        Assert.Null(table.CodeExpansions);
        Assert.Equal(6, table.OccupationCodes.Length);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "package-summary-v1.json")));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The pre-map fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public async Task RebuildReproducesTheFixtureSummary()
    {
        using var temp = new TemporaryDirectory();
        var rebuilt = await ClusterExpansionPackageFixtureBuilder.BuildAsync(temp.Path);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(rebuilt);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, ClusterExpansionPackageFixtureBuilder.SummaryFileName)));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "A rebuild no longer reproduces the checked-in cluster-expansion package-summary-v1.");
    }

    [Fact]
    public async Task RebuildIsByteDeterministic()
    {
        using var first = new TemporaryDirectory();
        using var second = new TemporaryDirectory();
        var a = await ClusterExpansionPackageFixtureBuilder.BuildAsync(first.Path);
        var b = await ClusterExpansionPackageFixtureBuilder.BuildAsync(second.Path);

        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a))),
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(b))));
    }

    [Fact]
    public async Task RegenerateClusterExpansionPackageFixtureWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_REGENERATE_CLUSTER_EXPANSION_FIXTURE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        await ClusterExpansionPackageFixtureBuilder.WriteArtifactsAsync(FixtureDirectory);

        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            ClusterExpansionPackageFixtureBuilder.PackageFileName)));
        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            ClusterExpansionPackageFixtureBuilder.SummaryFileName)));
    }

    private static ModelPayload DecodeModel(string fixture)
    {
        var path = RepositoryPaths.Combine(
            "tests/contracts/packages",
            fixture,
            "payloads",
            "model-1.bin");
        return MessagePackSerializer.Deserialize<ModelPayload>(File.ReadAllBytes(path));
    }
}
