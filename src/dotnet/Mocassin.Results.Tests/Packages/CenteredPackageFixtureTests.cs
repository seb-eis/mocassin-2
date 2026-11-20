// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Results.Packages;
using Mocassin.Results.Packages.Internal;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Verifies the checked-in Step 13 C-centered package fixture and its canonical summary.
/// </summary>
/// <remarks>
/// Regenerate with:
/// <c>MOCASSIN_REGENERATE_CENTERED_FIXTURE=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegenerateCenteredPackageFixture"</c>.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class CenteredPackageFixtureTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine(TestSupport.CenteredPackageFixtureBuilder.DirectoryName);

    [Fact]
    public async Task FixtureVerifiesAndMatchesTheCanonicalSummary()
    {
        var path = Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.PackageFileName);
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(1, report.Inspection.LatticeCount);
        Assert.Equal(1, report.Inspection.JobCount);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.SummaryFileName)));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The centered fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public void FixtureHasTheCenteredShape()
    {
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.SummaryFileName)));
        var model = golden.RootElement.GetProperty("models")[0];
        Assert.Equal(4u, model.GetProperty("particleCount").GetUInt32());
        Assert.Equal(8u, model.GetProperty("positionCount").GetUInt32());
        Assert.Equal(2u, model.GetProperty("siteCount").GetUInt32());
        Assert.Equal(4u, model.GetProperty("pairTableCount").GetUInt32());
        Assert.Equal(0u, model.GetProperty("clusterTableCount").GetUInt32());
        Assert.Equal(1u, model.GetProperty("collectionCount").GetUInt32());
        Assert.Equal(8u, model.GetProperty("ruleCount").GetUInt32());

        var jobs = golden.RootElement.GetProperty("jobs").EnumerateArray().ToArray();
        Assert.Single(jobs);
        Assert.Equal("mmc", jobs[0].GetProperty("kind").GetString());
    }

    [Fact]
    public async Task FixtureCarriesEquivalentStartsAndTheMaskDifference()
    {
        var path = Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.PackageFileName);
        var read = await PackageReader.ReadAsync(path, new PackageVerificationOptions(), CancellationToken.None);
        var model = Assert.Single(read.Models).Decoded;
        Assert.NotNull(model);
        Assert.Equal(4u, model.Particle.Count);
        Assert.Equal(8u, model.Structure.PositionCount);

        var crossBuckets = new HashSet<(uint Bucket, uint Direction, uint DirectionPosition)>();
        var offset = 0;
        for (var position = 0u; position < model.Structure.PositionCount; position++)
        {
            for (var particle = 0u; particle < model.Particle.Count; particle++)
            {
                var count = model.Transitions.JumpCount[(int)(position * model.Particle.Count + particle)];
                for (var slot = 0; slot < count; slot++)
                {
                    var directionId = model.Transitions.JumpMapping[offset + slot];
                    var directionPosition = model.Transitions.JumpDirections[(int)directionId].PositionId;
                    if (directionPosition != position)
                    {
                        crossBuckets.Add((position, directionId, directionPosition));
                    }
                }

                offset += (int)count;
            }
        }

        Assert.Contains((2u, 3u, 6u), crossBuckets);
        Assert.Contains((3u, 1u, 7u), crossBuckets);
        Assert.Contains((4u, 0u, 0u), crossBuckets);
        Assert.Contains((5u, 2u, 1u), crossBuckets);

        var foundMaskDifference = false;
        foreach (var collection in model.Transitions.JumpCollections)
        {
            var directions = collection.DirectionIds
                .Select(id => model.Transitions.JumpDirections[(int)id])
                .ToArray();
            var nodes = directions.Max(static direction => direction.Path.Length);
            for (var node = 0; node < nodes; node++)
            {
                var masks = directions
                    .Where(direction => node < direction.Path.Length)
                    .Select(direction => model.Structure.Positions[direction.Path[node][3]].ParticleIds)
                    .Select(static ids => string.Join(',', ids))
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                foundMaskDifference |= masks > 1;
            }
        }

        Assert.True(
            foundMaskDifference,
            "The centered real-writer fixture must carry at least one per-node mask difference.");
    }

    [Fact]
    public void FixtureHashMatchesItsProvenance()
    {
        var path = Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.PackageFileName);
        var recorded = File.ReadAllText(
                Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.HashFileName))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void ExtractedPayloadsMatchTheirIndex()
    {
        var payloadDirectory = Path.Combine(
            FixtureDirectory,
            TestSupport.CenteredPackageFixtureBuilder.PayloadDirectoryName);
        using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(payloadDirectory, "index.json")));
        var entries = index.RootElement.GetProperty("payloads").EnumerateArray().ToArray();

        Assert.Equal(3, entries.Length);
        Assert.Contains(entries, static entry => entry.GetProperty("kind").GetString() == "model");
        Assert.Contains(entries, static entry => entry.GetProperty("kind").GetString() == "lattice");
        Assert.Contains(entries, static entry => entry.GetProperty("kind").GetString() == "job");
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
    public async Task RebuildReproducesTheFixtureSummary()
    {
        using var temp = new TemporaryDirectory();
        var rebuilt = await TestSupport.CenteredPackageFixtureBuilder.BuildAsync(temp.Path);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(rebuilt);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.CenteredPackageFixtureBuilder.SummaryFileName)));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "A rebuild no longer reproduces the checked-in centered package-summary-v1.");
    }

    [Fact]
    public async Task RegenerateCenteredPackageFixtureWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_REGENERATE_CENTERED_FIXTURE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        await TestSupport.CenteredPackageFixtureBuilder.WriteArtifactsAsync(FixtureDirectory);

        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            TestSupport.CenteredPackageFixtureBuilder.PackageFileName)));
        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            TestSupport.CenteredPackageFixtureBuilder.SummaryFileName)));
    }
}
