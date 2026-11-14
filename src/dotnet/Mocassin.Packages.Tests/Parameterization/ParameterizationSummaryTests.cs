// SPDX-License-Identifier: MIT

using System.Globalization;
using FsCheck;
using FsCheck.Xunit;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Verifies the required, explicit, and effective metrics of <see cref="ParameterizationSummary"/>.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationSummaryTests
{
    [Fact]
    public async Task CeriaSummaryMatchesTheCompiledModelAndAuthoredBackground()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);

        var summary = ParameterizationFactory.Describe(project, model);

        var pair = PairFacts(model);
        var groupSlots = model.Energy.ClusterTables.Sum(static table => table.OccupationCount);
        var siteSlots = model.Structure.Sites.Sum(site => ParameterizationTestSupport.PopCount(
            ParameterizationTestSupport.SiteMask(model, site.Index)));
        var required = pair.Slots + groupSlots;
        Assert.Equal(pair.Stable, summary.StablePairTableCount);
        Assert.Equal(pair.Unstable, summary.UnstablePairTableCount);
        Assert.Equal(pair.Chiral, summary.ChiralPairTableCount);
        Assert.Equal(pair.Slots, summary.PairSlotCount);
        Assert.Equal(model.Energy.ClusterTableCount, summary.GroupTableCount);
        Assert.Equal(groupSlots, summary.GroupSlotCount);
        Assert.Equal(0, summary.KineticTransitionRuleSetCount);
        Assert.Equal(0, summary.KineticRuleSlotCount);
        Assert.Equal(siteSlots, summary.SiteEnergySlotCount);
        Assert.Equal(required, summary.RequiredSlotCount);
        Assert.Equal(required, summary.UnsetValueCount);
        Assert.Equal(0, summary.SetValueCount);
        Assert.Equal(0, summary.NonDefaultValueCount);
        Assert.Equal(0, summary.ExplicitPairValueCount);
        Assert.Equal(0, summary.ExplicitGroupValueCount);
        Assert.Equal(0, summary.ExplicitRuleFrequencyCount);
        Assert.Equal(0, summary.SiteEnergyOverrideCount);
        Assert.Equal(EstimatedBytes(model), summary.EstimatedValueBytes);
        Assert.False(summary.HasExplicitValues);
        Assert.Equal(-0.5, summary.EffectiveEnergyMinimumEv!.Value);
        Assert.Equal(0.0, summary.EffectiveEnergyMaximumEv!.Value);
        Assert.Null(summary.EffectiveFrequencyMinimumHz);
        Assert.Null(summary.EffectiveFrequencyMedianHz);
        Assert.Null(summary.EffectiveFrequencyMaximumHz);
        Assert.Equal(0.0, summary.IdenticalValueShare);
    }

    [Fact]
    public async Task CeriaKmcSummaryCountsKineticDefaults()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.CeriaKmc);

        var summary = ParameterizationFactory.Describe(project, model);

        Assert.Equal(0, summary.PairSlotCount);
        Assert.Equal(0, summary.GroupSlotCount);
        Assert.Equal(model.Transitions.CollectionCount, summary.KineticTransitionRuleSetCount);
        Assert.Equal(model.Transitions.RuleCount, summary.KineticRuleSlotCount);
        Assert.Equal(model.Transitions.RuleCount, summary.RequiredSlotCount);
        Assert.Equal(model.Transitions.RuleCount, summary.UnsetValueCount);
        Assert.Equal(0, summary.NonParameterizableRuleSetCount);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, summary.EffectiveFrequencyMinimumHz!.Value);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, summary.EffectiveFrequencyMedianHz!.Value);
        Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, summary.EffectiveFrequencyMaximumHz!.Value);
        Assert.Equal(0.0, summary.EffectiveEnergyMinimumEv!.Value);
        Assert.Equal(0.0, summary.EffectiveEnergyMaximumEv!.Value);
        Assert.Equal(EstimatedBytes(model), summary.EstimatedValueBytes);
    }

    [Fact]
    public async Task BariumZirconateCountsSharedChiralSlots()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.BariumZirconate);

        var summary = ParameterizationFactory.Describe(project, model);

        var pair = PairFacts(model);
        Assert.Equal(12, pair.Chiral);
        Assert.Equal(12, summary.ChiralPairTableCount);
        Assert.Equal(pair.Slots, summary.PairSlotCount);
        Assert.Equal(pair.Slots, summary.RequiredSlotCount);
        Assert.Equal(7, summary.SiteEnergySlotCount);
        Assert.Equal(-0.4, summary.EffectiveEnergyMinimumEv!.Value);
        Assert.Equal(0.2, summary.EffectiveEnergyMaximumEv!.Value);
        Assert.False(summary.HasExplicitValues);
    }

    [Fact]
    public async Task PopulatedTemplateSummaryReflectsValuesAndOverrides()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);

        var summary = ParameterizationFactory.Describe(project, model, template);

        var pairValues = template.StablePairSets.AddRange(template.UnstablePairSets)
            .SelectMany(static set => set.Entries)
            .Select(static entry => entry.Energy.Value)
            .ToArray();
        var groupValues = template.GroupSets.SelectMany(static set => set.Entries)
            .Select(static entry => entry.Energy.Value)
            .ToArray();
        var explicitPair = pairValues.Length;
        var explicitGroup = groupValues.Length;
        var required = PairFacts(model).Slots +
                       model.Energy.ClusterTables.Sum(static table => table.OccupationCount);
        Assert.Equal(explicitPair, summary.ExplicitPairValueCount);
        Assert.Equal(explicitGroup, summary.ExplicitGroupValueCount);
        Assert.Equal(explicitPair + explicitGroup, summary.SetValueCount);
        Assert.Equal(required, summary.RequiredSlotCount);
        Assert.Equal(required - explicitPair - explicitGroup, summary.UnsetValueCount);
        Assert.Equal(template.SiteEnergyOverrides.Length, summary.SiteEnergyOverrideCount);
        Assert.True(summary.HasExplicitValues);

        var effective = EffectiveEnergies(project, model, template);
        Assert.Equal(effective.Minimum, summary.EffectiveEnergyMinimumEv!.Value);
        Assert.Equal(effective.Maximum, summary.EffectiveEnergyMaximumEv!.Value);
        Assert.Equal(ExplicitNonDefaultCount(project, model, template), summary.NonDefaultValueCount);
        Assert.Equal(
            IdenticalShare(pairValues.Concat(groupValues).ToArray()),
            summary.IdenticalValueShare);
    }

    [Fact]
    public async Task SummaryIgnoresTemplateIdentityAndMetadata()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(
            DateTimeOffset.Parse("2026-11-14T12:00:00Z", CultureInfo.InvariantCulture)));
        var first = factory.Create(project, model, new ParameterizationTemplateOptions { Name = "One" }).Template;
        var second = factory.Create(project, model, new ParameterizationTemplateOptions { Name = "Two" }).Template;

        Assert.Equal(
            ParameterizationFactory.Describe(project, model, first),
            ParameterizationFactory.Describe(project, model, second));
    }

    [Property(MaxTest = 8)]
    public bool SparseAndDenseTemplatesHaveTheSameEffectiveMetrics(int seed)
    {
        var (project, model) = ParameterizationTestSupport.LoadAndCompile(ParameterizationTestSupport.Ceria);
        var factory = new ParameterizationFactory(new FixedTimeProvider(
            DateTimeOffset.Parse("2026-11-14T12:00:00Z", CultureInfo.InvariantCulture)));
        var created = factory.Create(project, model, ParameterizationTemplateOptions.Default).Template;
        var sparse = ParameterizationTestSupport.Shuffle(created, seed);
        var dense = ParameterizationTestSupport.Shuffle(
            ParameterizationTestSupport.WithExplicitDefaults(created, model),
            seed);

        var sparseSummary = ParameterizationFactory.Describe(project, model, sparse);
        var denseSummary = ParameterizationFactory.Describe(project, model, dense);
        return sparseSummary == ParameterizationFactory.Describe(project, model, created) &&
               sparseSummary.RequiredSlotCount == denseSummary.RequiredSlotCount &&
               sparseSummary.PairSlotCount == denseSummary.PairSlotCount &&
               sparseSummary.GroupSlotCount == denseSummary.GroupSlotCount &&
               sparseSummary.KineticRuleSlotCount == denseSummary.KineticRuleSlotCount &&
               sparseSummary.EffectiveEnergyMinimumEv == denseSummary.EffectiveEnergyMinimumEv &&
               sparseSummary.EffectiveEnergyMaximumEv == denseSummary.EffectiveEnergyMaximumEv &&
               sparseSummary.EffectiveFrequencyMinimumHz == denseSummary.EffectiveFrequencyMinimumHz &&
               sparseSummary.EffectiveFrequencyMedianHz == denseSummary.EffectiveFrequencyMedianHz &&
               sparseSummary.EffectiveFrequencyMaximumHz == denseSummary.EffectiveFrequencyMaximumHz &&
               sparseSummary.EstimatedValueBytes == denseSummary.EstimatedValueBytes &&
               denseSummary.SetValueCount == denseSummary.RequiredSlotCount &&
               denseSummary.UnsetValueCount == 0;
    }

    private static (int Stable, int Unstable, int Chiral, int Slots) PairFacts(CompiledModel model)
    {
        var stable = 0;
        var unstable = 0;
        var chiral = 0;
        var slots = 0;
        foreach (var table in model.Energy.PairTables)
        {
            var geometry = ParameterizationTestSupport.Representative(model, table);
            var stability = model.Structure.Sites[geometry.CenterSiteIndex].Stability;
            if (stability == SiteStability.Stable)
            {
                stable++;
            }
            else
            {
                unstable++;
            }

            var centerMask = ParameterizationTestSupport.SiteMask(model, geometry.CenterSiteIndex);
            var partnerMask = ParameterizationTestSupport.SiteMask(model, geometry.PartnerSiteIndex);
            if (geometry.IsChiral)
            {
                chiral++;
                slots += ParameterizationTestSupport.ChiralSlotCount(centerMask, partnerMask);
            }
            else
            {
                slots += ParameterizationTestSupport.PopCount(centerMask) *
                         ParameterizationTestSupport.PopCount(partnerMask);
            }
        }

        return (stable, unstable, chiral, slots);
    }

    private static long EstimatedBytes(CompiledModel model)
    {
        long bytes = 0;
        foreach (var table in model.Energy.PairTables)
        {
            bytes += (long)table.ParticleCount * table.ParticleCount * sizeof(double);
        }

        foreach (var table in model.Energy.ClusterTables)
        {
            bytes += (long)table.OccupationCount * sizeof(double);
        }

        bytes += (long)model.Structure.PositionCount * model.Particles.Count * sizeof(double);
        bytes += (long)model.Transitions.RuleCount * sizeof(double);
        return bytes;
    }

    private static (double Minimum, double Maximum) EffectiveEnergies(
        Mocassin.Core.Project.Project project,
        CompiledModel model,
        ParameterizationTemplate template)
    {
        var values = new List<double>();
        if (model.Energy.PairTableCount > 0 ||
            model.Energy.ClusterOccupationCount > 0 ||
            ((long)model.Structure.PositionCount * model.Particles.Count) > 0)
        {
            values.Add(0.0);
        }

        foreach (var set in template.StablePairSets.AddRange(template.UnstablePairSets))
        {
            values.AddRange(set.Entries.Select(static entry => entry.Energy.Value));
        }

        foreach (var set in template.GroupSets)
        {
            values.AddRange(set.Entries.Select(static entry => entry.Energy.Value));
        }

        var authored = new Dictionary<(Mocassin.Core.Ids.CellSiteId Site, Mocassin.Core.Ids.ParticleId Particle), double>();
        if (!project.Model.SiteEnergies.IsDefault)
        {
            foreach (var set in project.Model.SiteEnergies)
            {
                foreach (var entry in set.Entries)
                {
                    authored[(set.SiteId, entry.ParticleId)] = entry.Energy.Value;
                }
            }
        }

        foreach (var entry in template.SiteEnergyOverrides)
        {
            authored[(entry.SiteId, entry.ParticleId)] = entry.Energy.Value;
        }

        foreach (var site in model.Structure.Sites)
        {
            var mask = ParameterizationTestSupport.SiteMask(model, site.Index);
            for (var particle = 1; particle < model.Particles.Count; particle++)
            {
                if ((mask & (1UL << particle)) == 0)
                {
                    continue;
                }

                values.Add(authored.GetValueOrDefault((site.Id, model.Particles.Particles[particle].Id)));
            }
        }

        return (values.Min(), values.Max());
    }

    private static int ExplicitNonDefaultCount(
        Mocassin.Core.Project.Project project,
        CompiledModel model,
        ParameterizationTemplate template)
    {
        var count = 0;
        foreach (var value in template.StablePairSets.AddRange(template.UnstablePairSets)
                     .SelectMany(static set => set.Entries)
                     .Select(static entry => entry.Energy.Value)
                     .Concat(template.GroupSets.SelectMany(static set => set.Entries)
                         .Select(static entry => entry.Energy.Value)))
        {
            if (value != 0.0)
            {
                count++;
            }
        }

        var authored = new Dictionary<(Mocassin.Core.Ids.CellSiteId Site, Mocassin.Core.Ids.ParticleId Particle), double>();
        if (!project.Model.SiteEnergies.IsDefault)
        {
            foreach (var set in project.Model.SiteEnergies)
            {
                foreach (var entry in set.Entries)
                {
                    authored[(set.SiteId, entry.ParticleId)] = entry.Energy.Value;
                }
            }
        }

        foreach (var entry in template.SiteEnergyOverrides)
        {
            if (authored.GetValueOrDefault((entry.SiteId, entry.ParticleId)) != entry.Energy.Value)
            {
                count++;
            }
        }

        return count;
    }

    private static double IdenticalShare(double[] values)
    {
        if (values.Length == 0)
        {
            return 0.0;
        }

        var maximum = values
            .Where(static value => value != 0.0)
            .GroupBy(static value => value)
            .Select(static group => group.Count())
            .DefaultIfEmpty(0)
            .Max();
        return (double)maximum / values.Length;
    }
}
