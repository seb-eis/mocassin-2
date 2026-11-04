// SPDX-License-Identifier: MIT

using System.Text.Json.Nodes;
using Mocassin.Core.Project;

namespace Mocassin.Core.Tests;

public sealed class ProjectMigrationTests
{
    [Fact]
    public void MigrationWritesResultingSchemaVersion()
    {
        var document = JsonNode.Parse("""{"schemaVersion":0,"value":0}""")!.AsObject();

        var result = ProjectMigrations.Apply(
            document,
            0,
            [new VersionZeroToOneMigration()]);

        Assert.Equal(1, result.Version);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(1, result.Document["schemaVersion"]!.GetValue<int>());
        Assert.Equal(1, result.Document["value"]!.GetValue<int>());
    }

    private sealed class VersionZeroToOneMigration : IProjectMigration
    {
        public int FromVersion => 0;

        public int ToVersion => 1;

        public JsonNode Apply(JsonNode document)
        {
            document["value"] = 1;
            return document;
        }
    }
}
