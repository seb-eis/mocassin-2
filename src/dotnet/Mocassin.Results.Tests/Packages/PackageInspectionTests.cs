// SPDX-License-Identifier: MIT

using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Reads real packages with the inspector and checks the canonical summary projection.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class PackageInspectionTests
{
    [Fact]
    public async Task InspectorReadsARealPackage()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);
        var inspection = await new MslPackageInspector().InspectAsync(built.Path);

        Assert.Equal(1, inspection.FormatVersion);
        Assert.Equal(built.Applied.Signature, Assert.Single(inspection.Models).Signature);
        Assert.Equal(built.Applied.ModelSignature, inspection.Models[0].ModelSignature);
        Assert.Equal(2, inspection.LatticeCount);
        Assert.Equal(2, inspection.JobCount);
        Assert.Equal(1.0, inspection.LatticeReuseFactor, 12);
        var collection = Assert.Single(inspection.Collections);
        Assert.Equal("kmc", collection.Kind);
        Assert.Equal("T773", collection.CollectionName);
        Assert.Equal("x001", collection.ConfigName);
        Assert.Equal(2, collection.JobCount);
        Assert.Equal(773.0, inspection.MinTemperatureK);
        Assert.Equal(773.0, inspection.MaxTemperatureK);
        Assert.Equal(1.0e7, inspection.MinElectricFieldVm);
        Assert.Equal(1.0e7, inspection.MaxElectricFieldVm);
        Assert.True(inspection.SizeBytes > 0);
        Assert.Equal("all", inspection.JobSelection);
        Assert.False(string.IsNullOrWhiteSpace(inspection.AppVersion));
        Assert.Equal($"mocassin .NET {inspection.AppVersion}", inspection.Generator);
    }

    [Fact]
    public async Task CanonicalSummaryIsDeterministicAndNormalized()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);
        var inspector = new MslPackageInspector();

        using var first = await inspector.BuildCanonicalSummaryAsync(built.Path);
        using var second = await inspector.BuildCanonicalSummaryAsync(built.Path);
        var raw = first.RootElement.GetRawText();
        Assert.Equal(raw, second.RootElement.GetRawText());
        Assert.DoesNotContain("createdUtc", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("appVersion", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("generator", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("sizeBytes", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("payloadBytes", raw, StringComparison.Ordinal);

        var root = first.RootElement;
        Assert.Equal("mocassin.package-summary", root.GetProperty("format").GetString());
        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal(built.Applied.Signature, root.GetProperty("models")[0].GetProperty("signature").GetString());
        Assert.Equal(2, root.GetProperty("lattices").GetArrayLength());
        Assert.Equal(2, root.GetProperty("jobs").GetArrayLength());
        Assert.Equal(1, root.GetProperty("counts").GetProperty("modelCount").GetInt32());
        Assert.Equal(2, root.GetProperty("counts").GetProperty("latticeCount").GetInt32());
        Assert.Equal(2, root.GetProperty("counts").GetProperty("jobCount").GetInt32());
        Assert.Equal(1.0, root.GetProperty("counts").GetProperty("latticeReuseFactor").GetDouble(), 12);
        var jobs = root.GetProperty("jobs").EnumerateArray().ToArray();
        Assert.Equal(jobs.OrderBy(static job => job.GetProperty("jobId").GetInt32()), jobs);
        Assert.All(jobs, static job =>
            Assert.Equal(64, job.GetProperty("seedHex").GetString()!.Length));
        Assert.Equal(1, root.GetProperty("schema").GetProperty("formatVersion").GetInt32());
        Assert.Equal("all", root.GetProperty("schema").GetProperty("jobSelection").GetString());
    }

    [Fact]
    public async Task PublishedPackageNeedsNoWalSidecars()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 1);

        Assert.False(File.Exists(built.Path + "-wal"));
        Assert.False(File.Exists(built.Path + "-shm"));
        var inspection = await new MslPackageInspector().InspectAsync(built.Path);
        Assert.Equal(1, inspection.JobCount);
    }

    [Fact]
    public async Task UnreadablePackageThrowsInvalidData()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "not-a-package.msl");
        await File.WriteAllTextAsync(path, "not a sqlite database");

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new MslPackageInspector().InspectAsync(path));
    }
}
