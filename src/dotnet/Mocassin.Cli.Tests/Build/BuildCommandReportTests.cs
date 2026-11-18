// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Compilation.Compiler;
using Mocassin.Packages.Build;
using Mocassin.Packages.Projects;

namespace Mocassin.Cli.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildCommandReportTests
{
    [Fact]
    public async Task PreviewReportIsSchemaValidAndCarriesThePipelinePayload()
    {
        using var test = await CliTestProject.CreateAsync();
        var (exitCode, stdout, stderr) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--json");

        Assert.Equal(0, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("build", root.GetProperty("kind").GetString());
        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.Equal("preview", root.GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("package").ValueKind);
        Assert.Equal("skipped", root.GetProperty("verification").GetString());
        Assert.Equal(6, root.GetProperty("preview").GetProperty("totalJobs").GetInt32());
        Assert.Equal(
            ["load", "select", "validate", "compile", "validateBuild", "parameterize", "expand", "preview"],
            root.GetProperty("timings").EnumerateArray().Select(static item => item.GetProperty("stage").GetString()));
        Assert.Equal(JsonValueKind.Null, root.GetProperty("selection").GetProperty("buildTemplate").ValueKind);
        Assert.True(root.GetProperty("selection").GetProperty("parameterization").GetProperty("implicitDefault").GetBoolean());
        Assert.True(root.GetProperty("summary").GetProperty("warnings").GetInt32() > 0);
        Assert.All(
            stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            static line => Assert.Equal(1, JsonElement.Parse(line).GetProperty("schemaVersion").GetInt32()));
        Assert.Empty(Directory.GetFiles(test.Directory, "*.msl", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task FullBuildReportCarriesThePackageAndVerification()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "ceria.msl");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--json");

        Assert.Equal(0, exitCode);
        JsonSchemaAssert.Matches("build-v1", stdout);
        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("build", root.GetProperty("mode").GetString());
        Assert.Equal("passed", root.GetProperty("verification").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("preview").ValueKind);
        var package = root.GetProperty("package");
        Assert.Equal(target, package.GetProperty("path").GetString());
        Assert.True(package.GetProperty("published").GetBoolean());
        Assert.Equal(6, package.GetProperty("jobCount").GetInt32());
        Assert.Equal(1, package.GetProperty("modelCount").GetInt32());
        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(target))),
            package.GetProperty("sha256").GetString());
        Assert.Equal(
            ["load", "select", "validate", "compile", "validateBuild", "parameterize", "expand", "generate", "publish", "verify"],
            root.GetProperty("timings").EnumerateArray().Select(static item => item.GetProperty("stage").GetString()));
        Assert.Equal("all", ReadJobSelection(target));
    }

    [Fact]
    public async Task PartialSelectionKeepsFullExpansionIds()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "partial.msl");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--jobs",
            "5,2,2",
            "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout);
        var jobSelection = document.RootElement.GetProperty("jobSelection");
        Assert.Equal("2,5", jobSelection.GetProperty("expression").GetString());
        Assert.Equal(6, jobSelection.GetProperty("expandedCount").GetInt32());
        Assert.Equal(2, jobSelection.GetProperty("selectedCount").GetInt32());
        Assert.Equal(2, document.RootElement.GetProperty("package").GetProperty("jobCount").GetInt32());
        Assert.Equal("2,5", ReadJobSelection(target));
    }

    [Fact]
    public async Task BuildTemplateAndParameterizationOverridesAreReported()
    {
        var load = await new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
            .LoadAsync(CliTestProject.SamplePath);
        var model = new ModelCompiler().Compile(load.Project!).Model!;
        const string parameterizationId = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a01";
        const string buildTemplateId = "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6b01";
        using var test = await CliTestProject.CreateAsync(root =>
        {
            CliTestProject.AddEmptyParameterization(root, parameterizationId, model.ModelSignature);
            CliTestProject.AddBuildTemplate(
                root,
                buildTemplateId,
                "Named build",
                CliTestProject.JobTemplateId(root),
                parameterizationId,
                "named-output");
        });

        var (previewExit, previewJson, _) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--template",
            buildTemplateId,
            "--json");

        Assert.Equal(0, previewExit);
        using var preview = JsonDocument.Parse(previewJson);
        var selection = preview.RootElement.GetProperty("selection");
        Assert.Equal(buildTemplateId, selection.GetProperty("buildTemplate").GetProperty("id").GetString());
        Assert.Equal("named-output", selection.GetProperty("outputName").GetString());
        Assert.Equal(parameterizationId, selection.GetProperty("parameterization").GetProperty("id").GetString());

        var target = System.IO.Path.Combine(test.Directory, "explicit.msl");
        var (buildExit, buildJson, _) = await RunAsync(
            "build",
            test.Path,
            "--template",
            buildTemplateId,
            "-o",
            target,
            "--json");

        Assert.Equal(0, buildExit);
        using var build = JsonDocument.Parse(buildJson);
        Assert.Equal(JsonValueKind.Null, build.RootElement.GetProperty("selection").GetProperty("outputName").ValueKind);
        Assert.Equal(target, build.RootElement.GetProperty("package").GetProperty("path").GetString());
    }

    [Fact]
    public async Task ExplicitJobTemplateIsAccepted()
    {
        using var test = await CliTestProject.CreateAsync();
        var text = JsonDocument.Parse(await File.ReadAllTextAsync(test.Path));
        var jobTemplateId = text.RootElement.GetProperty("jobTemplates")[0].GetProperty("id").GetString();

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "--preview",
            "--job-template",
            jobTemplateId!,
            "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal(
            jobTemplateId,
            document.RootElement.GetProperty("selection").GetProperty("jobTemplate").GetProperty("id").GetString());
    }

    [Fact]
    public async Task CompactPackageStillVerifies()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "compact.msl");

        var (exitCode, stdout, _) = await RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--compact",
            "--json");

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout);
        Assert.Equal("passed", document.RootElement.GetProperty("verification").GetString());
        Assert.True(File.Exists(target));
    }

    [Fact]
    public async Task OmittedParallelUsesTheProcessorCount()
    {
        var pipeline = new ScriptedBuildPipeline(_ => ScriptedBuildPipeline.Failure(BuildFailureKind.Validation));
        var command = new Mocassin.Cli.Build.BuildCommand(
            pipeline,
            new Mocassin.Results.Packages.PackageVerifier(),
            TimeProvider.System);

        var (exitCode, _, _) = await CliTestRunner.RunAsync(
            command,
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--json");

        Assert.Equal(1, exitCode);
        Assert.NotNull(pipeline.LastRequest);
        Assert.Equal(Environment.ProcessorCount, pipeline.LastRequest!.MaxDegreeOfParallelism);
    }

    [Fact]
    public async Task ParallelismProducesTheSamePackageBytesWithAFixedClock()
    {
        using var test = await CliTestProject.CreateAsync();
        var serialTarget = System.IO.Path.Combine(test.Directory, "serial.msl");
        var parallelTarget = System.IO.Path.Combine(test.Directory, "parallel.msl");
        var clock = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        var serialCommand = new Mocassin.Cli.Build.BuildCommand(
            new Mocassin.Packages.Build.BuildPipeline(),
            new Mocassin.Results.Packages.PackageVerifier(),
            clock);
        var parallelCommand = new Mocassin.Cli.Build.BuildCommand(
            new Mocassin.Packages.Build.BuildPipeline(),
            new Mocassin.Results.Packages.PackageVerifier(),
            clock);

        var (serialExit, serialJson, _) = await CliTestRunner.RunAsync(
            serialCommand,
            "build",
            test.Path,
            "-o",
            serialTarget,
            "--parallel",
            "1",
            "--json");
        var (parallelExit, parallelJson, _) = await CliTestRunner.RunAsync(
            parallelCommand,
            "build",
            test.Path,
            "-o",
            parallelTarget,
            "--parallel",
            "4",
            "--json");

        Assert.Equal(0, serialExit);
        Assert.Equal(0, parallelExit);
        using var serial = JsonDocument.Parse(serialJson);
        using var parallel = JsonDocument.Parse(parallelJson);
        Assert.Equal(
            serial.RootElement.GetProperty("package").GetProperty("sha256").GetString(),
            parallel.RootElement.GetProperty("package").GetProperty("sha256").GetString());
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);

    private static string ReadJobSelection(string path)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM schema_info WHERE key = 'job_selection'";
        return (string)command.ExecuteScalar()!;
    }
}
