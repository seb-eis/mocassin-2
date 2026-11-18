// SPDX-License-Identifier: MIT


namespace Mocassin.Cli.Tests.CommandLine;

[Trait("Tier", "PR-fast")]
public sealed class CliApplicationTests
{
    [Fact]
    public async Task HelpShowsTheExecutableNameAndEveryCommand()
    {
        var (exitCode, stdout, stderr) = await RunAsync("--help");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("Usage:", stdout, StringComparison.Ordinal);
        Assert.Contains("mocassin [command] [options]", stdout, StringComparison.Ordinal);
        foreach (var command in new[]
                 {
                     "new", "validate", "compile", "build", "inspect", "verify", "doctor", "clean",
                     "simulate", "collect", "evaluate", "export",
                 })
        {
            Assert.Contains(command, stdout, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task CommandHelpExitsZero()
    {
        var (exitCode, stdout, stderr) = await RunAsync("build", "--help");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("--preview", stdout, StringComparison.Ordinal);
        Assert.Contains("--job-template", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RootVersionPrintsTheInformationalVersionAndRuntime()
    {
        var (exitCode, stdout, stderr) = await RunAsync("--version");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("mocassin ", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("Runtime: ", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubcommandVersionIsCli001AndExitTwo()
    {
        var (exitCode, stdout, stderr) = await RunAsync("validate", "--version");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        Assert.Contains("only supported on the root command", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParseErrorIsCli001AndNeverTheLibraryDefaultOne()
    {
        var (exitCode, _, stderr) = await RunAsync("validate", "x.mocprj", "--bogus");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        Assert.Contains("--bogus", stderr, StringComparison.Ordinal);
        Assert.Contains("Usage:", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCommandIsAUsageErrorWithoutAReport()
    {
        var (exitCode, stdout, stderr) = await RunAsync("bogus", "--json");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoCommandIsAUsageErrorWithoutAReport()
    {
        var (exitCode, stdout, stderr) = await RunAsync();

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashDashEndsOptionParsing()
    {
        var (exitCode, _, _) = await RunAsync("validate", "--", "--json");

        Assert.Equal(3, exitCode);
    }

    [Fact]
    public async Task NegativeOptionValuesReachTheHandler()
    {
        var (jobsExit, _, jobsError) = await RunAsync(
            "build",
            RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj"),
            "--preview",
            "--jobs",
            "-1");
        var (outputExit, _, outputError) = await RunAsync(
            "build",
            RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj"),
            "--preview",
            "-o",
            "-file");

        Assert.Equal(2, jobsExit);
        Assert.Contains("CLI005", jobsError, StringComparison.Ordinal);
        Assert.Contains("-1", jobsError, StringComparison.Ordinal);
        Assert.DoesNotContain("Required argument missing", jobsError, StringComparison.Ordinal);
        Assert.Equal(2, outputExit);
        Assert.Contains("-o", outputError, StringComparison.Ordinal);
        Assert.DoesNotContain("Required argument missing", outputError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParallelRangeValidatorRejectsOutOfRangeValues()
    {
        var (exitCode, _, stderr) = await RunAsync(
            "build",
            RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj"),
            "--preview",
            "--parallel",
            "257");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        Assert.Contains("256", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PosixBundlingIsDisabled()
    {
        var (exitCode, _, stderr) = await RunAsync("validate", "x.mocprj", "-abc");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        Assert.Contains("-abc", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReservedCommandReportsCli008AndExitsTwo()
    {
        var (exitCode, stdout, stderr) = await RunAsync("simulate", "--anything");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI008", stderr, StringComparison.Ordinal);
        Assert.Contains("Step 19/27", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EverySubcommandRejectsRootOnlyVersion()
    {
        foreach (var command in new[] { "new", "build", "compile", "inspect", "verify", "doctor", "clean" })
        {
            var (exitCode, _, stderr) = await RunAsync(command, "--version");
            Assert.Equal(2, exitCode);
            Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        }
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);
}
