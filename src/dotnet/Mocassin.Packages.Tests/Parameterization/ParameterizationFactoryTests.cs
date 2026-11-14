// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for <see cref="ParameterizationFactory.Create"/> and <see cref="ParameterizationFactory.Describe"/>.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationFactoryTests
{
    private static readonly DateTimeOffset created = DateTimeOffset.Parse(
        "2026-11-14T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public async Task CeriaCreatesOneHeaderPerCompiledTableAndNoValues()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));

        var result = factory.Create(project, model, new ParameterizationTemplateOptions { Name = "Ceria parameters" });

        Assert.Empty(result.Diagnostics);
        Assert.Equal(model.ModelSignature, result.Template.ModelSignature);
        Assert.Equal("Ceria parameters", result.Template.Name);
        Assert.Equal(created, result.Template.CreatedUtc);
        Assert.Equal(7, result.Template.Id.Value.Version);
        Assert.Empty(result.Template.SiteEnergyOverrides);

        var sets = result.Template.StablePairSets.AddRange(result.Template.UnstablePairSets);
        Assert.Equal(model.Energy.PairTableCount, sets.Length);
        Assert.Equal(
            model.Energy.PairTables.Select(static table => table.EnergyId).OrderBy(static id => id, StringComparer.Ordinal),
            sets.Select(static set => set.EnergyId).OrderBy(static id => id, StringComparer.Ordinal));
        Assert.All(sets, set => Assert.Empty(set.Entries));

        foreach (var table in model.Energy.PairTables)
        {
            var set = sets.Single(candidate => candidate.EnergyId == table.EnergyId);
            var geometry = ParameterizationTestSupport.Representative(model, table);
            Assert.Equal(geometry.CenterSiteId, set.CenterSiteId);
            Assert.Equal(geometry.PartnerSiteId, set.PartnerSiteId);
            Assert.Equal(geometry.DistanceBucketFm, set.DistanceBucketFm);
            Assert.Equal(geometry.IsChiral, set.IsChiral);
            var expectedStable = model.Structure.Sites[geometry.CenterSiteIndex].Stability ==
                                 Mocassin.Core.Model.Structure.SiteStability.Stable;
            Assert.Equal(expectedStable, result.Template.StablePairSets.Contains(set));
        }

        Assert.Equal(model.Energy.ClusterTableCount, result.Template.GroupSets.Length);
        Assert.All(result.Template.GroupSets, set => Assert.Empty(set.Entries));
        Assert.Empty(result.Template.TransitionRuleSets);
        Assert.Equal(ParameterizationFactory.Describe(project, model), result.Summary);
        Assert.False(result.Summary.HasExplicitValues);
    }

    [Fact]
    public async Task CeriaKmcCreatesKineticRuleHeadersOnly()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.CeriaKmc);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));

        var result = factory.Create(project, model, ParameterizationTemplateOptions.Default);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(model.Transitions.Collections.Length, result.Template.TransitionRuleSets.Length);
        foreach (var collection in model.Transitions.Collections)
        {
            var set = result.Template.TransitionRuleSets.Single(
                candidate => candidate.TransitionKind == collection.Transition.Kind &&
                             candidate.TransitionId == collection.Transition.Id);
            Assert.Empty(set.Rules);
            Assert.Equal(TransitionKind.Kinetic, set.TransitionKind);
        }

        Assert.Equal(model.Transitions.CollectionCount, result.Summary.KineticTransitionRuleSetCount);
        Assert.Equal(model.Transitions.RuleCount, result.Summary.KineticRuleSlotCount);
        Assert.Equal(0, result.Summary.NonParameterizableRuleSetCount);
        Assert.Equal(0, result.Summary.ExplicitRuleFrequencyCount);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, result.Summary.EffectiveFrequencyMinimumHz);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, result.Summary.EffectiveFrequencyMedianHz);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, result.Summary.EffectiveFrequencyMaximumHz);
    }

    [Fact]
    public async Task BariumZirconateCreatesOneSharedHeaderPerChiralPair()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.BariumZirconate);
        var chiralGeometries = model.Energy.PairGeometries.Where(static geometry => geometry.IsChiral).ToArray();
        Assert.Equal(24, chiralGeometries.Length);

        var factory = new ParameterizationFactory(new FixedTimeProvider(created));
        var result = factory.Create(project, model, new ParameterizationTemplateOptions { Name = "Barium zirconate" });

        var sets = result.Template.StablePairSets.AddRange(result.Template.UnstablePairSets);
        var chiralSets = sets.Where(static set => set.IsChiral).ToArray();
        Assert.Equal(12, chiralSets.Length);
        Assert.Equal(12, chiralSets.Select(static set => set.EnergyId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(12, result.Summary.ChiralPairTableCount);

        foreach (var set in chiralSets)
        {
            var table = model.Energy.PairTables.Single(candidate => candidate.EnergyId == set.EnergyId);
            var geometries = model.Energy.PairGeometries.Where(
                geometry => geometry.PairTableIndex == table.Index).ToArray();
            Assert.Equal(2, geometries.Length);
            var representative = Assert.Single(
                geometries,
                geometry => string.Equals(geometry.GeometryId, table.EnergyId, StringComparison.Ordinal));
            Assert.Equal(representative.CenterSiteId, set.CenterSiteId);
            Assert.Equal(representative.PartnerSiteId, set.PartnerSiteId);
            Assert.Empty(set.Entries);
        }

        Assert.False(result.Summary.HasExplicitValues);
    }

    [Fact]
    public async Task OptionsOverrideTheIdentityAndTimestamp()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var id = ParameterizationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80"));
        var timestamp = DateTimeOffset.Parse("2020-01-02T03:04:05+02:00", CultureInfo.InvariantCulture);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));

        var result = factory.Create(
            project,
            model,
            new ParameterizationTemplateOptions { Name = "Named", Id = id, CreatedUtc = timestamp });

        Assert.Equal(id, result.Template.Id);
        Assert.Equal(timestamp.ToUniversalTime(), result.Template.CreatedUtc);
    }

    [Fact]
    public async Task DuplicateNameReportsPrm010WithoutMutatingTheProject()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var existing = Assert.Single(project.Parameterizations);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));

        var duplicate = factory.Create(
            project,
            model,
            new ParameterizationTemplateOptions { Name = existing.Name.ToUpper(CultureInfo.InvariantCulture) });

        var warning = Assert.Single(duplicate.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.DuplicateName, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("parameterization", warning.Path?.Domain);
        Assert.Equal(duplicate.Template.Id.Value, warning.Path?.ObjectId);
        Assert.Equal("name", warning.Path?.Property);
        Assert.Single(project.Parameterizations);

        var unique = factory.Create(
            project,
            model,
            new ParameterizationTemplateOptions { Name = "Another template" });
        Assert.Empty(unique.Diagnostics);
    }

    [Fact]
    public async Task CreationRejectsContractViolatingCompiledModels()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));

        var truncatedTable = model with
        {
            Energy = model.Energy with
            {
                PairTables = [.. model.Energy.PairTables.Select(
                    (table, index) => index == 0 ? table with { Values = table.Values.RemoveAt(0) } : table)],
            },
        };
        var missingGeometry = model with
        {
            Energy = model.Energy with { PairGeometries = [] },
        };
        var truncatedCluster = model with
        {
            Energy = model.Energy with
            {
                ClusterTables = [.. model.Energy.ClusterTables.Select(
                    table => table with { Energies = [] })],
            },
        };

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(project, truncatedTable, ParameterizationTemplateOptions.Default));
        Assert.Throws<InvalidOperationException>(
            () => factory.Create(project, missingGeometry, ParameterizationTemplateOptions.Default));
        Assert.Throws<InvalidOperationException>(
            () => factory.Create(project, truncatedCluster, ParameterizationTemplateOptions.Default));
    }

    [Fact]
    public async Task CreationAndSummaryAreCultureIndependent()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(created));
        var options = new ParameterizationTemplateOptions { Name = "Ceria parameters" };
        var invariant = factory.Create(project, model, options);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var localized = factory.Create(project, model, options);

            Assert.Equal(invariant.Template.Name, localized.Template.Name);
            Assert.Equal(invariant.Template.ModelSignature, localized.Template.ModelSignature);
            Assert.Equal(invariant.Template.CreatedUtc, localized.Template.CreatedUtc);
            Assert.Equal(invariant.Summary, localized.Summary);
            Assert.Equal(invariant.Diagnostics.Length, localized.Diagnostics.Length);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public async Task CreationDoesNotMutateTheProject()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var before = Mocassin.Core.Project.ProjectCanonicalizer.ToCanonicalJson(project);

        _ = new ParameterizationFactory(new FixedTimeProvider(created))
            .Create(project, model, ParameterizationTemplateOptions.Default);

        Assert.Equal(before, Mocassin.Core.Project.ProjectCanonicalizer.ToCanonicalJson(project));
    }
}
