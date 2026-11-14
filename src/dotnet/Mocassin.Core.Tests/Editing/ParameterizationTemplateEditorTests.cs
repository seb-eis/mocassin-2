// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationTemplateEditorTests
{
    [Fact]
    public void SetPairEnergyAddsAndReplacesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var added = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            ParameterizationTestModels.PairEnergyIdB,
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(0.35));

        Assert.True(added.IsSuccess);
        var addedSet = PairSet(added.Template, ParameterizationTestModels.PairEnergyIdB);
        Assert.Equal(2, addedSet.Entries.Length);
        Assert.Equal(new EnergyEv(0.35), addedSet.Entries.Single(static entry =>
            entry.CenterParticleId == ParameterizationTestModels.ParticleA).Energy);

        var replaced = ParameterizationTemplateEditor.SetPairEnergy(
            added.Template,
            ParameterizationTestModels.PairEnergyIdB,
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(0.45));

        Assert.True(replaced.IsSuccess);
        var replacedSet = PairSet(replaced.Template, ParameterizationTestModels.PairEnergyIdB);
        Assert.Equal(2, replacedSet.Entries.Length);
        Assert.Equal(
            new EnergyEv(0.45),
            replacedSet.Entries.Single(static entry =>
                entry.CenterParticleId == ParameterizationTestModels.ParticleA).Energy);
    }

    [Fact]
    public void SetPairEnergyStoresTheSmallerOrientationForAChiralSet()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            ParameterizationTestModels.PairEnergyIdC,
            ParameterizationTestModels.ParticleB,
            ParameterizationTestModels.ParticleA,
            new EnergyEv(0.4));

        Assert.True(result.IsSuccess);
        var entry = PairSet(result.Template, ParameterizationTestModels.PairEnergyIdC).Entries.Single();
        Assert.Equal(ParameterizationTestModels.ParticleA, entry.CenterParticleId);
        Assert.Equal(ParameterizationTestModels.ParticleB, entry.PartnerParticleId);
        Assert.Equal(new EnergyEv(0.4), entry.Energy);
    }

    [Fact]
    public void ClearPairEnergyRemovesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.ClearPairEnergy(
            template,
            ParameterizationTestModels.PairEnergyIdA,
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB);

        Assert.True(result.IsSuccess);
        var set = PairSet(result.Template, ParameterizationTestModels.PairEnergyIdA);
        Assert.Single(set.Entries);
        Assert.Equal(ParameterizationTestModels.ParticleB, set.Entries[0].CenterParticleId);
    }

    [Fact]
    public void SetGroupEnergyAddsAndReplacesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();
        ImmutableArray<ParticleId> occupation =
        [
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            ParameterizationTestModels.ParticleA,
        ];

        var added = ParameterizationTemplateEditor.SetGroupEnergy(
            template,
            ParameterizationTestModels.ClusterEnergyIdA,
            occupation,
            new EnergyEv(0.2));

        Assert.True(added.IsSuccess);
        var addedEntries = added.Template.GroupSets.Single().Entries;
        Assert.Equal(3, addedEntries.Length);
        Assert.Contains(addedEntries, static entry =>
            entry.OccupationParticleIds.SequenceEqual(
                new[]
                {
                    ParameterizationTestModels.ParticleA,
                    ParameterizationTestModels.ParticleB,
                    ParameterizationTestModels.ParticleA,
                }) &&
            entry.Energy == new EnergyEv(0.2));

        var replaced = ParameterizationTemplateEditor.SetGroupEnergy(
            added.Template,
            ParameterizationTestModels.ClusterEnergyIdA,
            occupation,
            new EnergyEv(0.25));

        Assert.True(replaced.IsSuccess);
        Assert.Equal(
            new EnergyEv(0.25),
            replaced.Template.GroupSets.Single().Entries.Single(static entry =>
                entry.OccupationParticleIds.Length == 3 &&
                entry.OccupationParticleIds[1] == ParameterizationTestModels.ParticleB).Energy);
    }

    [Fact]
    public void ClearGroupEnergyRemovesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.ClearGroupEnergy(
            template,
            ParameterizationTestModels.ClusterEnergyIdA,
            [
                ParameterizationTestModels.ParticleA,
                ParameterizationTestModels.ParticleA,
                ParameterizationTestModels.ParticleB,
            ]);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Template.GroupSets.Single().Entries);
    }

    [Fact]
    public void SetRuleFrequencyAddsAndReplacesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var replaced = ParameterizationTemplateEditor.SetRuleFrequency(
            template,
            TransitionKind.Kinetic,
            ParameterizationTestModels.TransitionAIdValue,
            ParameterizationTestModels.RuleGeometryIdA,
            new FrequencyHz(3.0e13));

        Assert.True(replaced.IsSuccess);
        var rule = replaced.Template.TransitionRuleSets
            .Single(static set => set.TransitionKind == TransitionKind.Kinetic)
            .Rules.Single(static candidate =>
                candidate.RuleGeometryId == ParameterizationTestModels.RuleGeometryIdA);
        Assert.Equal(new FrequencyHz(3.0e13), rule.Frequency);

        var added = ParameterizationTemplateEditor.SetRuleFrequency(
            replaced.Template,
            TransitionKind.Kinetic,
            ParameterizationTestModels.TransitionAIdValue,
            "9999999999999999999999999999999999999999999999999999999999999999",
            new FrequencyHz(4.0e13));

        Assert.True(added.IsSuccess);
        Assert.Equal(
            3,
            added.Template.TransitionRuleSets.Single(static set =>
                set.TransitionKind == TransitionKind.Kinetic).Rules.Length);
    }

    [Fact]
    public void ClearRuleFrequencyRemovesOneValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.ClearRuleFrequency(
            template,
            TransitionKind.Kinetic,
            ParameterizationTestModels.TransitionAIdValue,
            ParameterizationTestModels.RuleGeometryIdA);

        Assert.True(result.IsSuccess);
        Assert.Single(
            result.Template.TransitionRuleSets.Single(static set =>
                set.TransitionKind == TransitionKind.Kinetic).Rules);
    }

    [Fact]
    public void SetAndRemoveSiteEnergyOverrideRestoresTheAuthoredValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var added = ParameterizationTemplateEditor.SetSiteEnergyOverride(
            template,
            ParameterizationTestModels.SiteA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(0.5));

        Assert.True(added.IsSuccess);
        Assert.Equal(3, added.Template.SiteEnergyOverrides.Length);

        var removed = ParameterizationTemplateEditor.RemoveSiteEnergyOverride(
            added.Template,
            ParameterizationTestModels.SiteA,
            ParameterizationTestModels.ParticleB);

        Assert.True(removed.IsSuccess);
        Assert.Equal(2, removed.Template.SiteEnergyOverrides.Length);
    }

    [Fact]
    public void ClearValuesKeepsSetHeadersAndRemovesEveryValue()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.ClearValues(template);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Template.StablePairSets.Length);
        Assert.Single(result.Template.UnstablePairSets);
        Assert.Single(result.Template.GroupSets);
        Assert.Equal(2, result.Template.TransitionRuleSets.Length);
        Assert.Empty(result.Template.SiteEnergyOverrides);
        Assert.All(result.Template.StablePairSets, static set => Assert.Empty(set.Entries));
        Assert.All(result.Template.UnstablePairSets, static set => Assert.Empty(set.Entries));
        Assert.All(result.Template.GroupSets, static set => Assert.Empty(set.Entries));
        Assert.All(result.Template.TransitionRuleSets, static set => Assert.Empty(set.Rules));
    }

    [Fact]
    public void MissingSetReturnsUnknownReferenceWithoutChangingTheTemplate()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            "9999999999999999999999999999999999999999999999999999999999999999",
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(0.1));

        Assert.False(result.IsSuccess);
        Assert.Same(template, result.Template);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void MalformedKeyReturnsPrm014()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();

        var result = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            "not-a-stable-key",
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(0.1));

        Assert.False(result.IsSuccess);
        Assert.Same(template, result.Template);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void OutOfRangeEnergyReturnsPrm004()
    {
        var result = ParameterizationTemplateEditor.SetPairEnergy(
            ParameterizationTestModels.CreatePopulatedTemplate(),
            ParameterizationTestModels.PairEnergyIdA,
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(101.0));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidEnergy);
    }

    [Fact]
    public void ZeroFrequencyReturnsPrm005()
    {
        var result = ParameterizationTemplateEditor.SetRuleFrequency(
            ParameterizationTestModels.CreatePopulatedTemplate(),
            TransitionKind.Kinetic,
            ParameterizationTestModels.TransitionAIdValue,
            ParameterizationTestModels.RuleGeometryIdA,
            new FrequencyHz(0.0));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidFrequency);
    }

    [Fact]
    public void RenameRejectsAnEmptyName()
    {
        var result = ParameterizationTemplateEditor.Rename(
            ParameterizationTestModels.CreatePopulatedTemplate(),
            "  ");

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidMetadata);
    }

    [Fact]
    public void RenameRejectsAnOverlongName()
    {
        var result = ParameterizationTemplateEditor.Rename(
            ParameterizationTestModels.CreatePopulatedTemplate(),
            new string('a', ParameterizationLimits.TemplateNameLength + 1));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.LimitExceeded);
    }

    [Fact]
    public void EditorNeverMutatesItsInput()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();
        var before = ProjectCanonicalizer.ToCanonicalJson(
            ParameterizationTestModels.CreateProject(template));

        _ = ParameterizationTemplateEditor.SetPairEnergy(
            template,
            ParameterizationTestModels.PairEnergyIdA,
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            new EnergyEv(2.0));
        _ = ParameterizationTemplateEditor.ClearValues(template);
        _ = ParameterizationTemplateEditor.Rename(template, "Renamed");

        var after = ProjectCanonicalizer.ToCanonicalJson(
            ParameterizationTestModels.CreateProject(template));
        Assert.Equal(before, after);
    }

    private static PairEnergySet PairSet(ParameterizationTemplate template, string energyId) =>
        template.StablePairSets.Single(set => set.EnergyId == energyId);
}
