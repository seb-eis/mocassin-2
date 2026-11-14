// SPDX-License-Identifier: MIT

using Mocassin.Core.Model;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for the explicit <see cref="ParameterizationService.Diff"/> surface over real sample
/// projects and the real compiler.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationDiffTests
{
    [Fact]
    public async Task ImplicitDefaultListsEveryRequiredSlotAsAdded()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var summary = ParameterizationFactory.Describe(project, model);

        var diff = new ParameterizationService().Diff(project, model);

        Assert.Equal(model.ModelSignature, diff.TemplateModelSignature);
        Assert.Equal(model.ModelSignature, diff.ModelSignature);
        Assert.Equal(summary.RequiredSlotCount, diff.Added.Length);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Matched);
        Assert.Empty(diff.GeometryChanges);
        Assert.All(diff.Added, static entry => Assert.Null(entry.TemplateValue));
        Assert.All(diff.Added, static entry => Assert.False(string.IsNullOrEmpty(entry.Descriptor)));
        Assert.Equal(
            diff.Added.Length,
            diff.Added.Select(static entry => entry.Key).Distinct().Count());
        Assert.All(
            diff.Added,
            static entry => Assert.Contains(
                entry.Key.Kind,
                new[]
                {
                    ParameterizationValueKind.StablePair,
                    ParameterizationValueKind.UnstablePair,
                    ParameterizationValueKind.Group,
                    ParameterizationValueKind.RuleFrequency,
                }));
    }

    [Fact]
    public async Task CurrentTemplateReportsEveryExplicitEntryAsMatched()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var summary = ParameterizationFactory.Describe(project, model, template);

        var diff = new ParameterizationService().Diff(project, model, template);

        Assert.Empty(diff.Removed);
        Assert.Empty(diff.GeometryChanges);
        Assert.Equal(ExplicitCount(template), diff.Matched.Length);
        Assert.Equal(summary.UnsetValueCount, diff.Added.Length);
        Assert.All(diff.Matched, static entry => Assert.NotNull(entry.TemplateValue));
        Assert.Contains(
            diff.Matched,
            static entry => entry.Key.Kind == ParameterizationValueKind.SiteEnergyOverride);
        Assert.DoesNotContain(
            diff.Added,
            static entry => entry.Key.Kind == ParameterizationValueKind.SiteEnergyOverride);
    }

    [Fact]
    public async Task ChangedModelReportsEveryCategoryDeterministically()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var changedProject = ParameterizationTestSupport.ChangeCellParameters(project, 0.05);
        var changedModel = ParameterizationTestSupport.Compile(changedProject);
        Assert.NotEqual(model.ModelSignature, changedModel.ModelSignature);
        var service = new ParameterizationService();

        var first = service.Diff(changedProject, changedModel, template);
        var second = service.Diff(changedProject, changedModel, template);

        Assert.NotEmpty(first.Added);
        Assert.NotEmpty(first.Removed);
        Assert.NotEmpty(first.Matched);
        Assert.NotEmpty(first.GeometryChanges);
        Assert.All(first.Added, static entry => Assert.Null(entry.TemplateValue));
        Assert.All(first.Removed, static entry => Assert.NotNull(entry.TemplateValue));
        Assert.Equal(first.Added.Select(static entry => entry.Key), second.Added.Select(static entry => entry.Key));
        Assert.Equal(first.Removed.Select(static entry => entry.Key), second.Removed.Select(static entry => entry.Key));
        Assert.Equal(first.Matched.Select(static entry => entry.Key), second.Matched.Select(static entry => entry.Key));
        Assert.Equal(
            first.GeometryChanges.Select(static change => (change.RemovedKey, change.AddedKey)),
            second.GeometryChanges.Select(static change => (change.RemovedKey, change.AddedKey)));
        Assert.All(
            first.GeometryChanges,
            static change => Assert.Contains(
                change.Kind,
                new[] { ParameterizationValueKind.StablePair, ParameterizationValueKind.UnstablePair, ParameterizationValueKind.Group }));
    }

    [Fact]
    public async Task DiffIsIndependentOfTemplateEntryOrder()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var shuffled = ParameterizationTestSupport.Shuffle(template, 17);
        var service = new ParameterizationService();

        var original = service.Diff(project, model, template);
        var reordered = service.Diff(project, model, shuffled);

        Assert.Equal(
            original.Matched.Select(static entry => entry.Key),
            reordered.Matched.Select(static entry => entry.Key));
        Assert.Equal(original.Added.Length, reordered.Added.Length);
        Assert.Empty(reordered.Removed);
    }

    [Fact]
    public async Task KineticRuleTemplateListsEveryRuleAsMatched()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var template = ParameterizationTestSupport.WithAllExplicitValues(
            ParameterizationTestSupport.CreateTemplate(project, model),
            model);

        var diff = new ParameterizationService().Diff(project, model, template);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Equal(template.TransitionRuleSets.Sum(static set => set.Rules.Length), diff.Matched.Length);
        Assert.All(
            diff.Matched,
            static entry => Assert.Equal(ParameterizationValueKind.RuleFrequency, entry.Key.Kind));
    }

    private static int ExplicitCount(ParameterizationTemplate template) =>
        template.StablePairSets.Sum(static set => set.Entries.Length) +
        template.UnstablePairSets.Sum(static set => set.Entries.Length) +
        template.GroupSets.Sum(static set => set.Entries.Length) +
        template.TransitionRuleSets.Sum(static set => set.Rules.Length) +
        template.SiteEnergyOverrides.Length;
}
