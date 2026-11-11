// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.Packages.Tests.Projects.Validation;

[Trait("Tier", "PR-medium")]
public sealed class ValidationPerformanceTests
{
    [Fact]
    public void CeriaValidationMeetsTheDocumentedTargets()
    {
        var project = ProjectJson.Deserialize(
            File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", "project-ceria.mocprj")));
        var pipeline = new ProjectValidationPipeline();
        _ = pipeline.Validate(project);
        _ = pipeline.Validate(project, options: new ValidationOptions { IncludeStatistics = true });

        var plain = Measure(() => pipeline.Validate(project));
        var stats = Measure(() => pipeline.Validate(
            project,
            options: new ValidationOptions { IncludeStatistics = true }));

        Assert.True(plain < 200, $"Ceria validation took {plain:0.0} ms; target is < 200 ms.");
        Assert.True(stats < 2000, $"Ceria statistics validation took {stats:0.0} ms; target is < 2 s.");
    }

    private static double Measure(Action action)
    {
        var best = double.MaxValue;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var started = Stopwatch.GetTimestamp();
            action();
            best = Math.Min(best, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        return best;
    }
}
