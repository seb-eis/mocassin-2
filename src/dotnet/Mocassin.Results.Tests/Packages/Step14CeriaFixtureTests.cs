// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Verifies the checked-in Step 14 ceria 10×10×10 context fixture and its canonical summary.
/// </summary>
/// <remarks>
/// Regenerate with:
/// <c>MOCASSIN_REGENERATE_STEP14_CERIA_FIXTURE=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegenerateStep14CeriaFixture"</c>.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class Step14CeriaFixtureTests
{
    private static string FixtureDirectory =>
        RepositoryPaths.Combine(Step14CeriaFixtureBuilder.DirectoryName);

    [Fact]
    public async Task FixtureVerifiesAndMatchesTheCanonicalSummary()
    {
        var path = Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.PackageFileName);
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(1, report.Inspection.LatticeCount);
        Assert.Equal(1, report.Inspection.JobCount);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.SummaryFileName)));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The Step 14 ceria fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public void FixtureHasTheTenCubedShape()
    {
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.SummaryFileName)));
        var model = golden.RootElement.GetProperty("models")[0];
        Assert.Equal(5u, model.GetProperty("particleCount").GetUInt32());
        Assert.Equal(4u, model.GetProperty("positionCount").GetUInt32());
        Assert.Equal(4u, model.GetProperty("siteCount").GetUInt32());
        Assert.Equal(0u, model.GetProperty("pairTableCount").GetUInt32());
        Assert.Equal(0u, model.GetProperty("clusterTableCount").GetUInt32());
        Assert.Equal(1u, model.GetProperty("collectionCount").GetUInt32());
        Assert.Equal(4u, model.GetProperty("ruleCount").GetUInt32());

        var lattice = golden.RootElement.GetProperty("lattices")[0];
        Assert.Equal(10u, lattice.GetProperty("latticeSize")[0].GetUInt32());
        Assert.Equal(10u, lattice.GetProperty("latticeSize")[1].GetUInt32());
        Assert.Equal(10u, lattice.GetProperty("latticeSize")[2].GetUInt32());
        Assert.Equal(4u, lattice.GetProperty("positionCount").GetUInt32());
        Assert.Equal(4000u, lattice.GetProperty("latticeLength").GetUInt32());

        var jobs = golden.RootElement.GetProperty("jobs").EnumerateArray().ToArray();
        Assert.Single(jobs);
        Assert.Equal("kmc", jobs[0].GetProperty("kind").GetString());
    }

    [Fact]
    public void FixtureHashMatchesItsProvenance()
    {
        var path = Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.PackageFileName);
        var recorded = File.ReadAllText(
                Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.HashFileName))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void ExtractedPayloadsMatchTheirIndex()
    {
        var payloadDirectory = Path.Combine(
            FixtureDirectory,
            Step14CeriaFixtureBuilder.PayloadDirectoryName);
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
        var rebuilt = await Step14CeriaFixtureBuilder.BuildAsync(temp.Path);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(rebuilt);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, Step14CeriaFixtureBuilder.SummaryFileName)));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "A rebuild no longer reproduces the checked-in Step 14 ceria package-summary-v1.");
    }

    [Fact]
    public async Task RegenerateStep14CeriaFixtureWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_REGENERATE_STEP14_CERIA_FIXTURE"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        await Step14CeriaFixtureBuilder.WriteArtifactsAsync(FixtureDirectory);

        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            Step14CeriaFixtureBuilder.PackageFileName)));
        Assert.True(File.Exists(Path.Combine(
            FixtureDirectory,
            Step14CeriaFixtureBuilder.SummaryFileName)));
    }
}
