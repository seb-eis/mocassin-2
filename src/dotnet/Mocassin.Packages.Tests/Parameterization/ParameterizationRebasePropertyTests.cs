// SPDX-License-Identifier: MIT

using Mocassin.Core.Model;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Deterministic property tests for diff/rebase round-trips and silent-reuse prevention.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ParameterizationRebasePropertyTests
{
    [Theory]
    [InlineData(ParameterizationTestSupport.Ceria, 0)]
    [InlineData(ParameterizationTestSupport.Ceria, 1)]
    [InlineData(ParameterizationTestSupport.Ceria, 2)]
    [InlineData(ParameterizationTestSupport.CeriaKmc, 0)]
    [InlineData(ParameterizationTestSupport.CeriaKmc, 1)]
    [InlineData(ParameterizationTestSupport.CeriaKmc, 2)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, 0)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, 1)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, 2)]
    public async Task KeepMatchingToTheSameModelRoundTripsTheSignature(string sample, int seed)
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(sample);
        var template = ParameterizationTestSupport.Shuffle(
            ParameterizationTestSupport.WithSparseExplicitValues(
                ParameterizationTestSupport.CreateTemplate(project, model),
                model,
                seed),
            seed);
        var summary = ParameterizationFactory.Describe(project, model, template);
        var explicitCount = ExplicitCount(template);
        var service = new ParameterizationService();

        var rebase = service.Rebase(project, model, template, ParameterizationRebaseOptions.Default);

        Assert.Equal(explicitCount, rebase.Report.CopiedValues);
        Assert.Equal(summary.RequiredSlotCount - explicitCount, rebase.Report.DefaultedValues);
        Assert.Equal(0, rebase.Report.DroppedValues);
        Assert.Equal(0, rebase.Report.AmbiguousMatches);
        Assert.Equal(0, rebase.Report.DescriptorMatches);
        var original = service.Apply(project, model, template);
        var rebound = service.Apply(project, model, rebase.Template);
        Assert.True(original.IsSuccess);
        Assert.True(rebound.IsSuccess);
        Assert.Equal(original.Model!.Signature, rebound.Model!.Signature);
        var reboundDiff = service.Diff(project, model, rebase.Template);
        Assert.Empty(reboundDiff.Removed);
        Assert.Equal(explicitCount, reboundDiff.Matched.Length);
    }

    [Theory]
    [InlineData(ParameterizationTestSupport.Ceria, ParameterizationRebaseStrategy.KeepMatching)]
    [InlineData(ParameterizationTestSupport.Ceria, ParameterizationRebaseStrategy.NearestGeometry)]
    [InlineData(ParameterizationTestSupport.Ceria, ParameterizationRebaseStrategy.Reset)]
    [InlineData(ParameterizationTestSupport.Vehicle5, ParameterizationRebaseStrategy.KeepMatching)]
    [InlineData(ParameterizationTestSupport.Vehicle5, ParameterizationRebaseStrategy.NearestGeometry)]
    [InlineData(ParameterizationTestSupport.Vehicle5, ParameterizationRebaseStrategy.Reset)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, ParameterizationRebaseStrategy.KeepMatching)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, ParameterizationRebaseStrategy.NearestGeometry)]
    [InlineData(ParameterizationTestSupport.BariumZirconate, ParameterizationRebaseStrategy.Reset)]
    public async Task EveryStrategyCopiesOnlyExactOrReportedDescriptors(
        string sample,
        ParameterizationRebaseStrategy strategy)
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(sample);
        var template = ParameterizationTestSupport.WithSparseExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, model),
            model,
            seed: 7);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        var service = new ParameterizationService();
        var diff = service.Diff(changedProject, changedModel, template);

        var rebase = service.Rebase(
            changedProject,
            changedModel,
            template,
            new ParameterizationRebaseOptions
            {
                Strategy = strategy,
                NearestDistanceToleranceFm = 5000,
            });

        var report = rebase.Report;
        var reboundDiff = service.Diff(changedProject, changedModel, rebase.Template);
        Assert.Empty(reboundDiff.Removed);
        Assert.Equal(report.DescriptorMatches, report.DescriptorMatchDetails.Length);
        Assert.Equal(report.CopiedValues, reboundDiff.Matched.Length);
        Assert.Equal(report.DefaultedValues, reboundDiff.Added.Length);

        if (strategy == ParameterizationRebaseStrategy.Reset)
        {
            var changedSummary = ParameterizationFactory.Describe(changedProject, changedModel, rebase.Template);
            Assert.Equal(0, report.CopiedValues);
            Assert.Equal(changedSummary.RequiredSlotCount, report.DefaultedValues);
            Assert.Equal(ExplicitCount(template), report.DroppedValues);
            Assert.Equal(0, report.DescriptorMatches);
            Assert.Empty(report.DescriptorMatchDetails);
            return;
        }

        Assert.Equal(diff.Matched.Length + report.DescriptorMatches, report.CopiedValues);
        Assert.Equal(diff.Added.Length - report.DescriptorMatches, report.DefaultedValues);
        Assert.Equal(diff.Removed.Length - report.DescriptorMatches, report.DroppedValues);

        var allowedValues = diff.Matched.ToDictionary(
            static entry => entry.Key,
            static entry => entry.TemplateValue!.Value);
        foreach (var detail in report.DescriptorMatchDetails)
        {
            Assert.Contains(detail.RemovedKey, diff.Removed.Select(static entry => entry.Key));
            Assert.Contains(detail.AddedKey, diff.Added.Select(static entry => entry.Key));
            allowedValues.Add(detail.AddedKey, detail.Value);
        }

        foreach (var entry in reboundDiff.Matched)
        {
            Assert.True(
                allowedValues.TryGetValue(entry.Key, out var expected),
                $"Rebased key {entry.Key} was not copied from an exact match or a reported descriptor match.");
            Assert.Equal(expected, entry.TemplateValue!.Value, 12);
        }

        var removedKeys = diff.Removed.Select(static entry => entry.Key).ToHashSet();
        Assert.DoesNotContain(reboundDiff.Matched, entry => removedKeys.Contains(entry.Key));
    }

    private static int ExplicitCount(ParameterizationTemplate template) =>
        template.StablePairSets.Sum(static set => set.Entries.Length) +
        template.UnstablePairSets.Sum(static set => set.Entries.Length) +
        template.GroupSets.Sum(static set => set.Entries.Length) +
        template.TransitionRuleSets.Sum(static set => set.Rules.Length) +
        template.SiteEnergyOverrides.Length;
}
