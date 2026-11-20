// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Verifies the checked-in Step 13 populated package fixture and its canonical summary.
/// </summary>
/// <remarks>
/// Regenerate with:
/// <c>MOCASSIN_REGENERATE_POPULATED_FIXTURE=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegeneratePopulatedPackageFixture"</c>.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class PopulatedPackageFixtureTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine(TestSupport.PopulatedPackageFixtureBuilder.DirectoryName);

    [Fact]
    public async Task FixtureVerifiesAndMatchesTheCanonicalSummary()
    {
        var path = Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.PackageFileName);
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(1, report.Inspection.LatticeCount);
        Assert.Equal(2, report.Inspection.JobCount);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.SummaryFileName)));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The populated fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public void FixtureHasThePopulatedShape()
    {
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.SummaryFileName)));
        var model = golden.RootElement.GetProperty("models")[0];
        Assert.Equal(6, model.GetProperty("pairTableCount").GetInt32());
        Assert.Equal(1, model.GetProperty("clusterTableCount").GetInt32());

        var jobs = golden.RootElement.GetProperty("jobs").EnumerateArray().ToArray();
        Assert.Equal(2, jobs.Length);
        Assert.Contains(jobs, static job => job.GetProperty("kind").GetString() == "kmc");
        Assert.Contains(jobs, static job => job.GetProperty("kind").GetString() == "mmc");
    }

    [Fact]
    public void FixtureHashMatchesItsProvenance()
    {
        var path = Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.PackageFileName);
        var recorded = File.ReadAllText(
                Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.HashFileName))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void ExtractedPayloadsMatchTheirIndex()
    {
        var payloadDirectory = Path.Combine(
            FixtureDirectory,
            TestSupport.PopulatedPackageFixtureBuilder.PayloadDirectoryName);
        using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(payloadDirectory, "index.json")));
        var entries = index.RootElement.GetProperty("payloads").EnumerateArray().ToArray();

        Assert.Equal(4, entries.Length);
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
        var rebuilt = await TestSupport.PopulatedPackageFixtureBuilder.BuildAsync(temp.Path);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(rebuilt);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, TestSupport.PopulatedPackageFixtureBuilder.SummaryFileName)));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "A rebuild no longer reproduces the checked-in populated package-summary-v1.");
    }

    [Fact]
    public async Task RegeneratePopulatedPackageFixtureWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_REGENERATE_POPULATED_FIXTURE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        await TestSupport.PopulatedPackageFixtureBuilder.WriteArtifactsAsync(FixtureDirectory);

        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            TestSupport.PopulatedPackageFixtureBuilder.PackageFileName)));
        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            TestSupport.PopulatedPackageFixtureBuilder.SummaryFileName)));
    }
}
