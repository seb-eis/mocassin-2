// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Tests.Inspect;

[Trait("Tier", "PR-fast")]
public sealed class InspectCommandTests
{
    [Fact]
    public async Task JsonMatchesTheStep11Inspection()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);

        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync("inspect", package, "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
        var expected = await new MslPackageInspector().InspectAsync(package);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("inspect", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("ok").GetBoolean());
        var reported = root.GetProperty("package");
        Assert.Equal(expected.FormatVersion, reported.GetProperty("formatVersion").GetInt32());
        Assert.Equal(expected.AppVersion, reported.GetProperty("appVersion").GetString());
        Assert.Equal(expected.ProjectId.ToString("D"), reported.GetProperty("projectId").GetString());
        Assert.Equal(expected.JobSelection, reported.GetProperty("jobSelection").GetString());
        Assert.Equal(expected.JobCount, reported.GetProperty("jobCount").GetInt32());
        Assert.Equal(expected.LatticeCount, reported.GetProperty("latticeCount").GetInt32());
        Assert.Equal(expected.Models.Length, reported.GetProperty("models").GetArrayLength());
        Assert.Equal(expected.Collections.Length, reported.GetProperty("collections").GetArrayLength());
    }

    [Fact]
    public async Task HumanOutputPrintsThePackageHeaderAndModels()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", package);

        Assert.Equal(0, exitCode);
        Assert.Contains("Format version: 1", stdout, StringComparison.Ordinal);
        Assert.Contains("Job selection: all", stdout, StringComparison.Ordinal);
        Assert.Contains("Models: 1", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingPackageIsExitThree()
    {
        using var directory = new TempDirectory();
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "inspect",
            System.IO.Path.Combine(directory.Path, "missing.msl"),
            "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task ADirectoryIsExitThree()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "package.msl");
        Directory.CreateDirectory(path);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", path, "--json");

        Assert.Equal(3, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(3, document.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task NonSqliteIsCli009AndExitOne()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "text.msl");
        await File.WriteAllTextAsync(path, "not a database");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", path, "--json");

        Assert.Equal(1, exitCode);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI009"
                && diagnostic.GetProperty("hint").GetString()!.Contains("verify", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("PRAGMA application_id = 0;")]
    [InlineData("UPDATE schema_info SET value = '2' WHERE key = 'format_version';")]
    [InlineData("DROP TABLE jobs;")]
    public async Task StructurallyInvalidPackagesAreCli009(string sql)
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        PackageTestSupport.Execute(package, sql);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", package, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI009");
    }

    [Theory]
    [InlineData("UPDATE models SET payload = x'0001020304';", "PKG004")]
    [InlineData("UPDATE models SET signature = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';", "PKG005")]
    public async Task NonFatalPayloadAndSignatureDefectsStillInspect(string sql, string verifyCode)
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        PackageTestSupport.Execute(package, sql);

        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync("inspect", package, "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal("inspect", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("package").GetProperty("jobCount").GetInt32() > 0);
        Assert.True(root.GetProperty("package").GetProperty("latticeCount").GetInt32() > 0);

        var (verifyExit, verifyStdout, _) = await CliTestRunner.RunAsync("verify", package, "--json");
        Assert.Equal(1, verifyExit);
        using var verify = JsonDocument.Parse(verifyStdout);
        Assert.False(verify.RootElement.GetProperty("valid").GetBoolean());
        Assert.Contains(
            verify.RootElement.GetProperty("diagnostics").EnumerateArray(),
            diagnostic => diagnostic.GetProperty("code").GetString() == verifyCode);
    }

    [Fact]
    public async Task ATruncatedPackageIsCli009()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        var bytes = await File.ReadAllBytesAsync(package);
        await File.WriteAllBytesAsync(package, bytes[..1024]);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", package, "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI009");
    }

    [Fact]
    public async Task AParseErrorEmitsOneInspectReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneInspectReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("inspect", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("inspect-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
    }

    [Fact]
    public async Task PreCancelledInspectIsExitSixWithCli006()
    {
        using var directory = new TempDirectory();
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new Mocassin.Cli.CommandLine.CliApplication();

        var exitCode = await application.RunAsync(
            ["inspect", package, "--json"],
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
