// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Results.Packages;
using Mocassin.Results.Tests.Packages.TestSupport;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises every <c>PKG</c> code against a real package produced by the real writer.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class PackageVerifierTests
{
    private static readonly PackageVerificationOptions standard = new();

    [Fact]
    public async Task ValidPackagePassesEveryCheck()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path, jobCount: 2);

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.True(report.IsValid);
        Assert.DoesNotContain(report.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(10, report.Checks.Length);
        Assert.Equal(2, report.Inspection.JobCount);
        Assert.Equal(2, report.Inspection.LatticeCount);
    }

    [Fact]
    public async Task Pkg001UnreadableDatabase()
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "bad.msl");
        await File.WriteAllTextAsync(path, "not a sqlite database");

        var report = await new PackageVerifier().VerifyAsync(path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG001");
    }

    [Fact]
    public async Task Pkg002UnsupportedVersion()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "PRAGMA application_id = 0;");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG002");
    }

    [Fact]
    public async Task Pkg002MalformedFormatVersion()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "UPDATE schema_info SET value = 'one' WHERE key = 'format_version';");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG002");
        Assert.DoesNotContain(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG003");
    }

    [Fact]
    public async Task Pkg003MissingSchemaElement()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "DROP INDEX ix_jobs_temperature;");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG003");
    }

    [Fact]
    public async Task Pkg003MalformedProjectId()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "UPDATE schema_info SET value = 'not-a-guid' WHERE key = 'project_id';");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG003");
        Assert.DoesNotContain(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG002");
    }

    [Fact]
    public async Task Pkg003MalformedCreatedUtc()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "UPDATE schema_info SET value = 'yesterday' WHERE key = 'created_utc';");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG003");
        Assert.DoesNotContain(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG002");
    }

    [Fact]
    public async Task Pkg004CorruptPayload()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "UPDATE models SET payload = X'0102';");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG004");
    }

    [Fact]
    public async Task Pkg005WrongModelSignature()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(
            built.Path,
            $"UPDATE models SET signature = '{new string('0', 64)}';");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG005");
    }

    [Fact]
    public async Task Pkg005WrongLatticeSignature()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(
            built.Path,
            "UPDATE lattices SET signature = 'a' || substr(signature, 2) " +
            "WHERE lattice_id = (SELECT MIN(lattice_id) FROM lattices);");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG005");
    }

    [Fact]
    public async Task Pkg006MissingJobReference()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(
            built.Path,
            "PRAGMA foreign_keys = OFF; UPDATE jobs SET model_id = 999 WHERE job_id = 1;");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG006");
    }

    [Fact]
    public async Task Pkg007WrongPayloadDerivedColumn()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "UPDATE jobs SET temperature_k = 1.0 WHERE job_id = 1;");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG007");
    }

    [Fact]
    public async Task Pkg008ResultsTable()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await MutateAsync(built.Path, "CREATE TABLE results (value TEXT);");

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG008");
    }

    [Fact]
    public async Task Pkg008WalSidecar()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        await File.WriteAllBytesAsync(built.Path + "-wal", []);

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG008");
    }

    [Fact]
    public async Task Pkg009AdvisorySizeIsAWarningOnly()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);

        var report = await new PackageVerifier().VerifyAsync(
            built.Path,
            new PackageVerificationOptions(AdvisorySizeBytes: 1));

        Assert.True(report.IsValid);
        var warning = Assert.Single(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG009");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public async Task Pkg010OutOfBoundsModelPayloadValue()
    {
        using var temp = new TemporaryDirectory();
        var built = await PackageTestSupport.BuildAsync(temp.Path);
        var original = await PackageTestSupport.ReadModelPayloadAsync(built.Path);
        var payload = PackageTestSupport.DecodeModel(original);
        Assert.False(payload.Structure.Positions.IsDefaultOrEmpty);
        var position = payload.Structure.Positions[0] with { SiteIndex = 999 };
        var mutated = payload with
        {
            Structure = payload.Structure with
            {
                Positions = payload.Structure.Positions.SetItem(0, position),
            },
        };
        await PackageTestSupport.WriteModelPayloadAsync(built.Path, PackageTestSupport.Encode(mutated));

        var report = await new PackageVerifier().VerifyAsync(built.Path, standard);

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, static diagnostic => diagnostic.Code == "PKG010");
    }

    private static async Task MutateAsync(string path, string sql)
    {
        await using var connection = await PackageTestSupport.OpenReadWriteAsync(path);
        await PackageTestSupport.ExecuteAsync(connection, sql);
    }
}
