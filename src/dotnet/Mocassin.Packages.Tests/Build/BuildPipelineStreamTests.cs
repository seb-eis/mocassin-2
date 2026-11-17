// SPDX-License-Identifier: MIT

using Microsoft.Data.Sqlite;
using Mocassin.Packages.Build;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Packages.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildPipelineStreamTests
{
    [Fact]
    public async Task PreviewStreamEmitsOrderedStagesAndExactlyOneFinishedEvent()
    {
        using var test = await TestProject.CreateAsync();
        var events = new List<BuildEvent>();

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
        }))
        {
            events.Add(buildEvent);
        }

        var finished = events.OfType<BuildFinished>().ToArray();
        Assert.Single(finished);
        Assert.IsType<BuildFinished>(events[^1]);
        Assert.Equal(BuildStatus.Succeeded, finished[0].Result.Status);

        var started = events.OfType<BuildStageStarted>().Select(static item => item.Stage).ToArray();
        var completed = events.OfType<BuildStageCompleted>().Select(static item => item.Stage).ToArray();
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
            started);
        Assert.Equal(started, completed);
        Assert.DoesNotContain(BuildStage.GenerateAndWriteJobs, started);
        Assert.DoesNotContain(BuildStage.Publish, started);

        var buildStartedIndex = events.FindIndex(static item => item is BuildStarted);
        var lastStageIndex = events.FindIndex(static item => item is BuildStageCompleted { Stage: BuildStage.ExpandJobs });
        Assert.True(lastStageIndex < buildStartedIndex);
        var buildStarted = (BuildStarted)events[buildStartedIndex];
        Assert.Equal(6, buildStarted.ExpandedJobCount);
        Assert.Equal(6, buildStarted.SelectedJobCount);

        var diagnosticEvents = events.OfType<BuildDiagnostic>()
            .Select(static item => (item.Diagnostic.Code, item.Diagnostic.Path, item.Diagnostic.Message))
            .ToArray();
        var resultDiagnostics = finished[0].Result.Diagnostics
            .Select(static item => (item.Code, item.Path, item.Message))
            .ToArray();
        Assert.Equal(resultDiagnostics.ToHashSet(), diagnosticEvents.ToHashSet());
        Assert.Equal(diagnosticEvents.Length, diagnosticEvents.Distinct().Count());
    }

    [Fact]
    public async Task ChannelCapacityOneDoesNotLoseEvents()
    {
        using var test = await TestProject.CreateAsync();
        var small = new List<BuildEvent>();
        var large = new List<BuildEvent>();

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            Options = new BuildOptions { EventChannelCapacity = 1 },
        }))
        {
            small.Add(buildEvent);
        }

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            Preview = true,
            Options = new BuildOptions { EventChannelCapacity = 256 },
        }))
        {
            large.Add(buildEvent);
        }

        Assert.Equal(large.Count, small.Count);
        Assert.Equal(
            large.Select(static item => item.GetType()),
            small.Select(static item => item.GetType()));
    }

    [Fact]
    public async Task ProgressIsMonotonicAndEndsAtTheSelectedCount()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "progress.msl");
        var progress = new List<BuildProgress>();

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            Options = new BuildOptions { ProgressInterval = TimeSpan.Zero },
        }))
        {
            if (buildEvent is BuildProgress item)
            {
                progress.Add(item);
            }
        }

        Assert.Equal(6, progress.Count);
        Assert.Equal(Enumerable.Range(1, 6), progress.Select(static item => item.Completed));
        Assert.All(progress, static item => Assert.Equal(6, item.Total));
        Assert.Equal("job-00006", progress[^1].Current);
    }

    [Fact]
    public async Task AFixedClockEmitsOnlyTheLastProgress()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "progress.msl");
        var clock = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        var progress = new List<BuildProgress>();

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            Options = new BuildOptions
            {
                ProgressInterval = TimeSpan.FromMilliseconds(100),
                TimeProvider = clock,
            },
        }))
        {
            if (buildEvent is BuildProgress item)
            {
                progress.Add(item);
            }
        }

        var only = Assert.Single(progress);
        Assert.Equal(6, only.Completed);
        Assert.Equal(TimeSpan.Zero, only.Elapsed);
    }

    [Fact]
    public async Task APreCancelledRequestFinishesOnceWithACancelledResult()
    {
        using var test = await TestProject.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var events = new List<BuildEvent>();

        await foreach (var buildEvent in BuildPipelineTestFactory.Create().BuildAsync(
                           new BuildRequest { ProjectPath = test.Path, Preview = true },
                           cancellation.Token))
        {
            events.Add(buildEvent);
        }

        var finished = Assert.Single(events.OfType<BuildFinished>());
        Assert.Equal(BuildStatus.Cancelled, finished.Result.Status);
        Assert.Equal(BuildFailureKind.None, finished.Result.Failure);
        Assert.IsType<BuildFinished>(events[^1]);
    }

    [Fact]
    public async Task AnAbsentCreatedUtcUsesTheInjectedClock()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "clock.msl");
        var now = new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
        var pipeline = BuildPipelineTestFactory.Create();

        var result = await pipeline.BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            Options = new BuildOptions { TimeProvider = new FixedTimeProvider(now) },
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        using var connection = new SqliteConnection($"Data Source={target};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM schema_info WHERE key = 'created_utc'";
        var created = DateTimeOffset.Parse(
            (string)command.ExecuteScalar()!,
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(now, created);
    }

    [Fact]
    public async Task CancellationBeforePublicationLeavesNoTemporaryFileAndPreservesTheTarget()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "existing.msl");
        var original = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(target, original);
        using var cancellation = new CancellationTokenSource();
        var pipeline = BuildPipelineTestFactory.Create(
            writerFactory: (request, options) => new HookedWriter(
                SimulationPackageWriter.Create(request, options),
                cancellation.Cancel));

        var result = await pipeline.BuildToCompletionAsync(
            new BuildRequest
            {
                ProjectPath = test.Path,
                TargetPath = target,
                Overwrite = true,
                CreatedUtc = DateTimeOffset.UnixEpoch,
            },
            cancellation.Token);

        Assert.Equal(BuildStatus.Cancelled, result.Status);
        Assert.Equal(BuildFailureKind.None, result.Failure);
        Assert.Null(result.Package);
        Assert.Equal(original, await File.ReadAllBytesAsync(target));
        Assert.Empty(Directory.GetFiles(test.Directory, "*.tmp-*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ParallelismDoesNotChangeThePublishedBytesOrTheJobOrder()
    {
        using var test = await TestProject.CreateAsync();
        var serialTarget = System.IO.Path.Combine(test.Directory, "serial.msl");
        var parallelTarget = System.IO.Path.Combine(test.Directory, "parallel.msl");

        var serial = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = serialTarget,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            MaxDegreeOfParallelism = 1,
        });
        var parallel = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = parallelTarget,
            CreatedUtc = DateTimeOffset.UnixEpoch,
            MaxDegreeOfParallelism = 4,
        });

        Assert.Equal(BuildStatus.Succeeded, serial.Status);
        Assert.Equal(BuildStatus.Succeeded, parallel.Status);
        Assert.Equal(serial.Package!.Sha256, parallel.Package!.Sha256);
        Assert.Equal(serial.Package.JobCount, parallel.Package.JobCount);
        Assert.Equal(ReadJobIds(serialTarget), ReadJobIds(parallelTarget));
        Assert.Equal(Enumerable.Range(1, 6).ToArray(), ReadJobIds(serialTarget).ToArray());
    }

    [Fact]
    public async Task ASelectedSubsetKeepsFullExpansionIdsAndNormalizesTheExpression()
    {
        using var test = await TestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "partial.msl");

        var result = await BuildPipelineTestFactory.Create().BuildToCompletionAsync(new BuildRequest
        {
            ProjectPath = test.Path,
            TargetPath = target,
            JobSelection = "5,2,2",
            CreatedUtc = DateTimeOffset.UnixEpoch,
        });

        Assert.Equal(BuildStatus.Succeeded, result.Status);
        Assert.Equal("2,5", result.JobSelection!.Expression);
        Assert.Equal([2, 5], result.JobSelection.JobIds.ToArray());
        Assert.Equal([2, 5], ReadJobIds(target).ToArray());
        Assert.Equal("2,5", ReadJobSelection(target));
        Assert.Equal(2, result.Package!.JobCount);
    }

    private static List<int> ReadJobIds(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT job_id FROM jobs ORDER BY job_id";
        using var reader = command.ExecuteReader();
        var ids = new List<int>();
        while (reader.Read())
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    private static string ReadJobSelection(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM schema_info WHERE key = 'job_selection'";
        return (string)command.ExecuteScalar()!;
    }
}
