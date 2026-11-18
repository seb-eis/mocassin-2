// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.Compile;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Compilation.Caching;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Cli.Tests.Compile;

[Trait("Tier", "PR-fast")]
public sealed class CompileCommandTests
{
    private static readonly string sample = RepositoryPaths.Combine(
        "samples",
        "simulations",
        "ceria-kmc.mocprj");

    [Fact]
    public async Task ReportIsSchemaValidAndMatchesTheDirectCompilation()
    {
        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync("compile", sample, "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("compile-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("compile", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("outputPath").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("outputSha256").ValueKind);
        Assert.Equal("mocassin-compilation/1", root.GetProperty("compilerVersion").GetString());

        var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
            .LoadAsync(sample);
        var validation = new ProjectValidationPipeline().Validate(
            load.Project!,
            loadDiagnostics: load.Diagnostics,
            options: new ValidationOptions { RunBuildStage = false });
        var compilation = new ModelCompiler().Compile(load.Project!);
        Assert.Equal(validation.ProjectId, root.GetProperty("projectId").GetGuid());
        Assert.Equal(validation.ModelHash, root.GetProperty("modelHash").GetString());
        Assert.Equal(validation.DocumentHash, root.GetProperty("documentHash").GetString());
        Assert.Equal(compilation.Model!.ModelSignature, root.GetProperty("modelSignature").GetString());
        Assert.Equal(compilation.Report.Counts.Positions, root.GetProperty("counts").GetProperty("positionCount").GetInt32());
        Assert.Equal(compilation.Report.Cache.Misses, root.GetProperty("cache").GetProperty("misses").GetInt32());
        Assert.Equal(10, root.GetProperty("stages").GetArrayLength());
    }

    [Fact]
    public async Task OutputWritesTheCanonicalInspectionDocumentAndItsHash()
    {
        using var directory = new TempDirectory();
        var output = System.IO.Path.Combine(directory.Path, "inspection.json");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "compile",
            sample,
            "-o",
            output,
            "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(output, report.RootElement.GetProperty("outputPath").GetString());
        var bytes = await File.ReadAllBytesAsync(output);
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(bytes)),
            report.RootElement.GetProperty("outputSha256").GetString());
        using var inspection = JsonDocument.Parse(bytes);
        Assert.Equal("compiled-inspection-v1", inspection.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public async Task WithoutOutputNoInspectionDocumentIsWritten()
    {
        using var directory = new TempDirectory();
        var projectPath = System.IO.Path.Combine(directory.Path, "ceria.mocprj");
        await File.WriteAllBytesAsync(projectPath, await File.ReadAllBytesAsync(sample));

        var (exitCode, _, _) = await CliTestRunner.RunAsync("compile", projectPath);

        Assert.Equal(0, exitCode);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.json"));
    }

    [Fact]
    public async Task OutputReplacesAnExistingDerivedFile()
    {
        using var directory = new TempDirectory();
        var output = System.IO.Path.Combine(directory.Path, "inspection.json");
        await File.WriteAllTextAsync(output, "stale");

        var (exitCode, _, _) = await CliTestRunner.RunAsync("compile", sample, "-o", output, "--json");

        Assert.Equal(0, exitCode);
        Assert.Contains("compiled-inspection-v1", await File.ReadAllTextAsync(output), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HumanOutputSummarizesCountsAndSignature()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("compile", sample);

        Assert.Equal(0, exitCode);
        Assert.Contains("Model signature:", stdout, StringComparison.Ordinal);
        Assert.Contains("Counts: particles", stdout, StringComparison.Ordinal);
        Assert.Contains("Cache:", stdout, StringComparison.Ordinal);
        Assert.Contains("Stages:", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingProjectIsExitThree()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "compile",
            System.IO.Path.Combine(directory.Path, "missing.mocprj"),
            "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("compile-v1", stdout);
    }

    [Fact]
    public async Task MalformedProjectIsExitOne()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "bad.mocprj");
        await File.WriteAllTextAsync(path, "{ not json");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("compile", path, "--json");

        Assert.Equal(1, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(1, report.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task ValidationErrorsExitOneBeforeCompilation()
    {
        var invalid = RepositoryPaths.Combine("tests", "contracts", "project-transitions-populated.mocprj");

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("compile", invalid, "--json");

        Assert.Equal(1, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(JsonValueKind.Null, report.RootElement.GetProperty("compilerVersion").ValueKind);
        Assert.Equal(JsonValueKind.Null, report.RootElement.GetProperty("counts").ValueKind);
    }

    [Fact]
    public async Task CompilationErrorsExitFour()
    {
        var command = new CompileCommand(new FailingCompiler());
        var application = new CliApplication(
            new CliCommandGraph(),
            new Mocassin.Cli.Build.BuildCommand(),
            compileCommand: command);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(application, "compile", sample, "--json");

        Assert.Equal(4, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(4, report.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CMP001");
    }

    [Fact]
    public async Task OutputDirectoryIsExitThree()
    {
        using var directory = new TempDirectory();
        var output = System.IO.Path.Combine(directory.Path, "target");
        Directory.CreateDirectory(output);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "compile",
            sample,
            "-o",
            output,
            "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task MissingOutputParentIsExitThree()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            "compile",
            sample,
            "-o",
            System.IO.Path.Combine(directory.Path, "missing", "inspection.json"),
            "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(3, report.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task PreCancelledCompileIsExitSixWithCli006()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var application = new CliApplication();

        var exitCode = await application.RunAsync(
            ["compile", sample, "--json"],
            stdout,
            stderr,
            cancellation.Token);

        Assert.Equal(6, exitCode);
        using var report = JsonDocument.Parse(stdout.ToString());
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI006");
    }

    [Fact]
    public async Task AParseErrorEmitsOneCompileReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("compile", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("compile-v1", stdout);
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneCompileReport()
    {
        var (exitCode, stdout, _) = await CliTestRunner.RunAsync("compile", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("compile-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
    }

    private sealed class FailingCompiler : IModelCompiler
    {
        public CompilationResult Compile(
            Project project,
            CompilationOptions? options = null,
            CancellationToken cancellationToken = default) =>
            new(
                CompilationStatus.Failed,
                model: null,
                [new Diagnostic("CMP001", DiagnosticSeverity.Error, "Simulated compilation failure.")],
                new CompilationReport(
                    "fake-compiler/1",
                    modelSignature: null,
                    symmetryContractVersion: "1",
                    spaceGroupDataVersion: null,
                    spaceGroupDataSha256: null,
                    stages: [],
                    cache: CompilationCacheStatistics.Empty,
                    summary: "failed"));
    }
}
