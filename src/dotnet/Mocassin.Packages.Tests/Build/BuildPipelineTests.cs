// SPDX-License-Identifier: MIT

using Mocassin.Packages.Build;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildPipelineTests
{
    [Fact]
    public async Task PreviewBuildsTheTypedPayloadWithoutWritingAPackage()
    {
        using var test = await TestProject.CreateAsync();
        var pipeline = new BuildPipeline();

        var result = await pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            JobSelection = "all",
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        Assert.Equal(BuildFailureKind.None, result.Failure);
        Assert.NotNull(result.Preview);
        Assert.Null(result.Package);
        Assert.Equal("all", result.JobSelection!.Expression);
        Assert.Equal(6, result.JobSelection.SelectedCount);
        Assert.Equal(6, result.JobSelection.ExpandedCount);
        Assert.Equal(6, result.Preview.TotalJobs);
        Assert.Equal(2, result.Preview.Collections.Length);
        Assert.NotNull(result.Identity.ProjectId);
        Assert.NotNull(result.Identity.ModelHash);
        Assert.NotNull(result.Identity.DocumentHash);
        Assert.NotNull(result.Identity.ModelSignature);
        Assert.NotNull(result.Identity.ParameterizationSignature);
        Assert.Equal(
            [
                BuildStage.Load,
                BuildStage.Select,
                BuildStage.Validate,
                BuildStage.Compile,
                BuildStage.ValidateBuild,
                BuildStage.Parameterize,
                BuildStage.ExpandJobs,
            ],
            result.Timings.Select(static timing => timing.Stage));
        Assert.Empty(Directory.GetFiles(test.Directory, "*.msl", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task FullBuildPublishesTheSelectedPackage()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "out.msl");
        var pipeline = new BuildPipeline();

        var result = await pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            CreatedUtc = DateTimeOffset.UnixEpoch,
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        Assert.Equal(BuildFailureKind.None, result.Failure);
        Assert.Null(result.Preview);
        Assert.NotNull(result.Package);
        Assert.True(File.Exists(target));
        Assert.Equal(6, result.Package.JobCount);
        Assert.Equal(1, result.Package.ModelCount);
        Assert.Empty(result.Package.Warnings);
        Assert.Equal(
            [
                BuildStage.Load,
                BuildStage.Select,
                BuildStage.Validate,
                BuildStage.Compile,
                BuildStage.ValidateBuild,
                BuildStage.Parameterize,
                BuildStage.ExpandJobs,
                BuildStage.GenerateAndWriteJobs,
                BuildStage.Publish,
            ],
            result.Timings.Select(static timing => timing.Stage));
        Assert.Equal("all", ReadJobSelection(target));
    }

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
