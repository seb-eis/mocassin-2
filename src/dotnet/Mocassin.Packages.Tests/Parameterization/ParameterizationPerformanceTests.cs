// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Mocassin.Packages.Parameterization;
using Xunit.Abstractions;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Guards the Step 09 template-creation performance target.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CeriaCreationAndSummaryStayWithinTheLocalBudget()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch));
        var options = new ParameterizationTemplateOptions { Name = "Ceria parameters" };
        for (var index = 0; index < 3; index++)
        {
            _ = factory.Create(project, model, options);
        }

        const int iterations = 10;
        var created = 0;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
        {
            var result = factory.Create(project, model, options);
            created += result.Summary.RequiredSlotCount;
        }

        var creationElapsed = Stopwatch.GetElapsedTime(started);
        started = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
        {
            _ = ParameterizationFactory.Describe(project, model);
        }

        var describeElapsed = Stopwatch.GetElapsedTime(started);
        var creationAverage = creationElapsed / iterations;
        var describeAverage = describeElapsed / iterations;
        output.WriteLine(
            $"Ceria create+summary average: {creationAverage.TotalMilliseconds:F3} ms; " +
            $"describe average: {describeAverage.TotalMilliseconds:F3} ms; " +
            $"required slots: {created / iterations}.");
        Assert.True(created > 0);
        Assert.True(
            creationAverage < TimeSpan.FromMilliseconds(20),
            $"Ceria create+summary averaged {creationAverage.TotalMilliseconds:F3} ms; the target is below 20 ms.");
        Assert.True(
            describeAverage < TimeSpan.FromMilliseconds(20),
            $"Ceria describe averaged {describeAverage.TotalMilliseconds:F3} ms; the target is below 20 ms.");
    }
}
