// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.CommandLine;
using Mocassin.Cli.NewProject;
using Mocassin.Cli.Tests.CommandLine;
using Mocassin.Cli.Tests.TestSupport;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.Cli.Tests.NewProject;

[Trait("Tier", "PR-fast")]
public sealed class NewProjectCommandTests
{
    [Fact]
    public async Task BlankProjectIsWrittenAndReportedWithAFreshIdentity()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "First.mocprj");

        var (exitCode, stdout, stderr) = await RunAsync(
            "new",
            "First",
            "-o",
            directory.Path,
            "--json");

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal("newProject", report.RootElement.GetProperty("kind").GetString());
        Assert.Equal(path, report.RootElement.GetProperty("projectPath").GetString());
        Assert.Equal("blank", report.RootElement.GetProperty("template").GetString());

        var project = ProjectJson.Deserialize(await File.ReadAllTextAsync(path));
        Assert.Equal("First", project.Metadata.Name);
        Assert.Equal(report.RootElement.GetProperty("projectId").GetGuid(), project.Metadata.ProjectId);
        Assert.Equal(64, project.Metadata.MasterSeed.Length);
        Assert.Empty(project.Model.Particles);
        Assert.Empty(project.JobTemplates);
    }

    [Fact]
    public async Task TwoProjectsHaveDifferentIdentitiesAndSeeds()
    {
        using var directory = new TempDirectory();

        Assert.Equal(0, (await RunAsync("new", "One", "-o", directory.Path)).ExitCode);
        Assert.Equal(0, (await RunAsync("new", "Two", "-o", directory.Path)).ExitCode);

        var first = ProjectJson.Deserialize(await File.ReadAllTextAsync(
            System.IO.Path.Combine(directory.Path, "One.mocprj")));
        var second = ProjectJson.Deserialize(await File.ReadAllTextAsync(
            System.IO.Path.Combine(directory.Path, "Two.mocprj")));

        Assert.NotEqual(first.Metadata.ProjectId, second.Metadata.ProjectId);
        Assert.NotEqual(first.Metadata.MasterSeed, second.Metadata.MasterSeed);
    }

    [Fact]
    public async Task CeriaStarterMatchesTheAuthoredModel()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "Ceria.mocprj");

        var (exitCode, stdout, _) = await RunAsync(
            "new",
            "Ceria",
            "-o",
            directory.Path,
            "--template",
            "ceria",
            "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal("ceria", report.RootElement.GetProperty("template").GetString());
        var created = ProjectJson.Deserialize(await File.ReadAllTextAsync(path));
        var sample = ProjectJson.Deserialize(await File.ReadAllTextAsync(
            RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj")));
        Assert.Equal(ProjectHasher.Compute(sample), ProjectHasher.Compute(created));
        Assert.NotEmpty(created.Model.Particles);
        Assert.Empty(created.JobTemplates);
    }

    [Fact]
    public async Task TheMocprjSuffixIsStrippedAndWhitespaceTrimmed()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await RunAsync(
            "new",
            "  Named.mocprj  ",
            "-o",
            directory.Path,
            "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(
            System.IO.Path.Combine(directory.Path, "Named.mocprj"),
            report.RootElement.GetProperty("projectPath").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("..")]
    [InlineData("name:bad")]
    [InlineData("name*bad")]
    [InlineData("name?bad")]
    [InlineData("name\"bad")]
    [InlineData("name<bad")]
    [InlineData("name>bad")]
    [InlineData("name|bad")]
    [InlineData("a\tb")]
    [InlineData(".mocprj")]
    public async Task InvalidNamesAreCli001(string name)
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await RunAsync(
            "new",
            name,
            "-o",
            directory.Path,
            "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task AnOverlongNameIsCli001()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await RunAsync(
            "new",
            new string('a', 129),
            "-o",
            directory.Path,
            "--json");

        Assert.Equal(2, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task AnExistingProjectIsCli003AndIsPreserved()
    {
        using var directory = new TempDirectory();
        var path = System.IO.Path.Combine(directory.Path, "Taken.mocprj");
        var original = "original";
        await File.WriteAllTextAsync(path, original);

        var (exitCode, stdout, _) = await RunAsync("new", "Taken", "-o", directory.Path, "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI003");
        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task ADirectoryAtTheProjectPathIsAnIoError()
    {
        using var directory = new TempDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "Taken.mocprj"));

        var (exitCode, stdout, _) = await RunAsync("new", "Taken", "-o", directory.Path, "--json");

        Assert.Equal(3, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task TheOutputDirectoryIsCreated()
    {
        using var directory = new TempDirectory();
        var nested = System.IO.Path.Combine(directory.Path, "nested", "deeper");

        var (exitCode, _, _) = await RunAsync("new", "Nested", "-o", nested);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(System.IO.Path.Combine(nested, "Nested.mocprj")));
    }

    [Fact]
    public async Task HumanOutputNamesTheCreatedProject()
    {
        using var directory = new TempDirectory();

        var (exitCode, stdout, _) = await RunAsync("new", "Human", "-o", directory.Path);

        Assert.Equal(0, exitCode);
        Assert.Contains("Created project:", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AParseErrorEmitsOneNewProjectReport()
    {
        var (exitCode, stdout, _) = await RunAsync("new", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(2, report.RootElement.GetProperty("exitCode").GetInt32());
    }

    [Fact]
    public async Task AResponseFileFailureEmitsOneNewProjectReport()
    {
        var (exitCode, stdout, _) = await RunAsync("new", "@missing.rsp", "--json");

        Assert.Equal(2, exitCode);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI001");
    }

    [Fact]
    public async Task ATemplateDefectIsAnInternalError()
    {
        using var directory = new TempDirectory();
        var command = new NewProjectCommand(
            (kind, name, timestamp) =>
            {
                var project = ProjectTemplateFactory.CreateProject(kind, name, timestamp);
                return project with
                {
                    Metadata = project.Metadata with { MasterSeed = "not-a-valid-seed" },
                };
            },
            TimeProvider.System);
        var application = new CliApplication(new CliCommandGraph(), new Mocassin.Cli.Build.BuildCommand(), command);

        var (exitCode, stdout, _) = await CliTestRunner.RunAsync(
            application,
            "new",
            "Defect",
            "-o",
            directory.Path,
            "--json");

        Assert.Equal(10, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI007");
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task AnEmptyOutputDirectoryIsCli002AndExitThree()
    {
        var (exitCode, stdout, _) = await RunAsync("new", "EmptyTarget", "-o", "", "--json");

        Assert.Equal(3, exitCode);
        JsonSchemaAssert.Matches("new-project-v1", stdout);
        using var report = JsonDocument.Parse(stdout);
        Assert.Contains(
            report.RootElement.GetProperty("diagnostics").EnumerateArray(),
            static diagnostic => diagnostic.GetProperty("code").GetString() == "CLI002");
    }

    [Fact]
    public async Task AMissingOutputDirectoryIsCreated()
    {
        using var directory = new TempDirectory();
        var output = System.IO.Path.Combine(directory.Path, "missing", "nested");

        var (exitCode, stdout, _) = await RunAsync("new", "Nested", "-o", output, "--json");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(System.IO.Path.Combine(output, "Nested.mocprj")));
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(
            System.IO.Path.Combine(output, "Nested.mocprj"),
            report.RootElement.GetProperty("projectPath").GetString());
    }

    private static Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(params string[] arguments) =>
        CliTestRunner.RunAsync(arguments);
}
