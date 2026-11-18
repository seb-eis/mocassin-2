// SPDX-License-Identifier: MIT

using System.Text;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Tests.TestSupport;

namespace Mocassin.Cli.Tests.CommandLine;

[Trait("Tier", "PR-fast")]
public sealed class ResponseFileTests
{
    private static readonly string ceria = RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj");

    [Theory]
    [InlineData("a\"b c\"d", "ab cd")]
    [InlineData("a\"\"b", "ab")]
    [InlineData("x\"y z", "xy z")]
    [InlineData("a\"b c\"", "ab c")]
    [InlineData("ab\" cd \"ef", "ab cd ef")]
    [InlineData("back\\slash", "back\\slash")]
    [InlineData("a 'b c' d", "a", "'b", "c'", "d")]
    public void TokenizerFollowsTheDocumentedQuotingRules(string line, params string[] expected)
    {
        Assert.Equal(expected, CliResponseFiles.TokenizeLine(line));
    }

    [Fact]
    public void TokenizerPinsTheGoldenCases()
    {
        Assert.Empty(CliResponseFiles.TokenizeLine("\"x y"));
        Assert.Equal(["a", "b"], CliResponseFiles.TokenizeLine("\"a\"\"b\""));
        Assert.Equal([string.Empty], CliResponseFiles.TokenizeLine("\"\""));
        Assert.Equal([string.Empty, "x"], CliResponseFiles.TokenizeLine("\"\"x"));
    }

    [Fact]
    public async Task ResponseFileExpandsRecursivelyRelativeToTheWorkingDirectory()
    {
        using var directory = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(directory.Path, "nested"));
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "nested", "inner.rsp"),
            "--json\n" + ceria + "\n");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "outer.rsp"), "@nested/inner.rsp\n");

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "validate", "@outer.rsp");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("\"kind\": \"validation\"", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommentsBomCrLfAndTabsAreAccepted()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "args.rsp");
        var content = "# a comment\r\n\t--json\t\r\n" + ceria + "\r\n";
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "validate", "@args.rsp");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("\"kind\": \"validation\"", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AtAloneIsALiteralArgument()
    {
        var (exitCode, _, stderr) = await RunAsync("validate", "@", "--json");

        Assert.Equal(3, exitCode);
        Assert.DoesNotContain("Response file", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoubleAtReferencesAFileNamedAt()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "@args.rsp"), "--json\n" + ceria + "\n");

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "validate", "@@args.rsp");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("\"kind\": \"validation\"", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashDashInsideAResponseFileStopsExpansionForThatFile()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "inner.rsp"), "x\n--json\n");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "outer.rsp"), "--\n@inner.rsp\n");

        var (exitCode, _, _) = await RunInAsync(directory.Path, "validate", "@outer.rsp");

        // Literal `@inner.rsp` becomes the project path and is missing (3); expanding it would produce
        // x plus a literal --json, which is an extra argument (2).
        Assert.Equal(3, exitCode);
    }

    [Fact]
    public async Task SingleValueOptionReceivesEveryFileToken()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "paths.rsp"), ceria + "\n" + ceria + "\n");

        var (exitCode, _, stderr) = await RunInAsync(directory.Path, "validate", "@paths.rsp");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EqualsFormIsNotExpanded()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "jobs.rsp"), "1-3\n");

        var (exitCode, _, stderr) = await RunInAsync(
            directory.Path,
            "build",
            RepositoryPaths.Combine("samples", "jobs", "ceria-temperature-sweep.mocprj"),
            "--preview",
            "--jobs=@jobs.rsp");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI005", stderr, StringComparison.Ordinal);
        Assert.Contains("@jobs.rsp", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingFileIsCli001AndExitsTwo()
    {
        var (exitCode, stdout, stderr) = await RunAsync("validate", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        Assert.Contains("CLI001", stdout, StringComparison.Ordinal);
        Assert.Contains("was not found", stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public async Task MissingFileWithoutACommandEmitsNoReport()
    {
        var (exitCode, stdout, stderr) = await RunAsync("@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectoryIsACli001ReadFailure()
    {
        using var directory = new TempDirectory();
        Directory.CreateDirectory(Path.Combine(directory.Path, "args.rsp"));

        var (exitCode, stdout, _) = await RunInAsync(directory.Path, "validate", "@args.rsp", "--json");

        Assert.Equal(2, exitCode);
        Assert.Contains("could not be read", stdout, StringComparison.Ordinal);
        Assert.Contains("directory", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidUtf8AndNulAreCli001()
    {
        using var directory = new TempDirectory();
        await File.WriteAllBytesAsync(Path.Combine(directory.Path, "invalid.rsp"), [0xFF, 0xFE]);
        await File.WriteAllBytesAsync(Path.Combine(directory.Path, "nul.rsp"), [0x61, 0x00, 0x62]);

        var (invalidExit, invalidStdout, _) = await RunInAsync(directory.Path, "validate", "@invalid.rsp", "--json");
        var (nulExit, nulStdout, _) = await RunInAsync(directory.Path, "validate", "@nul.rsp", "--json");

        Assert.Equal(2, invalidExit);
        Assert.Contains("not valid UTF-8", invalidStdout, StringComparison.Ordinal);
        Assert.Equal(2, nulExit);
        Assert.Contains("not valid UTF-8", nulStdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PerFileByteLimitIsEnforced()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "large.rsp");
        await File.WriteAllBytesAsync(path, new byte[CliResponseFiles.MaxFileBytes + 1]);

        var (exitCode, stdout, _) = await RunInAsync(directory.Path, "validate", "@large.rsp", "--json");

        Assert.Equal(2, exitCode);
        Assert.Contains("per-file limit", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TotalByteLimitIsEnforced()
    {
        using var directory = new TempDirectory();
        var arguments = new List<string> { "validate" };
        for (var index = 0; index < 5; index++)
        {
            var path = Path.Combine(directory.Path, $"part{index}.rsp");
            await File.WriteAllBytesAsync(path, Enumerable.Repeat((byte)'a', CliResponseFiles.MaxFileBytes).ToArray());
            arguments.Add($"@part{index}.rsp");
        }

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, arguments.ToArray());

        Assert.Equal(2, exitCode);
        Assert.Contains("total limit", stdout + stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NestingDepthLimitIsEnforced()
    {
        using var directory = new TempDirectory();
        for (var index = 0; index < 10; index++)
        {
            var content = index == 9 ? "--json\n" : $"@level{index + 1}.rsp\n";
            await File.WriteAllTextAsync(Path.Combine(directory.Path, $"level{index}.rsp"), content);
        }

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "validate", "@level0.rsp");

        Assert.Equal(2, exitCode);
        Assert.Contains("depth limit", stdout + stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileCountLimitIsEnforced()
    {
        using var directory = new TempDirectory();
        var arguments = new List<string> { "validate" };
        for (var index = 0; index <= CliResponseFiles.MaxFiles; index++)
        {
            await File.WriteAllTextAsync(Path.Combine(directory.Path, $"f{index}.rsp"), string.Empty);
            arguments.Add($"@f{index}.rsp");
        }

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, arguments.ToArray());

        Assert.Equal(2, exitCode);
        Assert.Contains("32 files", stdout + stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContributedTokenLimitIsEnforced()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "tokens.rsp");
        await File.WriteAllTextAsync(
            path,
            string.Join(' ', Enumerable.Repeat("token", CliResponseFiles.MaxContributedTokens + 1)));

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "validate", "@tokens.rsp");

        Assert.Equal(2, exitCode);
        Assert.Contains("token limit", stdout + stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACommandOnlyInsideTheFailedFileEmitsNoReport()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "partial.rsp"),
            "build\n@missing-inside.rsp\n");

        var (exitCode, stdout, stderr) = await RunInAsync(directory.Path, "@partial.rsp", "--json");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("CLI001", stderr, StringComparison.Ordinal);
        Assert.Contains("missing-inside.rsp", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACommandOutsideTheFailedFileStillEmitsAReport()
    {
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "build.rsp"), "build\n");

        var (exitCode, stdout, stderr) = await RunInAsync(
            directory.Path,
            "@build.rsp",
            "@missing.rsp",
            "--json");

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stderr);
        Assert.Contains("\"kind\": \"build\"", stdout, StringComparison.Ordinal);
        Assert.Contains("CLI001", stdout, StringComparison.Ordinal);
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunInAsync(
        string workingDirectory,
        params string[] arguments)
    {
        var previous = Environment.CurrentDirectory;
        Environment.CurrentDirectory = workingDirectory;
        try
        {
            return await CliTestRunner.RunAsync(arguments);
        }
        finally
        {
            Environment.CurrentDirectory = previous;
        }
    }
}
