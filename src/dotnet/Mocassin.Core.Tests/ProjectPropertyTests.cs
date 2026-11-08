// SPDX-License-Identifier: MIT

using System.Text.Json;
using FsCheck.Xunit;
using Mocassin.Core.Ids;
using Mocassin.Core.Project;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class ProjectPropertyTests
{
    [Property(MaxTest = 200)]
    public bool ParserOnlyThrowsJsonExceptionForArbitraryBytes(byte[]? bytes)
    {
        if (bytes is null)
        {
            return true;
        }

        try
        {
            ProjectJson.Deserialize(bytes);
        }
        catch (JsonException)
        {
            // Arbitrary bytes are expected to be rejected as malformed JSON.
        }

        return true;
    }

    [Property(MaxTest = 200)]
    public bool LengthValidationMatchesDocumentedRule(double value)
    {
        var expected = double.IsFinite(value) && value >= 0.0;
        var actual = LengthAngstrom.TryCreate(value, out _);
        return actual == expected;
    }

    [Property(MaxTest = 200)]
    public bool ParticleIdJsonRoundTrips(Guid value)
    {
        var id = ParticleId.Create(value);
        var json = JsonSerializer.Serialize(id);
        var loaded = JsonSerializer.Deserialize<ParticleId>(json);
        return loaded == id;
    }

    [Property(MaxTest = 200)]
    public bool CanonicalDocumentIsStableForRandomProjectNames(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var project = TestProject(name);
        var first = ProjectCanonicalizer.ToCanonicalJson(project);
        var loaded = ProjectJson.Deserialize(ProjectCanonicalizer.ToCanonicalUtf8(project));
        var second = ProjectCanonicalizer.ToCanonicalJson(loaded);
        return string.Equals(first, second, StringComparison.Ordinal);
    }

    private static Mocassin.Core.Project.Project TestProject(string name)
    {
        var time = DateTimeOffset.Parse("2026-11-04T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        return new Mocassin.Core.Project.Project(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
                new string('a', 64),
                name,
                null,
                time,
                time),
            new Mocassin.Core.Model.ModelDefinition(),
            [],
            [],
            [],
            Mocassin.Core.Model.ViewSettings.Default);
    }
}
