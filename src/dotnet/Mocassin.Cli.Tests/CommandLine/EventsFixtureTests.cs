// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Cli.Tests.CommandLine;

[Trait("Tier", "PR-fast")]
public sealed class EventsFixtureTests
{
    [Fact]
    public void BuildSequenceSatisfiesTheEventsContract()
    {
        using var schema = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("contracts", "generated", "schema", "events-v1.schema.json")));
        var eventNames = schema.RootElement.GetProperty("properties")
            .GetProperty("event")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(static item => item.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var requiredFields = schema.RootElement.GetProperty("x-event-required-fields");
        var lines = File.ReadAllLines(RepositoryPaths.Combine("tests", "contracts", "events-v1.jsonl"))
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        Assert.NotEmpty(lines);
        var events = new List<string>();
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
            var name = root.GetProperty("event").GetString()!;
            Assert.Contains(name, eventNames);
            foreach (var required in requiredFields.GetProperty(name).EnumerateArray())
            {
                Assert.True(
                    root.TryGetProperty(required.GetString()!, out _),
                    $"Event '{name}' is missing required field '{required.GetString()}'.");
            }

            events.Add(name);
        }

        Assert.Equal(1, events.Count(static name => name == "build.finished"));
        Assert.True(
            events.FindIndex(static name => name == "build.stage")
            < events.FindIndex(static name => name == "build.started"),
            "The fixture must show build.stage events before build.started.");
    }

    [Fact]
    public void BuildStageIsRegisteredAsAnAdditiveEvent()
    {
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine("contracts", "manifest.json")));
        var events = manifest.RootElement
            .GetProperty("formats")
            .GetProperty("events")
            .GetProperty("events")
            .EnumerateArray()
            .ToArray();
        var stage = events.Single(static entry => entry.GetProperty("name").GetString() == "build.stage");

        Assert.Equal(
            ["stage", "status"],
            stage.GetProperty("requiredFields").EnumerateArray().Select(static item => item.GetString()));
        Assert.Equal(
            1,
            manifest.RootElement.GetProperty("formats").GetProperty("events")
                .GetProperty("schemaVersion").GetInt32());
    }
}
