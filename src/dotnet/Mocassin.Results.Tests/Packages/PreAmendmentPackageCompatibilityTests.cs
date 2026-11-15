// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Reads the checked-in package written by the unamended <c>net10.0</c> writer (amendment 1 §9.5).
/// </summary>
/// <remarks>
/// This is a compatibility fixture, not the feature-7 determinism fixture: it is captured once with the
/// pre-amendment writer and is never regenerated. It is the only source of a package produced before the
/// third-party Zstd codec was replaced, so it must keep verifying and produce the same canonical summary.
/// </remarks>
[Trait("Tier", "PR-fast")]
public sealed class PreAmendmentPackageCompatibilityTests
{
    private const string fixtureDirectory = "tests/contracts/packages/amend-1-net10";
    private const string fixtureName = "ceria-temperature-sweep.msl";

    private static string PackagePath => RepositoryPaths.Combine(fixtureDirectory, fixtureName);
    private static string SummaryPath => RepositoryPaths.Combine(fixtureDirectory, "package-summary-v1.json");
    private static string HashPath => RepositoryPaths.Combine(fixtureDirectory, "ceria-temperature-sweep.sha256");

    [Fact]
    public async Task PreAmendmentPackageVerifiesAndInspects()
    {
        var report = await new PackageVerifier().VerifyAsync(PackagePath, new PackageVerificationOptions());

        Assert.True(report.IsValid);
        Assert.Equal(10, report.Checks.Length);
        Assert.DoesNotContain(
            report.Diagnostics,
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(1, report.Inspection.FormatVersion);
        Assert.Equal("all", report.Inspection.JobSelection);
        Assert.Single(report.Inspection.Models);
        Assert.Equal(6, report.Inspection.LatticeCount);
        Assert.Equal(6, report.Inspection.JobCount);
    }

    [Fact]
    public async Task PreAmendmentCanonicalSummaryMatchesTheCheckedInGolden()
    {
        using var summary = await new MslPackageInspector().BuildCanonicalSummaryAsync(PackagePath);
        using var golden = JsonDocument.Parse(File.ReadAllText(SummaryPath));

        Assert.True(
            JsonElement.DeepEquals(summary.RootElement, golden.RootElement),
            "The package-summary-v1 projection of the pre-amendment fixture changed.");
    }

    [Fact]
    public void PreAmendmentFixtureHashMatchesItsProvenance()
    {
        var recorded = File.ReadAllText(HashPath).Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        var actual = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(PackagePath)));

        Assert.Equal(recorded, actual);
    }

    [Fact]
    public void PreAmendmentFixtureHasNoSidecarFiles()
    {
        var directory = Path.GetDirectoryName(PackagePath)!;
        Assert.False(File.Exists(PackagePath + "-wal"));
        Assert.False(File.Exists(PackagePath + "-shm"));
        Assert.DoesNotContain(
            Directory.EnumerateFiles(directory, "*.msl-*", SearchOption.TopDirectoryOnly),
            static path => path.EndsWith("-wal", StringComparison.Ordinal) ||
                           path.EndsWith("-shm", StringComparison.Ordinal));
    }
}
