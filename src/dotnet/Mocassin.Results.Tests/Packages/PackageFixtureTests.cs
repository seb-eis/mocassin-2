// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Reads and regenerates the checked-in amended-toolchain <c>.msl</c> fixture.
/// </summary>
/// <remarks>
/// The fixture is generated once with the real writer and a fixed <c>created_utc</c>. Its canonical summary and
/// payload index are the comparable artifacts; the package bytes are recorded by SHA-256 only and are not a
/// format contract (a runtime patch may change them). Regenerate with:
/// <c>MOCASSIN_REGENERATE_PACKAGE_FIXTURES=1 dotnet test src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter "FullyQualifiedName~RegeneratePackageFixture"</c>.
/// </remarks>
[Trait("Tier", "PR-medium")]
public sealed class PackageFixtureTests
{
    private static string FixtureDirectory => RepositoryPaths.Combine(PackageFixtureBuilder.DirectoryName);

    [Fact]
    public async Task FixtureVerifiesAndMatchesTheCanonicalSummary()
    {
        var path = Path.Combine(FixtureDirectory, PackageFixtureBuilder.PackageFileName);
        var report = await new PackageVerifier().VerifyAsync(path, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(6, report.Inspection.LatticeCount);
        Assert.Equal(6, report.Inspection.JobCount);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(path);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, PackageFixtureBuilder.SummaryFileName)));
        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The fixture's package-summary-v1 no longer matches its golden.");
    }

    [Fact]
    public void FixtureHashMatchesItsProvenance()
    {
        var path = Path.Combine(FixtureDirectory, PackageFixtureBuilder.PackageFileName);
        var recorded = File.ReadAllText(Path.Combine(FixtureDirectory, PackageFixtureBuilder.HashFileName))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void ExtractedPayloadsMatchTheirIndex()
    {
        var payloadDirectory = Path.Combine(FixtureDirectory, PackageFixtureBuilder.PayloadDirectoryName);
        using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(payloadDirectory, "index.json")));
        var entries = index.RootElement.GetProperty("payloads").EnumerateArray().ToArray();

        Assert.Equal(13, entries.Length);
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
    public async Task RebuildReproducesTheFixtureSummary()
    {
        using var temp = new TemporaryDirectory();
        var rebuilt = await PackageFixtureBuilder.BuildAsync(temp.Path);

        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(rebuilt);
        using var golden = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(FixtureDirectory, PackageFixtureBuilder.SummaryFileName)));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "A rebuild no longer reproduces the checked-in package-summary-v1.");
    }

    [Fact]
    public async Task RegeneratePackageFixtureWhenRequested()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("MOCASSIN_REGENERATE_PACKAGE_FIXTURES"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        await PackageFixtureBuilder.WriteArtifactsAsync(FixtureDirectory);

        Assert.True(File.Exists(Path.Combine(FixtureDirectory, PackageFixtureBuilder.PackageFileName)));
        Assert.True(File.Exists(Path.Combine(FixtureDirectory, PackageFixtureBuilder.SummaryFileName)));
    }
}
