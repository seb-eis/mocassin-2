// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for the three parameterization rebase strategies, the rebase report, and silent-reuse
/// prevention over real sample projects and the real compiler.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationRebaseTests
{
    [Fact]
    public async Task KeepMatchingOnTheSameModelRoundTripsTheAppliedSignature()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var summary = ParameterizationFactory.Describe(project, model, template);
        var service = new ParameterizationService();

        var rebase = service.Rebase(project, model, template, ParameterizationRebaseOptions.Default);

        Assert.Equal(template.Id, rebase.Template.Id);
        Assert.Equal(template.Name, rebase.Template.Name);
        Assert.Equal(template.CreatedUtc, rebase.Template.CreatedUtc);
        Assert.Equal(model.ModelSignature, rebase.Template.ModelSignature);
        var report = rebase.Report;
        Assert.Equal(ParameterizationRebaseStrategy.KeepMatching, report.Strategy);
        Assert.Equal(ExplicitCount(template), report.CopiedValues);
        Assert.Equal(summary.UnsetValueCount, report.DefaultedValues);
        Assert.Equal(0, report.DroppedValues);
        Assert.Equal(0, report.AmbiguousMatches);
        Assert.Equal(0, report.DescriptorMatches);
        Assert.Empty(report.DescriptorMatchDetails);

        var diagnostic = Assert.Single(rebase.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.RebaseReport, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains($"{report.CopiedValues} copied", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains($"{report.DefaultedValues} defaulted", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains($"{report.DroppedValues} dropped", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains($"{report.AmbiguousMatches} ambiguous", diagnostic.Message, StringComparison.Ordinal);

        var original = service.Apply(project, model, template);
        var rebound = service.Apply(project, model, rebase.Template);
        Assert.True(original.IsSuccess);
        Assert.True(rebound.IsSuccess);
        Assert.Equal(original.Model!.Signature, rebound.Model!.Signature);
        var reboundDiff = service.Diff(project, model, rebase.Template);
        Assert.Empty(reboundDiff.Removed);
        Assert.Equal(report.CopiedValues, reboundDiff.Matched.Length);
    }

    [Fact]
    public async Task KeepMatchingOnAChangedModelDropsRemovedValuesAndDefaultsNewSlots()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        Assert.NotEqual(model.ModelSignature, changedModel.ModelSignature);
        var service = new ParameterizationService();
        var diff = service.Diff(changedProject, changedModel, template);

        var rebase = service.Rebase(changedProject, changedModel, template, ParameterizationRebaseOptions.Default);

        var report = rebase.Report;
        Assert.Equal(diff.Matched.Length, report.CopiedValues);
        Assert.Equal(diff.Added.Length, report.DefaultedValues);
        Assert.Equal(diff.Removed.Length, report.DroppedValues);
        Assert.Equal(0, report.DescriptorMatches);
        Assert.Empty(report.DescriptorMatchDetails);

        var matchedKeys = diff.Matched.Select(static entry => entry.Key).ToHashSet();
        var removedSetIds = diff.Removed
            .Where(static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair)
            .Select(static entry => entry.Key.SetId!)
            .ToHashSet(StringComparer.Ordinal);
        var removedGroupIds = diff.Removed
            .Where(static entry => entry.Key.Kind == ParameterizationValueKind.Group)
            .Select(static entry => entry.Key.SetId!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(
            diff.Matched,
            static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair);
        Assert.DoesNotContain(
            rebase.Template.StablePairSets.AddRange(rebase.Template.UnstablePairSets),
            set => removedSetIds.Contains(set.EnergyId));
        Assert.DoesNotContain(rebase.Template.GroupSets, set => removedGroupIds.Contains(set.ClusterEnergyId));

        var reboundDiff = service.Diff(changedProject, changedModel, rebase.Template);
        Assert.Empty(reboundDiff.Removed);
        Assert.Equal(report.CopiedValues, reboundDiff.Matched.Length);
        Assert.Equal(report.DefaultedValues, reboundDiff.Added.Length);
        Assert.All(reboundDiff.Matched, entry => Assert.Contains(entry.Key, matchedKeys));
        foreach (var entry in reboundDiff.Matched)
        {
            var source = Assert.Single(diff.Matched, candidate => candidate.Key == entry.Key);
            Assert.Equal(source.TemplateValue, entry.TemplateValue);
        }

        var applied = service.Apply(changedProject, changedModel, rebase.Template);
        Assert.True(applied.IsSuccess);
        Assert.All(
            applied.Model!.Values.PairTables.SelectMany(static table => table),
            static value => Assert.Equal(0.0, value));
        Assert.Contains(0.48, applied.Model.Values.GroupSets.SelectMany(static table => table));
    }

    [Fact]
    public async Task NearestGeometryCopiesUniqueAchiralPairValuesAndReportsEachMatch()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Vehicle5);
        var template = ParameterizationTestSupport.WithAllExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, model),
            model);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        var service = new ParameterizationService();
        var diff = service.Diff(changedProject, changedModel, template);
        var chiralSetIds = template.StablePairSets
            .AddRange(template.UnstablePairSets)
            .Where(static set => set.IsChiral)
            .Select(static set => set.EnergyId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(chiralSetIds);

        var rebase = service.Rebase(
            changedProject,
            changedModel,
            template,
            new ParameterizationRebaseOptions
            {
                Strategy = ParameterizationRebaseStrategy.NearestGeometry,
                NearestDistanceToleranceFm = 5000,
            });

        var report = rebase.Report;
        Assert.True(report.DescriptorMatches > 0);
        Assert.Equal(report.DescriptorMatches, report.DescriptorMatchDetails.Length);
        Assert.Equal(0, report.AmbiguousMatches);
        Assert.Equal(diff.Matched.Length + report.DescriptorMatches, report.CopiedValues);
        Assert.Equal(diff.Added.Length - report.DescriptorMatches, report.DefaultedValues);
        Assert.Equal(diff.Removed.Length - report.DescriptorMatches, report.DroppedValues);
        Assert.All(
            report.DescriptorMatchDetails,
            detail => Assert.Equal(0.11, detail.Value, 12));
        Assert.All(
            report.DescriptorMatchDetails,
            detail => Assert.NotEqual(detail.SourceDistanceBucketFm, detail.TargetDistanceBucketFm));
        Assert.All(
            report.DescriptorMatchDetails,
            detail => Assert.Contains(detail.RemovedKey, diff.Removed.Select(static entry => entry.Key)));
        Assert.All(
            report.DescriptorMatchDetails,
            detail => Assert.Contains(detail.AddedKey, diff.Added.Select(static entry => entry.Key)));

        var reboundDiff = service.Diff(changedProject, changedModel, rebase.Template);
        Assert.Empty(reboundDiff.Removed);
        var allowedKeys = diff.Matched
            .Select(static entry => entry.Key)
            .Concat(report.DescriptorMatchDetails.Select(static detail => detail.AddedKey))
            .ToHashSet();
        Assert.All(reboundDiff.Matched, entry => Assert.Contains(entry.Key, allowedKeys));
        Assert.All(
            rebase.Template.StablePairSets.AddRange(rebase.Template.UnstablePairSets).Where(static set => set.IsChiral),
            static set => Assert.Empty(set.Entries));
        Assert.DoesNotContain(
            report.DescriptorMatchDetails,
            detail => chiralSetIds.Contains(detail.RemovedKey.SetId!));
        Assert.Equal(report.DescriptorMatches, report.DescriptorMatchDetails.Length);
    }

    [Fact]
    public async Task NearestGeometryRejectsAmbiguousPairMatchesWithoutCopying()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        var service = new ParameterizationService();
        var diff = service.Diff(changedProject, changedModel, template);
        var removedPairCount = diff.Removed.Count(
            static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair);
        Assert.True(removedPairCount > 0);

        var rebase = service.Rebase(
            changedProject,
            changedModel,
            template,
            new ParameterizationRebaseOptions
            {
                Strategy = ParameterizationRebaseStrategy.NearestGeometry,
                NearestDistanceToleranceFm = 100000,
            });

        var report = rebase.Report;
        Assert.Equal(0, report.DescriptorMatches);
        Assert.Empty(report.DescriptorMatchDetails);
        Assert.Equal(removedPairCount, report.AmbiguousMatches);
        Assert.Equal(diff.Matched.Length, report.CopiedValues);
        Assert.Equal(diff.Added.Length, report.DefaultedValues);
        Assert.Equal(diff.Removed.Length, report.DroppedValues);
        var removedPairIds = diff.Removed
            .Where(static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair)
            .Select(static entry => entry.Key.SetId!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain(
            rebase.Template.StablePairSets.AddRange(rebase.Template.UnstablePairSets),
            set => removedPairIds.Contains(set.EnergyId));
    }

    [Fact]
    public async Task NearestGeometryNeverCopiesChiralValuesAcrossAChangedModel()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.BariumZirconate);
        var template = ParameterizationTestSupport.WithAllExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, model),
            model);
        var chiralSets = template.StablePairSets
            .AddRange(template.UnstablePairSets)
            .Where(static set => set.IsChiral)
            .ToArray();
        Assert.Equal(24, model.Energy.PairGeometries.Count(static geometry => geometry.IsChiral));
        Assert.NotEmpty(chiralSets);
        Assert.All(chiralSets, static set => Assert.NotEmpty(set.Entries));
        Assert.Equal(chiralSets.Length, chiralSets.Select(static set => set.EnergyId).Distinct(StringComparer.Ordinal).Count());

        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        var service = new ParameterizationService();
        var diff = service.Diff(changedProject, changedModel, template);
        Assert.True(
            diff.Removed.Any(static entry => entry.Key.Kind is ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair));

        var rebase = service.Rebase(
            changedProject,
            changedModel,
            template,
            new ParameterizationRebaseOptions
            {
                Strategy = ParameterizationRebaseStrategy.NearestGeometry,
                NearestDistanceToleranceFm = 100000,
            });

        Assert.Equal(0, rebase.Report.DescriptorMatches);
        Assert.Empty(rebase.Report.DescriptorMatchDetails);
        Assert.True(rebase.Report.AmbiguousMatches > 0);
        Assert.All(
            rebase.Template.StablePairSets.AddRange(rebase.Template.UnstablePairSets).Where(static set => set.IsChiral),
            static set => Assert.Empty(set.Entries));
        var chiralIds = chiralSets.Select(static set => set.EnergyId).ToHashSet(StringComparer.Ordinal);
        var reboundDiff = service.Diff(changedProject, changedModel, rebase.Template);
        Assert.DoesNotContain(reboundDiff.Matched, entry => chiralIds.Contains(entry.Key.SetId!));
    }

    [Fact]
    public async Task ResetKeepsIdentityAndHeadersButClearsEveryValue()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        var service = new ParameterizationService();

        var rebase = service.Rebase(
            changedProject,
            changedModel,
            template,
            new ParameterizationRebaseOptions { Strategy = ParameterizationRebaseStrategy.Reset });

        Assert.Equal(template.Id, rebase.Template.Id);
        Assert.Equal(template.Name, rebase.Template.Name);
        Assert.Equal(template.CreatedUtc, rebase.Template.CreatedUtc);
        Assert.Equal(changedModel.ModelSignature, rebase.Template.ModelSignature);
        Assert.All(rebase.Template.StablePairSets, static set => Assert.Empty(set.Entries));
        Assert.All(rebase.Template.UnstablePairSets, static set => Assert.Empty(set.Entries));
        Assert.All(rebase.Template.GroupSets, static set => Assert.Empty(set.Entries));
        Assert.All(rebase.Template.TransitionRuleSets, static set => Assert.Empty(set.Rules));
        Assert.Empty(rebase.Template.SiteEnergyOverrides);
        Assert.Equal(
            changedModel.Energy.PairTableCount,
            rebase.Template.StablePairSets.Length + rebase.Template.UnstablePairSets.Length);
        Assert.Equal(changedModel.Energy.ClusterTableCount, rebase.Template.GroupSets.Length);
        Assert.Equal(
            changedModel.Transitions.Collections.Count(
                static collection => collection.Transition.Kind == Mocassin.Core.Model.Transitions.TransitionKind.Kinetic),
            rebase.Template.TransitionRuleSets.Length);
        var report = rebase.Report;
        Assert.Equal(0, report.CopiedValues);
        var changedSummary = ParameterizationFactory.Describe(changedProject, changedModel, rebase.Template);
        Assert.Equal(changedSummary.RequiredSlotCount, report.DefaultedValues);
        Assert.Equal(ExplicitCount(template), report.DroppedValues);
        Assert.Equal(0, report.AmbiguousMatches);
        Assert.Equal(0, report.DescriptorMatches);
        Assert.Empty(report.DescriptorMatchDetails);

        var reset = service.Apply(changedProject, changedModel, rebase.Template);
        var implicitDefault = service.Apply(changedProject, changedModel);
        Assert.True(reset.IsSuccess);
        Assert.True(implicitDefault.IsSuccess);
        Assert.Equal(implicitDefault.Model!.Signature, reset.Model!.Signature);
    }

    [Fact]
    public async Task RebaseRejectsTheImplicitDefaultAndInvalidOptions()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var service = new ParameterizationService();
        var implicitDefault = ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature);

        Assert.Throws<ArgumentException>(
            () => service.Rebase(project, model, implicitDefault, ParameterizationRebaseOptions.Default));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Rebase(
                project,
                model,
                ParameterizationTestSupport.CreateTemplate(project, model),
                new ParameterizationRebaseOptions { NearestDistanceToleranceFm = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Rebase(
                project,
                model,
                ParameterizationTestSupport.CreateTemplate(project, model),
                new ParameterizationRebaseOptions { Strategy = (ParameterizationRebaseStrategy)99 }));
    }

    [Theory]
    [InlineData(ParameterizationRebaseStrategy.KeepMatching)]
    [InlineData(ParameterizationRebaseStrategy.NearestGeometry)]
    [InlineData(ParameterizationRebaseStrategy.Reset)]
    public async Task EveryStrategyEmitsThePrm009InfoReport(ParameterizationRebaseStrategy strategy)
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);

        var rebase = new ParameterizationService().Rebase(
            project,
            model,
            template,
            new ParameterizationRebaseOptions { Strategy = strategy });

        var diagnostic = Assert.Single(rebase.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.RebaseReport, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.NotNull(diagnostic.Path);
        Assert.Equal(strategy, rebase.Report.Strategy);
    }

    private static int ExplicitCount(ParameterizationTemplate template) =>
        template.StablePairSets.Sum(static set => set.Entries.Length) +
        template.UnstablePairSets.Sum(static set => set.Entries.Length) +
        template.GroupSets.Sum(static set => set.Entries.Length) +
        template.TransitionRuleSets.Sum(static set => set.Rules.Length) +
        template.SiteEnergyOverrides.Length;
}
