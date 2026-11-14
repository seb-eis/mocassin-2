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

    [Fact]
    public async Task CeriaApplyAndSignatureStayWithinTheLocalBudget()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var service = new ParameterizationService();
        for (var index = 0; index < 3; index++)
        {
            _ = service.Apply(project, model, template);
        }

        const int iterations = 10;
        var applied = 0;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
        {
            applied += service.Apply(project, model, template).Model!.Values.SiteEnergies.Length;
        }

        var applyElapsed = Stopwatch.GetElapsedTime(started);
        var applyAverage = applyElapsed / iterations;
        output.WriteLine(
            $"Ceria apply+sign average: {applyAverage.TotalMilliseconds:F3} ms; site slots per apply: {applied / iterations}.");
        Assert.True(applied > 0);
        Assert.True(
            applyAverage < TimeSpan.FromMilliseconds(50),
            $"Ceria apply+sign averaged {applyAverage.TotalMilliseconds:F3} ms; the target is below 50 ms.");
    }

    [Fact]
    public async Task CeriaKmcApplyAndSignatureStayWithinTheLocalBudget()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var service = new ParameterizationService();
        for (var index = 0; index < 3; index++)
        {
            _ = service.Apply(project, model);
        }

        const int iterations = 10;
        var rules = 0;
        var started = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
        {
            rules += service.Apply(project, model).Model!.Values.TransitionRuleFrequencies.Length;
        }

        var applyElapsed = Stopwatch.GetElapsedTime(started);
        var applyAverage = applyElapsed / iterations;
        output.WriteLine(
            $"Ceria KMC apply+sign average: {applyAverage.TotalMilliseconds:F3} ms; rules per apply: {rules / iterations}.");
        Assert.True(rules > 0);
        Assert.True(
            applyAverage < TimeSpan.FromMilliseconds(50),
            $"Ceria KMC apply+sign averaged {applyAverage.TotalMilliseconds:F3} ms; the target is below 50 ms.");
    }
}
