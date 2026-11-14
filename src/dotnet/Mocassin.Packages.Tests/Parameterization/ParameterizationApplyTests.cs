// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Acceptance tests for <see cref="ParameterizationService.Apply"/> over real sample projects and the real
/// compiler.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationApplyTests
{
    [Fact]
    public async Task ImplicitDefaultAppliesZeroEnergiesAndTheAuthoredSiteBackground()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);

        var result = new ParameterizationService().Apply(project, model);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
        Assert.Null(result.Diff);
        Assert.NotNull(result.Model);
        var applied = result.Model!;
        Assert.Equal(model.ModelSignature, applied.ModelSignature);
        Assert.Equal(model.Energy.PairTableCount, applied.Values.PairTables.Length);
        Assert.All(applied.Values.PairTables, static table => Assert.All(table, static value => Assert.Equal(0.0, value)));
        Assert.All(applied.Values.GroupSets, static table => Assert.All(table, static value => Assert.Equal(0.0, value)));
        Assert.All(
            applied.Values.TransitionRuleFrequencies,
            static value => Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, value));

        var ceriaSite = model.Structure.Sites.Single(
            site => site.Id == CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f06")));
        var oxygen = model.Particles.IndexById[
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f01"))];
        foreach (var positionIndex in ceriaSite.PositionIndices)
        {
            Assert.Equal(-0.5, SiteEnergy(applied, model, positionIndex, oxygen));
        }

        Assert.Equal(ParameterizationFactory.Describe(project, model), result.Summary);
        Assert.False(result.Summary.HasExplicitValues);
        Assert.Equal(applied.Values.SiteEnergies.Length, model.Structure.PositionCount * model.Particles.Count);
        for (var position = 0; position < model.Structure.PositionCount; position++)
        {
            Assert.Equal(0.0, applied.Values.SiteEnergies[position * model.Particles.Count]);
        }
    }

    [Fact]
    public async Task CeriaTemplateAppliesEveryExplicitValueAndTheGoldenSignature()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);

        var result = new ParameterizationService().Apply(project, model, template);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Model);
        var applied = result.Model!;
        AssertPairEntries(applied, model, template);
        AssertGroupEntries(applied, model, template);
        foreach (var entry in template.SiteEnergyOverrides)
        {
            var site = model.Structure.Sites.Single(site => site.Id == entry.SiteId);
            var particle = model.Particles.IndexById[entry.ParticleId];
            foreach (var positionIndex in site.PositionIndices)
            {
                Assert.Equal(entry.Energy.Value, SiteEnergy(applied, model, positionIndex, particle));
            }
        }

        Assert.NotEqual(model.ModelSignature, applied.Signature);
        Assert.Equal(ceriaTemplateSignature, applied.Signature);
    }

    [Fact]
    public async Task BariumZirconateTemplateAppliesAndSignsDeterministically()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.BariumZirconateParameters);
        var template = Assert.Single(project.Parameterizations);
        var service = new ParameterizationService();

        var first = service.Apply(project, model, template);
        var second = service.Apply(project, model, template);

        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Model);
        Assert.Equal(first.Model!.Signature, second.Model!.Signature);
        Assert.Equal(bariumZirconateTemplateSignature, first.Model.Signature);
        AssertPairEntries(first.Model, model, template);
        AssertChiralTablesAreSymmetric(first.Model, model, template);
    }

    [Fact]
    public async Task ChiralEntryIsStoredSymmetricallyRegardlessOfAuthoredDirection()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.BariumZirconate);
        var template = new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch))
            .Create(project, model, new ParameterizationTemplateOptions { Name = "Chiral" })
            .Template;
        var descriptor = FindChiralDescriptorWithDistinctParticles(model);
        var firstIndex = FirstSetBit(descriptor.CenterMask);
        var secondIndex = SecondSetBit(descriptor.CenterMask, firstIndex);
        Assert.True(secondIndex > firstIndex);
        var first = model.Particles.Particles[firstIndex].Id;
        var second = model.Particles.Particles[secondIndex].Id;
        Assert.NotEqual(first, second);

        var edit = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            descriptor.EnergyId,
            first,
            second,
            new EnergyEv(0.22));
        Assert.Empty(edit.Diagnostics);
        Assert.NotNull(edit.Template);
        var result = new ParameterizationService().Apply(project, model, edit.Template);

        Assert.True(result.IsSuccess);
        var table = result.Model!.Values.PairTables[descriptor.TableIndex];
        var center = model.Particles.IndexById[first];
        var partner = model.Particles.IndexById[second];
        var particleCount = model.Energy.PairTables[descriptor.TableIndex].ParticleCount;
        Assert.Equal(0.22, table[(center * particleCount) + partner]);
        Assert.Equal(0.22, table[(partner * particleCount) + center]);
    }

    [Fact]
    public async Task ApplyIsDeterministicAndDoesNotMutateTheProject()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var before = Mocassin.Core.Project.ProjectCanonicalizer.ToCanonicalJson(project);
        var service = new ParameterizationService();

        var first = service.Apply(project, model, template).Model!;
        var second = service.Apply(project, model, template).Model!;

        Assert.Equal(first.Signature, second.Signature);
        Assert.Equal(first.Values.PairTables.Length, second.Values.PairTables.Length);
        for (var index = 0; index < first.Values.PairTables.Length; index++)
        {
            Assert.Equal(first.Values.PairTables[index].AsEnumerable(), second.Values.PairTables[index].AsEnumerable());
        }

        for (var index = 0; index < first.Values.GroupSets.Length; index++)
        {
            Assert.Equal(first.Values.GroupSets[index].AsEnumerable(), second.Values.GroupSets[index].AsEnumerable());
        }

        Assert.Equal(first.Values.SiteEnergies.AsEnumerable(), second.Values.SiteEnergies.AsEnumerable());
        Assert.Equal(
            first.Values.TransitionRuleFrequencies.AsEnumerable(),
            second.Values.TransitionRuleFrequencies.AsEnumerable());
        Assert.Equal(before, Mocassin.Core.Project.ProjectCanonicalizer.ToCanonicalJson(project));
    }

    [Fact]
    public async Task SignatureIsStableAndSeparatesValuesFromTheModelSignature()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var service = new ParameterizationService();
        var baseline = service.Apply(project, model, template).Model!;
        var repeated = service.Apply(project, model, template).Model!;
        Assert.Equal(baseline.Signature, repeated.Signature);

        var overrideEntry = Assert.Single(template.SiteEnergyOverrides);
        var edit = ParameterizationTemplateEditor.SetSiteEnergyOverride(
            template,
            overrideEntry.SiteId,
            overrideEntry.ParticleId,
            new EnergyEv(overrideEntry.Energy.Value + 0.05));
        var changed = service.Apply(project, model, edit.Template).Model!;

        Assert.NotEqual(baseline.Signature, changed.Signature);
        Assert.Equal(baseline.ModelSignature, changed.ModelSignature);
        Assert.Equal(model.ModelSignature, changed.ModelSignature);
    }

    [Fact]
    public async Task QuantizationUsesTwelveDecimalsAndNormalizesNegativeZero()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var set = template.StablePairSets.First(static candidate => candidate.Entries.Length > 0);
        var entry = set.Entries[0];

        Assert.Equal(1e-12, ApplyPairValue(project, model, template, set, entry, 5e-13));
        Assert.Equal(-1e-12, ApplyPairValue(project, model, template, set, entry, -5e-13));
        Assert.Equal(0.0, ApplyPairValue(project, model, template, set, entry, -4e-13));
        Assert.Equal(0.3, ApplyPairValue(project, model, template, set, entry, 0.30000000000000004));
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(ApplyPairValue(project, model, template, set, entry, -0.0)));
    }

    [Fact]
    public async Task MaxAppliedValueBytesIsEnforcedBeforeAllocation()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var options = new ParameterizationOptions { MaxAppliedValueBytes = 1 };

        var result = new ParameterizationService().Apply(project, model, null, options);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Model);
        Assert.True(result.Summary.EstimatedValueBytes > 1);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(ParameterizationDiagnosticCodes.LimitExceeded, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task UnknownAndForbiddenTemplateEntriesAreWarningsThatLeaveTheDefaults()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var unknownSet = new PairEnergySet(
            new string('0', 64),
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f05")),
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f06")),
            1,
            false,
            [new PairEnergyEntry(
                model.Particles.Particles[1].Id,
                model.Particles.Particles[1].Id,
                new EnergyEv(1.0))]);
        var descriptor = FindAchiralDescriptorWithForbiddenParticle(model);
        var forbidden = FirstForbiddenParticle(model, descriptor);
        var forbiddenSet = new PairEnergySet(
            descriptor.EnergyId,
            descriptor.CenterSiteId,
            descriptor.PartnerSiteId,
            descriptor.DistanceBucketFm,
            descriptor.IsChiral,
            [new PairEnergyEntry(forbidden, forbidden, new EnergyEv(2.0))]);
        var edited = template with
        {
            StablePairSets = [.. template.StablePairSets, unknownSet, forbiddenSet],
        };

        var result = new ParameterizationService().Apply(project, model, edited);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Model);
        Assert.Contains(result.Diagnostics, item => item.Code == ParameterizationDiagnosticCodes.UnknownReference);
        Assert.Contains(result.Diagnostics, item => item.Code == ParameterizationDiagnosticCodes.ForbiddenParticle);
        Assert.All(result.Diagnostics, item => Assert.Equal(DiagnosticSeverity.Warning, item.Severity));
        var table = result.Model!.Values.PairTables[descriptor.TableIndex];
        var forbiddenIndex = model.Particles.IndexById[forbidden];
        Assert.Equal(0.0, table[(forbiddenIndex * model.Energy.PairTables[descriptor.TableIndex].ParticleCount) + forbiddenIndex]);
    }

    [Fact]
    public async Task MetropolisRuleSetsAreIgnoredAndKineticRulesApply()
    {
        var (mmcProject, mmcModel) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaMmc);
        var mmcTemplate = new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch))
            .Create(mmcProject, mmcModel, new ParameterizationTemplateOptions { Name = "MMC" })
            .Template;
        var metropolis = mmcModel.Transitions.Collections.First(
            static collection => collection.Transition.Kind == TransitionKind.Metropolis);
        var mmcEdited = mmcTemplate with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Metropolis,
                    metropolis.Transition.Id,
                    [new RuleFrequencyEntry(
                        mmcModel.Transitions.Rules[metropolis.RuleIndices[0]].RuleGeometryId,
                        new FrequencyHz(5.0e13))]),
            ],
        };
        var mmcResult = new ParameterizationService().Apply(mmcProject, mmcModel, mmcEdited);
        Assert.True(mmcResult.IsSuccess);
        Assert.All(
            mmcResult.Model!.Values.TransitionRuleFrequencies,
            static value => Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, value));

        var (kmcProject, kmcModel) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var kmcTemplate = new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch))
            .Create(kmcProject, kmcModel, new ParameterizationTemplateOptions { Name = "KMC" })
            .Template;
        var kinetic = kmcModel.Transitions.Collections.First(
            static collection => collection.Transition.Kind == TransitionKind.Kinetic);
        var rule = kmcModel.Transitions.Rules[kinetic.RuleIndices[0]];
        var kmcEdited = ParameterizationTemplateEditor.SetRuleFrequency(
            kmcTemplate,
            TransitionKind.Kinetic,
            kinetic.Transition.Id,
            rule.RuleGeometryId,
            new FrequencyHz(2.0e13)).Template;
        var kmcResult = new ParameterizationService().Apply(kmcProject, kmcModel, kmcEdited);

        Assert.True(kmcResult.IsSuccess);
        Assert.Equal(2.0e13, kmcResult.Model!.Values.TransitionRuleFrequencies[rule.Index]);
        Assert.All(
            kmcResult.Model.Values.TransitionRuleFrequencies.Where((_, index) => index != rule.Index),
            static value => Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, value));
    }

    [Fact]
    public async Task DefaultTemplateFactoryProducesTheReservedInMemoryTemplate()
    {
        var (_, model) = await ParameterizationTestSupport.LoadAndCompileAsync(ParameterizationTestSupport.Ceria);

        var template = ParameterizationTemplateDefaults.CreateDefault(model.ModelSignature);

        Assert.Equal(Guid.Empty, template.Id.Value);
        Assert.Equal("Default parameters", template.Name);
        Assert.Equal(model.ModelSignature, template.ModelSignature);
        Assert.Equal(DateTimeOffset.UnixEpoch, template.CreatedUtc);
        Assert.Empty(template.StablePairSets);
        Assert.Empty(template.UnstablePairSets);
        Assert.Empty(template.GroupSets);
        Assert.Empty(template.SiteEnergyOverrides);
        Assert.Empty(template.TransitionRuleSets);
    }

    [Fact]
    public async Task DescribeDelegatesToTheFactorySummary()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var service = new ParameterizationService();

        Assert.Equal(
            ParameterizationFactory.Describe(project, model, template),
            service.Describe(project, model, template));
        Assert.Equal(
            ParameterizationFactory.Describe(project, model),
            service.Describe(project, model));
    }

    private const string ceriaTemplateSignature = "cac3b231e667164fa7dfd4daa7b326dbc5e23bde17fe30873f775f7f77975042";

    private const string bariumZirconateTemplateSignature = "a7f6ebd97ea336308360949e4fe703bb1c88220a545e9f9db06f009cd6d32b9d";

    private static double ApplyPairValue(
        Mocassin.Core.Project.Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        PairEnergySet set,
        PairEnergyEntry entry,
        double rawValue)
    {
        var edit = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            set.EnergyId,
            entry.CenterParticleId,
            entry.PartnerParticleId,
            new EnergyEv(rawValue));
        var result = new ParameterizationService().Apply(project, model, edit.Template);
        Assert.True(result.IsSuccess);
        var tableIndex = model.Energy.PairTables.Single(candidate => candidate.EnergyId == set.EnergyId).Index;
        var center = model.Particles.IndexById[entry.CenterParticleId];
        var partner = model.Particles.IndexById[entry.PartnerParticleId];
        var particleCount = model.Energy.PairTables[tableIndex].ParticleCount;
        return result.Model!.Values.PairTables[tableIndex][(center * particleCount) + partner];
    }

    private static void AssertPairEntries(
        ParameterizedModel applied,
        CompiledModel model,
        ParameterizationTemplate template)
    {
        foreach (var set in template.StablePairSets.AddRange(template.UnstablePairSets))
        {
            var tableIndex = model.Energy.PairTables.Single(candidate => candidate.EnergyId == set.EnergyId).Index;
            var table = applied.Values.PairTables[tableIndex];
            var particleCount = model.Energy.PairTables[tableIndex].ParticleCount;
            foreach (var entry in set.Entries)
            {
                var center = model.Particles.IndexById[entry.CenterParticleId];
                var partner = model.Particles.IndexById[entry.PartnerParticleId];
                Assert.Equal(entry.Energy.Value, table[(center * particleCount) + partner]);
                if (set.IsChiral)
                {
                    Assert.Equal(entry.Energy.Value, table[(partner * particleCount) + center]);
                }
            }
        }
    }

    private static void AssertGroupEntries(
        ParameterizedModel applied,
        CompiledModel model,
        ParameterizationTemplate template)
    {
        foreach (var set in template.GroupSets)
        {
            var tableIndex = model.Energy.ClusterTables.Single(
                candidate => candidate.ClusterEnergyId == set.ClusterEnergyId).Index;
            var table = model.Energy.ClusterTables[tableIndex];
            foreach (var entry in set.Entries)
            {
                var indices = entry.OccupationParticleIds
                    .Select(particle => model.Particles.IndexById[particle])
                    .ToArray();
                var occupationIndex = table.Occupations
                    .Select(static (occupation, index) => (occupation, index))
                    .First(candidate => candidate.occupation.CanonicalTuple.SequenceEqual(indices)).index;
                Assert.Equal(entry.Energy.Value, applied.Values.GroupSets[tableIndex][occupationIndex]);
            }
        }
    }

    private static void AssertChiralTablesAreSymmetric(
        ParameterizedModel applied,
        CompiledModel model,
        ParameterizationTemplate template)
    {
        foreach (var set in template.StablePairSets.AddRange(template.UnstablePairSets).Where(
                     static candidate => candidate.IsChiral))
        {
            var tableIndex = model.Energy.PairTables.Single(candidate => candidate.EnergyId == set.EnergyId).Index;
            var values = applied.Values.PairTables[tableIndex];
            var particleCount = model.Energy.PairTables[tableIndex].ParticleCount;
            for (var center = 0; center < particleCount; center++)
            {
                for (var partner = 0; partner < particleCount; partner++)
                {
                    Assert.Equal(
                        values[(center * particleCount) + partner],
                        values[(partner * particleCount) + center]);
                }
            }
        }
    }

    private static double SiteEnergy(
        ParameterizedModel applied,
        CompiledModel model,
        int positionIndex,
        int particleIndex) =>
        applied.Values.SiteEnergies[(positionIndex * model.Particles.Count) + particleIndex];

    private static PairTableInfo FindChiralDescriptorWithDistinctParticles(CompiledModel model)
    {
        foreach (var table in model.Energy.PairTables)
        {
            var geometry = model.Energy.PairGeometries.FirstOrDefault(
                candidate => candidate.PairTableIndex == table.Index && candidate.IsChiral);
            if (geometry is null)
            {
                continue;
            }

            var centerMask = ParameterizationTestSupport.SiteMask(model, geometry.CenterSiteIndex);
            var partnerMask = ParameterizationTestSupport.SiteMask(model, geometry.PartnerSiteIndex);
            if (ParameterizationTestSupport.PopCount(centerMask) >= 2 &&
                ParameterizationTestSupport.PopCount(partnerMask) >= 2)
            {
                return new PairTableInfo(
                    table.Index,
                    table.EnergyId,
                    geometry.CenterSiteId,
                    geometry.PartnerSiteId,
                    geometry.DistanceBucketFm,
                    true,
                    centerMask,
                    partnerMask);
            }
        }

        throw new InvalidOperationException("No chiral pair table with distinct particles was found.");
    }

    private static PairTableInfo FindAchiralDescriptorWithForbiddenParticle(CompiledModel model)
    {
        foreach (var table in model.Energy.PairTables)
        {
            var geometry = ParameterizationTestSupport.Representative(model, table);
            if (geometry.IsChiral)
            {
                continue;
            }

            var centerMask = ParameterizationTestSupport.SiteMask(model, geometry.CenterSiteIndex);
            var partnerMask = ParameterizationTestSupport.SiteMask(model, geometry.PartnerSiteIndex);
            if (FirstForbiddenParticle(model, centerMask) > 0)
            {
                return new PairTableInfo(
                    table.Index,
                    table.EnergyId,
                    geometry.CenterSiteId,
                    geometry.PartnerSiteId,
                    geometry.DistanceBucketFm,
                    geometry.IsChiral,
                    centerMask,
                    partnerMask);
            }
        }

        throw new InvalidOperationException("No pair table with a forbidden particle was found.");
    }

    private static ParticleId FirstForbiddenParticle(CompiledModel model, PairTableInfo descriptor)
    {
        var index = FirstForbiddenParticle(model, descriptor.CenterMask);
        return model.Particles.Particles[index].Id;
    }

    private static int FirstForbiddenParticle(CompiledModel model, ulong mask)
    {
        for (var index = 1; index < model.Particles.Count; index++)
        {
            if ((mask & (1UL << index)) == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FirstSetBit(ulong mask) => SecondSetBit(mask, 0);

    private static int SecondSetBit(ulong mask, int after)
    {
        for (var index = after + 1; index < 64; index++)
        {
            if ((mask & (1UL << index)) != 0)
            {
                return index;
            }
        }

        return -1;
    }

    private sealed record PairTableInfo(
        int TableIndex,
        string EnergyId,
        CellSiteId CenterSiteId,
        CellSiteId PartnerSiteId,
        int DistanceBucketFm,
        bool IsChiral,
        ulong CenterMask,
        ulong PartnerMask);
}
