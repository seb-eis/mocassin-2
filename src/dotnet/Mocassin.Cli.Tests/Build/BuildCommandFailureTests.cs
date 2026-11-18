// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Build;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Build;
using Mocassin.Results.Packages;

namespace Mocassin.Cli.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildCommandFailureTests
{
    [Fact]
    public async Task ExistingTargetWithoutForceIsCli003AndTheFileIsPreserved()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "existing.msl");
        var original = new byte[] { 1, 2, 3 };
        await File.WriteAllBytesAsync(target, original);

        var (exitCode, stdout, _) = await RunAsync("build", test.Path, "-o", target, "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(3, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI003");
        Assert.Equal(original, await File.ReadAllBytesAsync(target));
    }

    [Fact]
    public async Task ForceReplacesAnExistingPackage()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "existing.msl");
        await File.WriteAllBytesAsync(target, [1, 2, 3]);

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--force",
            "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.True(document.RootElement.GetProperty("package").GetProperty("published").GetBoolean());
    }

    [Fact]
    public async Task AnExplicitDirectoryTargetIsCli002BeforeLoading()
    {
        using var directory = new TempDirectory();
        var target = System.IO.Path.Combine(directory.Path, "target.msl");
        Directory.CreateDirectory(target);

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            CliTestProject.SamplePath,
            "-o",
            target,
            "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(3, report.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
        Assert.Empty(report.RootElement.GetProperty("timings").EnumerateArray());
    }

    [Fact]
    public async Task AMissingTargetParentIsCli002AndIsNotCreated()
    {
        using var directory = new TempDirectory();
        var parent = System.IO.Path.Combine(directory.Path, "missing", "nested");
        var target = System.IO.Path.Combine(parent, "package.msl");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            CliTestProject.SamplePath,
            "-o",
            target,
            "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
        Assert.Empty(report.RootElement.GetProperty("timings").EnumerateArray());
        Assert.False(Directory.Exists(parent));
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task MalformedJobSelectionIsRejectedBeforeProjectLoad()
    {
        using var directory = new TempDirectory();
        var missingProject = System.IO.Path.Combine(directory.Path, "missing.mocprj");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            missingProject,
            "--jobs",
            "1,,2",
            "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI005");
        Assert.DoesNotContain(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PROJ002");
        Assert.Empty(report.RootElement.GetProperty("timings").EnumerateArray());
    }

    [Fact]
    public async Task OutOfRangeJobSelectionNeedsTheProjectLoadAndExpansion()
    {
        using var directory = new TempDirectory();
        var missingProject = System.IO.Path.Combine(directory.Path, "missing.mocprj");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            missingProject,
            "--jobs",
            "99",
            "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.DoesNotContain(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI005");
    }

    [Fact]
    public async Task WarningsAsErrorsPromotesValidationWarningsToExitOne()
    {
        var (exitCode, stdout, _) = await RunAsync(
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--warnings-as-errors",
            "--json");

        Assert.Equal(1, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(1, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "ENM017");
        Assert.Equal("skipped", document.RootElement.GetProperty("verification").GetString());
    }

    [Fact]
    public async Task PromotedParameterizationWarningsMapToExitFourThroughTheCommandSeam()
    {
        var pipeline = new ScriptedBuildPipeline(request =>
        {
            Assert.True(request.WarningsAsErrors, "--warnings-as-errors must reach the pipeline.");
            return ScriptedBuildPipeline.Failure(
                BuildFailureKind.Parameterization,
                [new Diagnostic("PRM006", DiagnosticSeverity.Warning, "No explicit values were supplied.")]);
        });
        var command = new BuildCommand(pipeline, new PackageVerifier(), TimeProvider.System);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            command,
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--warnings-as-errors",
            "--json");

        Assert.Equal(4, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(4, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PRM006");
    }

    [Fact]
    public async Task VerificationFailureMapsToExitOneAndLeavesThePackage()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "invalid.msl");
        var command = new BuildCommand(
            new BuildPipeline(),
            new InvalidatingVerifier(),
            TimeProvider.System);

        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync(
            command,
            "build",
            test.Path,
            "-o",
            target,
            "--json");

        Assert.Equal(1, exitCode);
        Assert.True(File.Exists(target));
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal("failed", document.RootElement.GetProperty("verification").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PKG005");
        var finished = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => JsonElement.Parse(line))
            .Last(static element => element.GetProperty("event").GetString() == "build.finished");
        Assert.Equal(1, finished.GetProperty("exitCode").GetInt32());
        Assert.True(finished.GetProperty("published").GetBoolean());
    }

    [Fact]
    public async Task PreCancelledBuildMapsToExitSixWithCli006()
    {
        using var test = await CliTestProject.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new Mocassin.Cli.CommandLine.CliApplication();

        var exitCode = await application.RunAsync(
            ["build", test.Path, "--preview", "--json"],
            stdout,
            stderr,
            cancellation.Token);

        Assert.Equal(6, exitCode);
        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.Equal("skipped", document.RootElement.GetProperty("verification").GetString());
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
    }

    [Fact]
    public async Task MissingProjectIsAnIoFailure()
    {
        using var test = await CliTestProject.CreateAsync();

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            System.IO.Path.Combine(test.Directory, "missing.mocprj"),
            "--preview",
            "--json");

        Assert.Equal(3, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(3, document.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task ProjectWithoutAJobTemplateIsCli004()
    {
        var (exitCode, stdout, _) = await RunAsync(
            "build",
            RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj"),
            "--preview",
            "--json");

        Assert.Equal(2, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI004");
    }

    [Fact]
    public async Task UnknownBuildTemplateIsProj010AndExitOne()
    {
        using var test = await CliTestProject.CreateAsync();

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--template",
            Guid.NewGuid().ToString(),
            "--json");

        Assert.Equal(1, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "PROJ010");
    }

    [Fact]
    public async Task JobTemplatePassedAsBuildTemplateIsCli004()
    {
        using var test = await CliTestProject.CreateAsync();
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(test.Path));
        var jobTemplateId = document.RootElement.GetProperty("jobTemplates")[0].GetProperty("id").GetString();

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--template",
            jobTemplateId!,
            "--json");

        Assert.Equal(2, exitCode);
        using var result = JsonDocument.Parse(stdout);
        Assert.Contains(
            result.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI004"
                && diagnostic.GetProperty("message").GetString()!.Contains("--job-template", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MalformedJobSelectionIsCli005()
    {
        using var test = await CliTestProject.CreateAsync();

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--jobs",
            "0",
            "--json");

        Assert.Equal(2, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("jobSelection").GetProperty("expression").ValueKind);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI005");
    }

    [Fact]
    public async Task UsageErrorsProduceOneSchemaValidReport()
    {
        foreach (var option in new[] { "-o", "--force", "--compact" })
        {
            var arguments = new List<string> { "build", CliTestProject.SamplePath, "--preview", "--json" };
            if (option == "-o")
            {
                arguments.Add("-o");
                arguments.Add(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unused.msl"));
            }
            else
            {
                arguments.Add(option);
            }

            var (exitCode, stdout, stderr) = await RunAsync(arguments.ToArray());
            Assert.Equal(2, exitCode);
            JsonSchemaAssert.Matches("build-v1", stdout);
            Assert.Equal(string.Empty, stderr);
            using var document = JsonDocument.Parse(stdout);
            Assert.Equal("preview", document.RootElement.GetProperty("mode").GetString());
            Assert.Contains(
                document.RootElement.GetProperty("diagnostics").EnumerateArray(),
                static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
        }
    }

    [Fact]
    public async Task InternalScriptedFailureMapsToExitTenWithCli007()
    {
        var command = new BuildCommand(
            new ScriptedBuildPipeline(_ => ScriptedBuildPipeline.Failure(BuildFailureKind.Internal)),
            new PackageVerifier(),
            TimeProvider.System);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            command,
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--json");

        Assert.Equal(10, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Contains(
            document.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI007");
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);
}
