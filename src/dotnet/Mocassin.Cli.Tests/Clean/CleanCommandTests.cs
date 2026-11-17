// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;

namespace Mocassin.Cli.Tests.Clean;

[Trait("Tier", "PR-fast")]
public sealed class CleanCommandTests
{
    [Fact]
    public async Task DryRunListsJobFoldersWithoutDeleting()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);

        var (exitCode, stdout, stderr) = await RunAsync("clean", package, "--dry-run", "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("clean-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        var root = report.RootElement;
        Assert.True(root.GetProperty("dryRun").GetBoolean());
        Assert.Equal("all", root.GetProperty("jobSelection").GetString());
        Assert.Equal(6, root.GetProperty("jobCount").GetInt32());
        Assert.Equal(0, root.GetProperty("removedCount").GetInt32());
        Assert.Equal(
            [1, 2, 7],
            root.GetProperty("folders").EnumerateArray().Select(static item => item.GetProperty("jobId").GetInt32()));
        Assert.Equal(10, root.GetProperty("totalBytes").GetInt64());
        Assert.True(Directory.Exists(System.IO.Path.Combine(directory.Path, "Job00001")));
    }

    [Fact]
    public async Task OlderThanFiltersByTheFolderTimestamp()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);
        Directory.SetLastWriteTimeUtc(
            System.IO.Path.Combine(directory.Path, "Job00001"),
            DateTime.UtcNow.AddDays(-10));

        var (exitCode, stdout, _) = await RunAsync(
            "clean",
            package,
            "--dry-run",
            "--older-than",
            "5",
            "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(
            [1],
            report.RootElement.GetProperty("folders").EnumerateArray()
                .Select(static item => item.GetProperty("jobId").GetInt32()));
        Assert.Equal(5, report.RootElement.GetProperty("olderThanDays").GetInt32());

        var (noneExit, noneJson, _) = await RunAsync(
            "clean",
            package,
            "--dry-run",
            "--older-than",
            "20",
            "--json");
        Assert.Equal(0, noneExit);
        using var none = JsonDocument.Parse(noneJson);
        Assert.Empty(none.RootElement.GetProperty("folders").EnumerateArray());
    }

    [Fact]
    public async Task MissingPackageIsExitThree()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await RunAsync(
            "clean",
            System.IO.Path.Combine(directory.Path, "missing.msl"),
            "--dry-run",
            "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("clean-v1", stdout);
    }

    [Fact]
    public async Task MissingDirectoryIsExitThree()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);

        var (exitCode, stdout, _) = await RunAsync(
            "clean",
            package,
            "--dir",
            System.IO.Path.Combine(directory.Path, "missing"),
            "--dry-run",
            "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task InvalidPackageIsCli009()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "text.msl");
        await File.WriteAllTextAsync(path, "not a database");

        var (exitCode, stdout, _) = await RunAsync("clean", path, "--dry-run", "--json");

        Assert.Equal(1, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI009");
    }

    [Fact]
    public async Task NoDryRunIsCli008AndExitTwoWithNoDeletion()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);
        var before = Snapshot(directory.Path);

        var (exitCode, stdout, _) = await RunAsync("clean", package, "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("clean-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.False(report.RootElement.GetProperty("dryRun").GetBoolean());
        Assert.Equal(0, report.RootElement.GetProperty("removedCount").GetInt32());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI008");
        Assert.Equal(before, Snapshot(directory.Path));
    }

    [Fact]
    public async Task ResultsOptionIsCli008AndExitTwo()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);
        var before = Snapshot(directory.Path);

        var (exitCode, stdout, _) = await RunAsync(
            "clean",
            package,
            "--dry-run",
            "--results",
            System.IO.Path.Combine(directory.Path, "result.msr"),
            "--json");

        Assert.Equal(2, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI008");
        Assert.Equal(before, Snapshot(directory.Path));
    }

    [Fact]
    public async Task HumanListingPrintsTheFolders()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);

        var (exitCode, stdout, _) = await RunAsync("clean", package, "--dry-run");

        Assert.Equal(0, exitCode);
        Assert.Contains("Mode: dry run", stdout, StringComparison.Ordinal);
        Assert.Contains("Job00001", stdout, StringComparison.Ordinal);
        Assert.Contains("Total: 10 bytes", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DryRunDoesNotModifyThePackageOrOtherFiles()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);
        var beforeHash = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(package)));
        var beforeWrite = File.GetLastWriteTimeUtc(package);
        var before = Snapshot(directory.Path);

        Assert.Equal(0, (await RunAsync("clean", package, "--dry-run", "--json")).ExitCode);

        Assert.Equal(beforeHash, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(package))));
        Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(package));
        Assert.Equal(before, Snapshot(directory.Path));
        Assert.True(File.Exists(System.IO.Path.Combine(directory.Path, "results.msr")));
        Assert.True(File.Exists(System.IO.Path.Combine(directory.Path, "external.mcs")));
    }

    [Fact]
    public void CleanCommandContainsNoDeletionPath()
    {
        var source = File.ReadAllText(RepositoryPaths.Combine(
            "src",
            "dotnet",
            "Mocassin.Cli",
            "Clean",
            "CleanCommand.cs"));

        Assert.DoesNotContain("Directory.Delete", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.Delete", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AParseErrorEmitsOneCleanReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("clean", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("clean-v1", stdout);
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneCleanReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("clean", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("clean-v1", stdout);
    }

    [Fact]
    public async Task PreCancelledCleanIsExitSixWithCli006()
    {
        using var directory = new TempDirectory();
        var package = await CreateAsync(directory);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new CliApplication();

        var exitCode = await application.RunAsync(
            ["clean", package, "--dry-run", "--json"],
            stdout,
            stderr,
            cancellation.Token);

        Assert.Equal(6, exitCode);
        using var report = JsonDocument.Parse(stdout.ToString());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);

    private static async Task<string> CreateAsync(TempDirectory directory)
    {
        var package = await PackageTestSupport.CreateAsync(directory.Path);
        foreach (var jobId in new[] { 1, 2, 7 })
        {
            var jobDirectory = System.IO.Path.Combine(directory.Path, $"Job{jobId:D5}");
            Directory.CreateDirectory(jobDirectory);
            await File.WriteAllBytesAsync(
                System.IO.Path.Combine(jobDirectory, "run.mcs"),
                new byte[jobId]);
        }

        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "results.msr"), "results");
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "external.mcs"), "state");
        return package;
    }

    private static string Snapshot(string directory) =>
        string.Join(
            "\n",
            Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(static entry => File.Exists(entry)
                    ? $"{entry}:{Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(entry)))}"
                    : $"{entry}:directory"));
}
