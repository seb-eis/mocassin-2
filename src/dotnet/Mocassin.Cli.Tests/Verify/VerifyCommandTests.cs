// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Tests.Verify;

[Trait("Tier", "PR-fast")]
public sealed class VerifyCommandTests
{
    [Fact]
    public async Task JsonMatchesTheStep11Verification()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);

        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync("verify", package, "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("verify-v1", stdout);
        var expected = await new PackageVerifier().VerifyAsync(package, new PackageVerificationOptions());
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("verify", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("valid").GetBoolean());
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal(expected.Checks.Length, root.GetProperty("checks").GetArrayLength());
        Assert.Equal(expected.Inspection.JobCount, root.GetProperty("package").GetProperty("jobCount").GetInt32());
    }

    [Fact]
    public async Task HumanOutputPrintsTheOrderedChecks()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", package);

        Assert.Equal(0, exitCode);
        Assert.Contains("Valid: yes", stdout, StringComparison.Ordinal);
        Assert.Contains("Checks:", stdout, StringComparison.Ordinal);
        Assert.Contains("[PASS] PKG001", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingPackageIsExitThree()
    {
        using var directory = new TempDirectory();
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "verify",
            System.IO.Path.Combine(directory.Path, "missing.msl"),
            "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("verify-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task NonSqliteIsInvalidWithPkg001()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "text.msl");
        await File.WriteAllTextAsync(path, "not a database");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", path, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PKG001");
    }

    [Theory]
    [InlineData("PRAGMA application_id = 0;", "PKG002")]
    [InlineData("UPDATE schema_info SET value = '2' WHERE key = 'format_version';", "PKG002")]
    [InlineData("DROP TABLE jobs;", "PKG003")]
    [InlineData("UPDATE models SET payload = x'0001020304';", "PKG004")]
    [InlineData("UPDATE models SET signature = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';", "PKG005")]
    public async Task StructurallyInvalidPackagesPreserveThePkgCode(string sql, string expectedCode)
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        PackageTestSupport.Execute(package, sql);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", package, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            diagnostic => diagnostic.GetProperty("code").GetString() == expectedCode);
    }

    [Fact]
    public async Task ATruncatedPackageIsInvalid()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        var bytes = await File.ReadAllBytesAsync(package);
        await File.WriteAllBytesAsync(package, bytes[..1024]);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", package, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.False(document.RootElement.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task InspectionAndVerificationDoNotModifyThePackage()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        var beforeHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(package)));
        var beforeWrite = File.GetLastWriteTimeUtc(package);
        var beforeEntries = Directory.GetFileSystemEntries(directory.Path)
            .Select(static entry => System.IO.Path.GetFileName(entry))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(0, (await CliTestRunner.RunAsync("inspect", package, "--json")).ExitCode);
        Assert.Equal(0, (await CliTestRunner.RunAsync("verify", package, "--json")).ExitCode);

        Assert.Equal(beforeHash, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(package))));
        Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(package));
        Assert.Equal(
            beforeEntries,
            Directory.GetFileSystemEntries(directory.Path)
                .Select(static entry => System.IO.Path.GetFileName(entry))
                .Order(StringComparer.Ordinal)
                .ToArray());
        Assert.False(File.Exists(package + "-wal"));
        Assert.False(File.Exists(package + "-shm"));
    }

    [Fact]
    public async Task AParseErrorEmitsOneVerifyReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("verify-v1", stdout);
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneVerifyReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("verify", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("verify-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
    }

    [Fact]
    public async Task PreCancelledVerifyIsExitSixWithCli006()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new Mocassin.Cli.CommandLine.CliApplication();

        var exitCode = await application.RunAsync(
            ["verify", package, "--json"],
            stdout,
            stderr,
            cancellation.Token);

        Assert.Equal(6, exitCode);
        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
    }
}
