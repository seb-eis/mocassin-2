// SPDX-License-Identifier: MIT

using Mocassin.Packages.Build;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildPreviewEqualityTests
{
    [Fact]
    public async Task PreviewPayloadMatchesTheStep10ObservableValues()
    {
        using var test = await TestProject.CreateAsync();

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        var preview = result.Preview!;
        Assert.Equal("Ceria temperature and doping sweep", preview.JobTemplateName);
        Assert.Equal(6, preview.TotalJobs);
        Assert.Equal(2, preview.Collections.Length);
        Assert.True(preview.Elapsed >= TimeSpan.Zero);

        var collection = preview.Collections[0];
        Assert.Equal("kmc", collection.Kind);
        Assert.True(collection.UseDualDofCorrection);
        Assert.Equal(4, collection.JobCount);
        Assert.Equal(2, collection.ConfigCount);
        var optimizer = Assert.Single(collection.Optimizers);
        Assert.Equal(1, optimizer.TargetCount);
        Assert.Equal(0, optimizer.RemovedStartOptions);
        var config = collection.Configs[0];
        Assert.Equal(6, config.CellA);
        Assert.Equal(6, config.CellB);
        Assert.Equal(6, config.CellC);
        Assert.Equal(864, config.PositionCount);
        Assert.True(config.IsChargeNeutral);
        var doping = config.Dopings[0];
        Assert.Equal(0.01, doping.RequestedFraction);
        Assert.Equal(2, doping.AppliedPrimary);
        Assert.Equal(1, doping.AppliedDependent);
    }

    [Fact]
    public async Task PartialPreviewFiltersTheCollectionsAndKeepsTheExpandedCount()
    {
        using var test = await TestProject.CreateAsync();

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            JobSelection = "2,5",
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        Assert.Equal(2, result.Preview!.TotalJobs);
        Assert.Equal(2, result.Preview.Collections.Length);
        Assert.Equal(1, result.Preview.Collections[0].JobCount);
        Assert.Equal(1, result.Preview.Collections[1].JobCount);
        Assert.Equal(6, result.JobSelection!.ExpandedCount);
        Assert.Equal(2, result.JobSelection.SelectedCount);
        Assert.Equal("2,5", result.JobSelection.Expression);
    }

    [Fact]
    public async Task APreviewNeverCreatesAPackageEvenWhenATargetPathIsSupplied()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "ignored.msl");

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            TargetPath = target,
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        Assert.Null(result.Package);
        Assert.False(File.Exists(target));
    }
}
