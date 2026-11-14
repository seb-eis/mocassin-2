// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.Tests.Parameterization;

/// <summary>
/// Per-code emission and acceptance tests for the compiled-bound <c>PRM</c> diagnostics.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ParameterizationValidationTests
{
    [Fact]
    public async Task StaleTemplateEmitsPrm001WithoutADiffFromValidateAndWithADiffFromApply()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var changed = ChangeCeriaCell(project);
        var compilation = new ModelCompiler().Compile(changed);
        Assert.True(
            compilation.IsSuccess,
            string.Join(Environment.NewLine, compilation.Diagnostics.Select(static item => item.ToString())));
        var changedModel = compilation.Model!;
        Assert.NotEqual(model.ModelSignature, changedModel.ModelSignature);
        var service = new ParameterizationService();

        var diagnostics = service.Validate(changed, changedModel, template);

        var mismatch = Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.ModelSignatureMismatch);
        Assert.Equal(DiagnosticSeverity.Error, mismatch.Severity);
        Assert.DoesNotContain(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.RequiredValuesUnset);
        var applied = service.Apply(changed, changedModel, template);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
        Assert.NotNull(applied.Diff);
        Assert.Equal(ParameterizationDiagnosticCodes.ModelSignatureMismatch, applied.Diagnostics[0].Code);
    }

    [Fact]
    public async Task ImplicitDefaultEmitsNeitherPrm003NorPrm006()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);

        var diagnostics = new ParameterizationService().Validate(project, model);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ExplicitEmptyTemplateEmitsPrm003AndPrm006ButStillApplies()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var template = CreateTemplate(project, model);
        var service = new ParameterizationService();

        var diagnostics = service.Validate(project, model, template);

        var unset = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.RequiredValuesUnset);
        Assert.Equal(DiagnosticSeverity.Warning, unset.Severity);
        Assert.Contains("required values unset", unset.Message, StringComparison.Ordinal);
        var empty = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.NoExplicitValues);
        Assert.Equal(DiagnosticSeverity.Warning, empty.Severity);

        var applied = service.Apply(project, model, template);
        Assert.True(applied.IsSuccess);
        Assert.NotNull(applied.Model);
        Assert.Equal(
            diagnostics.Select(static item => item.Code),
            applied.Diagnostics.Select(static item => item.Code));
    }

    [Fact]
    public async Task ExplicitZeroValueSuppressesPrm006ButNotPrm003()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var slot = FindPairSlot(model);
        var template = CreateTemplate(project, model);
        var edit = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            slot.EnergyId,
            slot.Center,
            slot.Partner,
            new EnergyEv(0.0));
        Assert.NotNull(edit.Template);

        var diagnostics = new ParameterizationService().Validate(project, model, edit.Template);

        Assert.Contains(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.RequiredValuesUnset);
        Assert.DoesNotContain(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.NoExplicitValues);
    }

    [Fact]
    public async Task MetropolisRuleSetEmitsPrm011AndLeavesTheCompiledDefaults()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaMmc);
        var template = CreateTemplate(project, model);
        var metropolis = model.Transitions.Collections.First(
            static collection => collection.Transition.Kind == TransitionKind.Metropolis);
        var rule = model.Transitions.Rules[metropolis.RuleIndices[0]];
        var edited = template with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Metropolis,
                    metropolis.Transition.Id,
                    [new RuleFrequencyEntry(rule.RuleGeometryId, new FrequencyHz(2.0e13))]),
            ],
        };

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var ignored = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.MetropolisRuleSetIgnored);
        Assert.Equal(DiagnosticSeverity.Warning, ignored.Severity);
        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.True(applied.IsSuccess);
        Assert.All(
            applied.Model!.Values.TransitionRuleFrequencies,
            static value => Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, value));
    }

    [Fact]
    public async Task UniformNonDefaultValuesEmitPrm015()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var template = CreateTemplate(project, model);
        var filled = ParameterizationTestSupport.WithExplicitDefaults(template, model);
        var uniform = filled with
        {
            StablePairSets = [.. filled.StablePairSets.Select(static set => WithUniformEntries(set, 1.0))],
            UnstablePairSets = [.. filled.UnstablePairSets.Select(static set => WithUniformEntries(set, 1.0))],
        };

        var diagnostics = new ParameterizationService().Validate(project, model, uniform);

        var uniformDiagnostic = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.UniformValues);
        Assert.Equal(DiagnosticSeverity.Warning, uniformDiagnostic.Severity);
        Assert.Contains("95", uniformDiagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImplausibleFiniteFrequencyEmitsPrm016()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var kinetic = FindKineticRule(model);
        var template = CreateTemplate(project, model);
        var edited = ParameterizationTemplateEditor.SetRuleFrequency(
            template,
            TransitionKind.Kinetic,
            kinetic.Collection.Transition.Id,
            kinetic.Rule.RuleGeometryId,
            new FrequencyHz(1.0e-9)).Template;

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var implausible = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.ImplausibleFrequency);
        Assert.Equal(DiagnosticSeverity.Warning, implausible.Severity);
        Assert.DoesNotContain(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.InvalidFrequency);
        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.True(applied.IsSuccess);
        Assert.Equal(1.0e-9, applied.Model!.Values.TransitionRuleFrequencies[kinetic.Rule.Index]);
    }

    [Fact]
    public async Task OutOfRangeEnergyIsAnErrorAndProducesNoModel()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var slot = FindPairSlot(model);
        var template = CreateTemplate(project, model);
        var edited = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            slot.EnergyId,
            slot.Center,
            slot.Partner,
            new EnergyEv(EnergyLimits.MaxEnergy.Value + 1.0)).Template;

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var invalid = Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.InvalidEnergy);
        Assert.Equal(DiagnosticSeverity.Error, invalid.Severity);
        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
    }

    [Fact]
    public async Task NonPositiveFrequencyIsAnErrorAndProducesNoModel()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var kinetic = FindKineticRule(model);
        var template = CreateTemplate(project, model);
        var edited = ParameterizationTemplateEditor.SetRuleFrequency(
            template,
            TransitionKind.Kinetic,
            kinetic.Collection.Transition.Id,
            kinetic.Rule.RuleGeometryId,
            new FrequencyHz(0.0)).Template;

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var invalid = Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.InvalidFrequency);
        Assert.Equal(DiagnosticSeverity.Error, invalid.Severity);
        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
    }

    [Fact]
    public async Task HugeFiniteFrequencyIsAnErrorAndProducesNoModel()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var kinetic = FindKineticRule(model);
        var template = CreateTemplate(project, model);
        var edited = ParameterizationTemplateEditor.SetRuleFrequency(
            template,
            TransitionKind.Kinetic,
            kinetic.Collection.Transition.Id,
            kinetic.Rule.RuleGeometryId,
            new FrequencyHz(1e300)).Template;

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var invalid = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidFrequency);
        Assert.Equal(DiagnosticSeverity.Error, invalid.Severity);
        Assert.Contains("canonical 12-decimal projection", invalid.Message, StringComparison.Ordinal);

        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
        Assert.Contains(
            applied.Diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidFrequency &&
                item.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task HugeFiniteEnergyIsAnErrorAndProducesNoModel()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var slot = FindPairSlot(model);
        var template = CreateTemplate(project, model);
        var edited = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            slot.EnergyId,
            slot.Center,
            slot.Partner,
            new EnergyEv(1e300)).Template;

        var diagnostics = new ParameterizationService().Validate(project, model, edited);

        var invalid = Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.InvalidEnergy);
        Assert.Equal(DiagnosticSeverity.Error, invalid.Severity);

        var applied = new ParameterizationService().Apply(project, model, edited);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
        Assert.Contains(
            applied.Diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidEnergy &&
                item.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task StalePairAndGroupEntriesDoNotSuppressCompletenessWarnings()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaParameters);
        var template = Assert.Single(project.Parameterizations);
        var particle = model.Particles.Particles[1].Id;
        var stalePair = template.StablePairSets[0] with
        {
            EnergyId = new string('0', 64),
            Entries = [new PairEnergyEntry(particle, particle, new EnergyEv(1.0))],
        };
        var group = template.GroupSets[0];
        var table = model.Energy.ClusterTables.Single(
            candidate => string.Equals(candidate.ClusterEnergyId, group.ClusterEnergyId, StringComparison.Ordinal));
        var occupation = table.Occupations[0].CanonicalTuple
            .Select(index => model.Particles.Particles[index].Id)
            .ToImmutableArray();
        var staleGroup = group with
        {
            ClusterEnergyId = new string('1', 64),
            Entries = [new GroupEnergyEntry(occupation, new EnergyEv(1.0))],
        };
        var stale = template with
        {
            StablePairSets = [stalePair],
            UnstablePairSets = [],
            GroupSets = [staleGroup],
            SiteEnergyOverrides = [],
            TransitionRuleSets = [],
        };
        var required = ParameterizationFactory.Describe(project, model, template).RequiredSlotCount;

        var summary = ParameterizationFactory.Describe(project, model, stale);

        Assert.Equal(0, summary.SetValueCount);
        Assert.Equal(required, summary.UnsetValueCount);
        Assert.False(summary.HasExplicitValues);

        var service = new ParameterizationService();
        var diagnostics = service.Validate(project, model, stale);

        Assert.Equal(
            2,
            diagnostics.Count(static item => item.Code == ParameterizationDiagnosticCodes.UnknownReference));
        var unset = Assert.Single(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.RequiredValuesUnset);
        Assert.Contains(
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{required} of {required} required values unset"),
            unset.Message,
            StringComparison.Ordinal);
        Assert.Contains(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.NoExplicitValues);

        var applied = service.Apply(project, model, stale);
        Assert.True(applied.IsSuccess);
        Assert.NotNull(applied.Model);
        Assert.Contains(
            applied.Diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.NoExplicitValues);
    }

    [Fact]
    public async Task StaleRuleEntriesDoNotSuppressCompletenessWarnings()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.CeriaKmc);
        var template = CreateTemplate(project, model);
        var stale = template with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    Guid.Parse("55555555-5555-7555-8555-555555555555"),
                    [new RuleFrequencyEntry(new string('2', 64), new FrequencyHz(2.0e13))]),
            ],
        };
        var required = ParameterizationFactory.Describe(project, model, template).RequiredSlotCount;

        var summary = ParameterizationFactory.Describe(project, model, stale);

        Assert.Equal(0, summary.SetValueCount);
        Assert.Equal(required, summary.UnsetValueCount);
        Assert.False(summary.HasExplicitValues);

        var diagnostics = new ParameterizationService().Validate(project, model, stale);

        Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.UnknownReference);
        Assert.Contains(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.RequiredValuesUnset);
        Assert.Contains(
            diagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.NoExplicitValues);

        var applied = new ParameterizationService().Apply(project, model, stale);
        Assert.True(applied.IsSuccess);
        Assert.All(
            applied.Model!.Values.TransitionRuleFrequencies,
            static value => Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, value));
    }

    [Fact]
    public async Task ApplyAndValidateRunTheStructuralTemplateValidator()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var template = CreateTemplate(project, model);
        var service = new ParameterizationService();

        var overlong = template with
        {
            Name = new string('a', ParameterizationLimits.TemplateNameLength + 1),
        };
        var limit = Assert.Single(
            service.Validate(project, model, overlong),
            static item => item.Code == ParameterizationDiagnosticCodes.LimitExceeded &&
                item.Path?.Property == "name");
        Assert.Equal(DiagnosticSeverity.Error, limit.Severity);
        AssertApplyFailsWithStructuralError(
            service,
            project,
            model,
            overlong,
            ParameterizationDiagnosticCodes.LimitExceeded);

        var emptyId = template with { Id = ParameterizationId.Create(Guid.Empty) };
        var empty = Assert.Single(
            service.Validate(project, model, emptyId),
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidKey &&
                item.Path?.Property == "id");
        Assert.Equal(DiagnosticSeverity.Error, empty.Severity);
        AssertApplyFailsWithStructuralError(
            service,
            project,
            model,
            emptyId,
            ParameterizationDiagnosticCodes.InvalidKey);

        var malformed = template with
        {
            StablePairSets =
            [
                new PairEnergySet("not-a-stable-id", default, default, 0, false),
            ],
        };
        var malformedDiagnostics = service.Validate(project, model, malformed);
        Assert.Contains(
            malformedDiagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidKey);
        Assert.DoesNotContain(
            malformedDiagnostics,
            static item => item.Code == ParameterizationDiagnosticCodes.UnknownReference);
        AssertApplyFailsWithStructuralError(
            service,
            project,
            model,
            malformed,
            ParameterizationDiagnosticCodes.InvalidKey);

        var duplicate = template with
        {
            StablePairSets = [template.StablePairSets[0], template.StablePairSets[0]],
        };
        Assert.Contains(
            service.Validate(project, model, duplicate),
            static item => item.Code == ParameterizationDiagnosticCodes.InvalidKey);
        AssertApplyFailsWithStructuralError(
            service,
            project,
            model,
            duplicate,
            ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public async Task UnknownSetForbiddenParticleAndInvalidSiteTargetKeepTheirCodes()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var slot = FindPairSlot(model);
        var forbidden = FindForbiddenParticle(model, slot.CenterMask);
        var unknownSet = new PairEnergySet(
            new string('0', 64),
            slot.CenterSiteId,
            slot.PartnerSiteId,
            slot.DistanceBucketFm,
            false,
            [new PairEnergyEntry(slot.Center, slot.Partner, new EnergyEv(1.0))]);
        var forbiddenSet = new PairEnergySet(
            slot.EnergyId,
            slot.CenterSiteId,
            slot.PartnerSiteId,
            slot.DistanceBucketFm,
            false,
            [new PairEnergyEntry(forbidden, forbidden, new EnergyEv(1.0))]);
        var created = CreateTemplate(project, model);
        var template = created with
        {
            StablePairSets =
            [
                .. created.StablePairSets.Select(set => set.EnergyId == slot.EnergyId
                    ? forbiddenSet
                    : set),
                unknownSet,
            ],
            UnstablePairSets = created.UnstablePairSets.Select(set => set.EnergyId == slot.EnergyId
                ? forbiddenSet
                : set).ToImmutableArray(),
            SiteEnergyOverrides =
            [
                new SiteEnergyOverride(
                    CellSiteId.Create(Guid.Parse("11111111-1111-7111-8111-111111111111")),
                    ParticleId.Create(Guid.Parse("22222222-2222-7222-8222-222222222222")),
                    new EnergyEv(1.0)),
            ],
        };

        var diagnostics = new ParameterizationService().Validate(project, model, template);

        Assert.Contains(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.UnknownReference);
        Assert.Contains(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.ForbiddenParticle);
        Assert.Contains(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget);
        Assert.All(
            diagnostics.Where(
                static item => item.Code is ParameterizationDiagnosticCodes.UnknownReference or
                    ParameterizationDiagnosticCodes.ForbiddenParticle or
                    ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget),
            static item => Assert.Equal(DiagnosticSeverity.Warning, item.Severity));
    }

    [Fact]
    public async Task ApplyLimitIsReportedByValidateAsAnError()
    {
        var (project, model) = await ParameterizationTestSupport.LoadAndCompileAsync(
            ParameterizationTestSupport.Ceria);
        var options = new ParameterizationOptions { MaxAppliedValueBytes = 1 };

        var diagnostics = new ParameterizationService().Validate(project, model, null, options);

        var limit = Assert.Single(diagnostics, static item => item.Code == ParameterizationDiagnosticCodes.LimitExceeded);
        Assert.Equal(DiagnosticSeverity.Error, limit.Severity);
        var applied = new ParameterizationService().Apply(project, model, null, options);
        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
    }

    private static void AssertApplyFailsWithStructuralError(
        ParameterizationService service,
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        string code)
    {
        var applied = service.Apply(project, model, template);

        Assert.False(applied.IsSuccess);
        Assert.Null(applied.Model);
        Assert.Contains(
            applied.Diagnostics,
            item => item.Code == code && item.Severity == DiagnosticSeverity.Error);
    }

    private static ParameterizationTemplate CreateTemplate(Project project, CompiledModel model) =>
        new ParameterizationFactory(new FixedTimeProvider(DateTimeOffset.UnixEpoch))
            .Create(project, model, new ParameterizationTemplateOptions { Name = "Validation" })
            .Template;

    private static Mocassin.Core.Project.Project ChangeCeriaCell(Mocassin.Core.Project.Project project)
    {
        var cell = project.Model.Structure.Cell;
        var edit = new ProjectEditor().SetCellParameters(
            project,
            cell with
            {
                A = new LengthAngstrom(cell.A.Value + 0.05),
                B = new LengthAngstrom(cell.B.Value + 0.05),
                C = new LengthAngstrom(cell.C.Value + 0.05),
            });
        Assert.True(edit.IsSuccess, string.Join(Environment.NewLine, edit.Diagnostics));
        return edit.Project;
    }

    private static PairSlot FindPairSlot(CompiledModel model)
    {
        foreach (var table in model.Energy.PairTables)
        {
            var geometry = ParameterizationTestSupport.Representative(model, table);
            var centerMask = ParameterizationTestSupport.SiteMask(model, geometry.CenterSiteIndex);
            var partnerMask = ParameterizationTestSupport.SiteMask(model, geometry.PartnerSiteIndex);
            var center = FirstParticleIndex(centerMask);
            var partner = FirstParticleIndex(partnerMask);
            if (center > 0 && partner > 0)
            {
                return new PairSlot(
                    table.EnergyId,
                    model.Particles.Particles[center].Id,
                    model.Particles.Particles[partner].Id,
                    geometry.CenterSiteId,
                    geometry.PartnerSiteId,
                    geometry.DistanceBucketFm,
                    centerMask,
                    partnerMask);
            }
        }

        throw new InvalidOperationException("No pair slot with admissible particles was found.");
    }

    private static ParticleId FindForbiddenParticle(CompiledModel model, ulong centerMask)
    {
        for (var index = 1; index < model.Particles.Count; index++)
        {
            if ((centerMask & (1UL << index)) == 0)
            {
                return model.Particles.Particles[index].Id;
            }
        }

        throw new InvalidOperationException("No forbidden particle was found.");
    }

    private static int FirstParticleIndex(ulong mask)
    {
        for (var index = 1; index < 64; index++)
        {
            if ((mask & (1UL << index)) != 0)
            {
                return index;
            }
        }

        return 0;
    }

    private static PairEnergySet WithUniformEntries(PairEnergySet set, double value) =>
        set with
        {
            Entries = [.. set.Entries.Select(entry => entry with { Energy = new EnergyEv(value) })],
        };

    private static (CompiledJumpCollection Collection, CompiledJumpRule Rule) FindKineticRule(CompiledModel model)
    {
        foreach (var collection in model.Transitions.Collections)
        {
            if (collection.Transition.Kind == TransitionKind.Kinetic)
            {
                return (collection, model.Transitions.Rules[collection.RuleIndices[0]]);
            }
        }

        throw new InvalidOperationException("No kinetic rule was found.");
    }

    private sealed record PairSlot(
        string EnergyId,
        ParticleId Center,
        ParticleId Partner,
        CellSiteId CenterSiteId,
        CellSiteId PartnerSiteId,
        int DistanceBucketFm,
        ulong CenterMask,
        ulong PartnerMask);
}
