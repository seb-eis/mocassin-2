// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Cli.Tests.CommandLine;

namespace Mocassin.Cli.Tests.Build;

[Trait("Tier", "PR-fast")]
public sealed class BuildCommandEventTests
{
    [Fact]
    public async Task JsonStreamIsStderrOnlyAndEndsWithOneFinishedEvent()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "events.msl");

        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--json");

        Assert.Equal(0, exitCode);
        using var report = JsonDocument.Parse(stdout);
        Assert.Equal(0, report.RootElement.GetProperty("exitCode").GetInt32());

        using var schema = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("contracts", "generated", "schema", "events-v1.schema.json")));
        var eventNames = schema.RootElement.GetProperty("properties").GetProperty("event").GetProperty("enum")
            .EnumerateArray().Select(static item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
        var requiredFields = schema.RootElement.GetProperty("x-event-required-fields");
        var events = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => JsonElement.Parse(line))
            .ToArray();

        Assert.NotEmpty(events);
        foreach (var element in events)
        {
            Assert.Equal(1, element.GetProperty("schemaVersion").GetInt32());
            var name = element.GetProperty("event").GetString()!;
            Assert.Contains(name, eventNames);
            foreach (var field in requiredFields.GetProperty(name).EnumerateArray())
            {
                Assert.True(
                    element.TryGetProperty(field.GetString()!, out _),
                    $"Event '{name}' is missing required field '{field.GetString()}'.");
            }
        }

        var finished = events.Where(static element => element.GetProperty("event").GetString() == "build.finished")
            .ToArray();
        Assert.Single(finished);
        Assert.Equal("build.finished", events[^1].GetProperty("event").GetString());
        Assert.Equal(0, finished[0].GetProperty("exitCode").GetInt32());
        Assert.True(finished[0].GetProperty("published").GetBoolean());
        var firstStage = Array.FindIndex(
            events,
            static element => element.GetProperty("event").GetString() == "build.stage");
        var started = Array.FindIndex(
            events,
            static element => element.GetProperty("event").GetString() == "build.started");
        Assert.True(firstStage >= 0 && started >= 0 && firstStage < started);
    }

    [Fact]
    public async Task QuietJsonKeepsOnlyDiagnosticsStartedAndFinished()
    {
        var (exitCode, _, stderr) = await CliTestRunner.RunAsync(
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--json",
            "--quiet");

        Assert.Equal(0, exitCode);
        var events = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => JsonElement.Parse(line).GetProperty("event").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("build.stage", events);
        Assert.DoesNotContain("build.progress", events);
        Assert.DoesNotContain("log", events);
        Assert.Contains("diagnostic", events);
        Assert.Contains("build.started", events);
        Assert.Contains("build.finished", events);
    }

    [Fact]
    public async Task ProgressIsMonotonicAndEndsAtTheSelectedCount()
    {
        using var test = await CliTestProject.CreateAsync();
        var target = System.IO.Path.Combine(test.Directory, "progress.msl");

        var (exitCode, _, stderr) = await CliTestRunner.RunAsync(
            "build",
            test.Path,
            "-o",
            target,
            "--json");

        Assert.Equal(0, exitCode);
        var progress = stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => JsonElement.Parse(line))
            .Where(static element => element.GetProperty("event").GetString() == "build.progress")
            .Select(static element => element.GetProperty("completed").GetInt32())
            .ToArray();
        Assert.NotEmpty(progress);
        Assert.Equal(progress.Order(), progress);
        Assert.Equal(6, progress[^1]);
    }

    [Fact]
    public async Task HumanProgressGoesToStderrAndTheSummaryToStdout()
    {
        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync(
            "build",
            CliTestProject.SamplePath,
            "--preview");

        Assert.Equal(0, exitCode);
        Assert.Contains("Mode: preview", stdout, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
        Assert.Contains("[1/8]", stderr, StringComparison.Ordinal);
        Assert.Contains("[8/8]", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("[1/8]", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuietHumanKeepsDiagnosticsAndSuppressesStages()
    {
        var (exitCode, stdout, stderr) = await CliTestRunner.RunAsync(
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--quiet");

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("[1/8]", stderr, StringComparison.Ordinal);
        Assert.Contains("ENM017", stderr, StringComparison.Ordinal);
        Assert.Contains("Result: ok (exit 0)", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task JsonModeWritesNoHumanProgress()
    {
        var (_, _, stderr) = await CliTestRunner.RunAsync(
            "build",
            CliTestProject.SamplePath,
            "--preview",
            "--json");

        Assert.DoesNotContain("[1/8]", stderr, StringComparison.Ordinal);
        Assert.All(
            stderr.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            static line =>
            {
                var element = JsonElement.Parse(line);
                Assert.True(element.TryGetProperty("event", out _));
            });
    }
}
