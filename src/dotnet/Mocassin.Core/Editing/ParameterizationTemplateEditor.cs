// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Units;

namespace Mocassin.Core.Editing;

/// <summary>
/// Provides pure, immutable parameterization-template mutations for the library and the future editor UI.
/// </summary>
/// <remarks>
/// Every operation looks up stable keys in the template only. It never compiles or inspects a model, never
/// mutates its input, and returns the candidate template together with the structural diagnostics of that
/// candidate.
/// </remarks>
public static class ParameterizationTemplateEditor
{
    /// <summary>
    /// Renames a template.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="name">The new name.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult Rename(ParameterizationTemplate template, string name)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return Failure(
                template,
                ProjectDiagnosticCodes.InvalidMetadata,
                "Parameterization template name must not be empty.",
                "name");
        }

        return Result(template with { Name = name });
    }

    /// <summary>
    /// Sets one pair-energy value.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="energyId">The shared pair-energy identity.</param>
    /// <param name="centerParticleId">The centre particle.</param>
    /// <param name="partnerParticleId">The partner particle.</param>
    /// <param name="energy">The interaction energy.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult SetPairEnergy(
        ParameterizationTemplate template,
        string energyId,
        ParticleId centerParticleId,
        ParticleId partnerParticleId,
        EnergyEv energy)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(energyId);
        if (!ParameterizationTemplateValidator.IsValidStableId(energyId))
        {
            return InvalidStableId(template, "pair energy", energyId);
        }

        if (!TryFindPairSet(template, energyId, out var isStable, out var setIndex, out var set))
        {
            return MissingPairSet(template, energyId);
        }

        var (center, partner) = NormalizePairOrientation(set, centerParticleId, partnerParticleId);
        var entries = set.Entries.IsDefault ? ImmutableArray<PairEnergyEntry>.Empty : set.Entries;
        var entryIndex = FindPairEntryIndex(entries, center, partner, set.IsChiral);
        var replacement = new PairEnergyEntry(center, partner, energy);
        entries = entryIndex >= 0
            ? ReplaceAt(entries, entryIndex, replacement)
            : [.. entries, replacement];

        return Result(ReplacePairSet(template, isStable, setIndex, set with { Entries = entries }));
    }

    /// <summary>
    /// Clears one pair-energy value.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="energyId">The shared pair-energy identity.</param>
    /// <param name="centerParticleId">The centre particle.</param>
    /// <param name="partnerParticleId">The partner particle.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult ClearPairEnergy(
        ParameterizationTemplate template,
        string energyId,
        ParticleId centerParticleId,
        ParticleId partnerParticleId)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(energyId);
        if (!ParameterizationTemplateValidator.IsValidStableId(energyId))
        {
            return InvalidStableId(template, "pair energy", energyId);
        }

        if (!TryFindPairSet(template, energyId, out var isStable, out var setIndex, out var set))
        {
            return MissingPairSet(template, energyId);
        }

        var (center, partner) = NormalizePairOrientation(set, centerParticleId, partnerParticleId);
        var entries = set.Entries.IsDefault ? ImmutableArray<PairEnergyEntry>.Empty : set.Entries;
        var entryIndex = FindPairEntryIndex(entries, center, partner, set.IsChiral);
        return entryIndex < 0
            ? Result(template)
            : Result(ReplacePairSet(
                template,
                isStable,
                setIndex,
                set with { Entries = RemoveAt(entries, entryIndex) }));
    }

    /// <summary>
    /// Sets one group-energy value.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="clusterEnergyId">The shared cluster-energy identity.</param>
    /// <param name="occupationParticleIds">The occupation particles in site order.</param>
    /// <param name="energy">The interaction energy.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult SetGroupEnergy(
        ParameterizationTemplate template,
        string clusterEnergyId,
        ImmutableArray<ParticleId> occupationParticleIds,
        EnergyEv energy)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(clusterEnergyId);
        if (!ParameterizationTemplateValidator.IsValidStableId(clusterEnergyId))
        {
            return InvalidStableId(template, "cluster energy", clusterEnergyId);
        }

        if (!TryFindGroupSet(template, clusterEnergyId, out var setIndex, out var set))
        {
            return Failure(
                template,
                ProjectDiagnosticCodes.UnknownReference,
                $"Parameterization template does not contain a group-energy set with cluster ID '{clusterEnergyId}'.",
                $"groupSets/{clusterEnergyId}");
        }

        var occupation = occupationParticleIds.IsDefault
            ? ImmutableArray<ParticleId>.Empty
            : occupationParticleIds;
        var entries = set.Entries.IsDefault ? ImmutableArray<GroupEnergyEntry>.Empty : set.Entries;
        var entryIndex = FindGroupEntryIndex(entries, occupation);
        var replacement = new GroupEnergyEntry(occupation, energy);
        entries = entryIndex >= 0
            ? ReplaceAt(entries, entryIndex, replacement)
            : [.. entries, replacement];

        return Result(ReplaceGroupSet(template, setIndex, set with { Entries = entries }));
    }

    /// <summary>
    /// Clears one group-energy value.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="clusterEnergyId">The shared cluster-energy identity.</param>
    /// <param name="occupationParticleIds">The occupation particles in site order.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult ClearGroupEnergy(
        ParameterizationTemplate template,
        string clusterEnergyId,
        ImmutableArray<ParticleId> occupationParticleIds)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(clusterEnergyId);
        if (!ParameterizationTemplateValidator.IsValidStableId(clusterEnergyId))
        {
            return InvalidStableId(template, "cluster energy", clusterEnergyId);
        }

        if (!TryFindGroupSet(template, clusterEnergyId, out var setIndex, out var set))
        {
            return Failure(
                template,
                ProjectDiagnosticCodes.UnknownReference,
                $"Parameterization template does not contain a group-energy set with cluster ID '{clusterEnergyId}'.",
                $"groupSets/{clusterEnergyId}");
        }

        var occupation = occupationParticleIds.IsDefault
            ? ImmutableArray<ParticleId>.Empty
            : occupationParticleIds;
        var entries = set.Entries.IsDefault ? ImmutableArray<GroupEnergyEntry>.Empty : set.Entries;
        var entryIndex = FindGroupEntryIndex(entries, occupation);
        return entryIndex < 0
            ? Result(template)
            : Result(ReplaceGroupSet(
                template,
                setIndex,
                set with { Entries = RemoveAt(entries, entryIndex) }));
    }

    /// <summary>
    /// Sets one rule attempt frequency.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="transitionKind">The transition kind.</param>
    /// <param name="transitionId">The transition identifier.</param>
    /// <param name="ruleGeometryId">The stable rule geometry identity.</param>
    /// <param name="frequency">The absolute attempt frequency.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult SetRuleFrequency(
        ParameterizationTemplate template,
        TransitionKind transitionKind,
        Guid transitionId,
        string ruleGeometryId,
        FrequencyHz frequency)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(ruleGeometryId);
        if (transitionKind is not (TransitionKind.Kinetic or TransitionKind.Metropolis))
        {
            return Failure(
                template,
                ParameterizationDiagnosticCodes.InvalidKey,
                "A transition rule set has an unknown transition kind.",
                "transitionRuleSets");
        }

        if (!ParameterizationTemplateValidator.IsValidStableId(ruleGeometryId))
        {
            return InvalidStableId(template, "rule geometry", ruleGeometryId);
        }

        if (!TryFindRuleSet(template, transitionKind, transitionId, out var setIndex, out var set))
        {
            return Failure(
                template,
                ProjectDiagnosticCodes.UnknownReference,
                $"Parameterization template does not contain a transition rule set for {transitionKind} {transitionId:D}.",
                $"transitionRuleSets/{transitionKind}/{transitionId:D}");
        }

        var rules = set.Rules.IsDefault ? ImmutableArray<RuleFrequencyEntry>.Empty : set.Rules;
        var ruleIndex = FindRuleEntryIndex(rules, ruleGeometryId);
        var replacement = new RuleFrequencyEntry(ruleGeometryId, frequency);
        rules = ruleIndex >= 0
            ? ReplaceAt(rules, ruleIndex, replacement)
            : [.. rules, replacement];

        return Result(ReplaceRuleSet(template, setIndex, set with { Rules = rules }));
    }

    /// <summary>
    /// Clears one rule attempt frequency.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="transitionKind">The transition kind.</param>
    /// <param name="transitionId">The transition identifier.</param>
    /// <param name="ruleGeometryId">The stable rule geometry identity.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult ClearRuleFrequency(
        ParameterizationTemplate template,
        TransitionKind transitionKind,
        Guid transitionId,
        string ruleGeometryId)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(ruleGeometryId);
        if (transitionKind is not (TransitionKind.Kinetic or TransitionKind.Metropolis))
        {
            return Failure(
                template,
                ParameterizationDiagnosticCodes.InvalidKey,
                "A transition rule set has an unknown transition kind.",
                "transitionRuleSets");
        }

        if (!TryFindRuleSet(template, transitionKind, transitionId, out var setIndex, out var set))
        {
            return Failure(
                template,
                ProjectDiagnosticCodes.UnknownReference,
                $"Parameterization template does not contain a transition rule set for {transitionKind} {transitionId:D}.",
                $"transitionRuleSets/{transitionKind}/{transitionId:D}");
        }

        var rules = set.Rules.IsDefault ? ImmutableArray<RuleFrequencyEntry>.Empty : set.Rules;
        var ruleIndex = FindRuleEntryIndex(rules, ruleGeometryId);
        return ruleIndex < 0
            ? Result(template)
            : Result(ReplaceRuleSet(
                template,
                setIndex,
                set with { Rules = RemoveAt(rules, ruleIndex) }));
    }

    /// <summary>
    /// Sets one site-energy override.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="siteId">The reference site.</param>
    /// <param name="particleId">The occupying particle.</param>
    /// <param name="energy">The site energy.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult SetSiteEnergyOverride(
        ParameterizationTemplate template,
        CellSiteId siteId,
        ParticleId particleId,
        EnergyEv energy)
    {
        ArgumentNullException.ThrowIfNull(template);
        var overrides = template.SiteEnergyOverrides.IsDefault
            ? ImmutableArray<SiteEnergyOverride>.Empty
            : template.SiteEnergyOverrides;
        var index = FindSiteOverrideIndex(overrides, siteId, particleId);
        var replacement = new SiteEnergyOverride(siteId, particleId, energy);
        overrides = index >= 0
            ? ReplaceAt(overrides, index, replacement)
            : [.. overrides, replacement];
        return Result(template with { SiteEnergyOverrides = overrides });
    }

    /// <summary>
    /// Removes one site-energy override, restoring the authored model value.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <param name="siteId">The reference site.</param>
    /// <param name="particleId">The occupying particle.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult RemoveSiteEnergyOverride(
        ParameterizationTemplate template,
        CellSiteId siteId,
        ParticleId particleId)
    {
        ArgumentNullException.ThrowIfNull(template);
        var overrides = template.SiteEnergyOverrides.IsDefault
            ? ImmutableArray<SiteEnergyOverride>.Empty
            : template.SiteEnergyOverrides;
        var index = FindSiteOverrideIndex(overrides, siteId, particleId);
        return index < 0
            ? Result(template)
            : Result(template with { SiteEnergyOverrides = RemoveAt(overrides, index) });
    }

    /// <summary>
    /// Clears every explicit value while keeping the set headers and the template identity.
    /// </summary>
    /// <param name="template">The current template.</param>
    /// <returns>The edit result.</returns>
    public static ParameterizationTemplateEditResult ClearValues(ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var stable = (template.StablePairSets.IsDefault
                ? ImmutableArray<PairEnergySet>.Empty
                : template.StablePairSets)
            .Select(static set => set is null ? null! : set with { Entries = [] })
            .ToImmutableArray();
        var unstable = (template.UnstablePairSets.IsDefault
                ? ImmutableArray<PairEnergySet>.Empty
                : template.UnstablePairSets)
            .Select(static set => set is null ? null! : set with { Entries = [] })
            .ToImmutableArray();
        var groups = (template.GroupSets.IsDefault
                ? ImmutableArray<GroupEnergySet>.Empty
                : template.GroupSets)
            .Select(static set => set is null ? null! : set with { Entries = [] })
            .ToImmutableArray();
        var rules = (template.TransitionRuleSets.IsDefault
                ? ImmutableArray<TransitionRuleSet>.Empty
                : template.TransitionRuleSets)
            .Select(static set => set is null ? null! : set with { Rules = [] })
            .ToImmutableArray();

        return Result(template with
        {
            StablePairSets = stable,
            UnstablePairSets = unstable,
            GroupSets = groups,
            SiteEnergyOverrides = [],
            TransitionRuleSets = rules,
        });
    }

    private static ParameterizationTemplateEditResult Result(ParameterizationTemplate template) =>
        new(template, ParameterizationTemplateValidator.Validate(template));

    private static ParameterizationTemplateEditResult Failure(
        ParameterizationTemplate template,
        string code,
        string message,
        string property) =>
        new(
            template,
            [
                new Diagnostic(
                    code,
                    DiagnosticSeverity.Error,
                    message,
                    new ModelPath("parameterization", template.Id.Value, property)),
            ]);

    private static ParameterizationTemplateEditResult InvalidStableId(
        ParameterizationTemplate template,
        string kind,
        string id) =>
        Failure(
            template,
            ParameterizationDiagnosticCodes.InvalidKey,
            $"A {kind} ID must be 64 lowercase hexadecimal characters: '{id}'.",
            kind switch
            {
                "pair energy" => $"stablePairSets/{id}",
                "cluster energy" => $"groupSets/{id}",
                _ => $"transitionRuleSets/{id}",
            });

    private static ParameterizationTemplateEditResult MissingPairSet(
        ParameterizationTemplate template,
        string energyId) =>
        Failure(
            template,
            ProjectDiagnosticCodes.UnknownReference,
            $"Parameterization template does not contain a pair-energy set with ID '{energyId}'.",
            $"stablePairSets/{energyId}");

    private static bool TryFindPairSet(
        ParameterizationTemplate template,
        string energyId,
        out bool isStable,
        out int index,
        out PairEnergySet set)
    {
        if (TryFindPairSet(template.StablePairSets, energyId, out index, out set))
        {
            isStable = true;
            return true;
        }

        if (TryFindPairSet(template.UnstablePairSets, energyId, out index, out set))
        {
            isStable = false;
            return true;
        }

        isStable = true;
        index = -1;
        set = null!;
        return false;
    }

    private static bool TryFindPairSet(
        ImmutableArray<PairEnergySet> sets,
        string energyId,
        out int index,
        out PairEnergySet set)
    {
        if (!sets.IsDefault)
        {
            for (var candidate = 0; candidate < sets.Length; candidate++)
            {
                if (sets[candidate] is not null &&
                    string.Equals(sets[candidate].EnergyId, energyId, StringComparison.Ordinal))
                {
                    index = candidate;
                    set = sets[candidate];
                    return true;
                }
            }
        }

        index = -1;
        set = null!;
        return false;
    }

    private static ParameterizationTemplate ReplacePairSet(
        ParameterizationTemplate template,
        bool isStable,
        int index,
        PairEnergySet set)
    {
        var sets = isStable ? template.StablePairSets : template.UnstablePairSets;
        var builder = (sets.IsDefault ? ImmutableArray<PairEnergySet>.Empty : sets).ToBuilder();
        builder[index] = set;
        var updated = builder.ToImmutable();
        return isStable
            ? template with { StablePairSets = updated }
            : template with { UnstablePairSets = updated };
    }

    private static (ParticleId Center, ParticleId Partner) NormalizePairOrientation(
        PairEnergySet set,
        ParticleId center,
        ParticleId partner) =>
        set.IsChiral && partner.Value.CompareTo(center.Value) < 0
            ? (partner, center)
            : (center, partner);

    private static int FindPairEntryIndex(
        ImmutableArray<PairEnergyEntry> entries,
        ParticleId center,
        ParticleId partner,
        bool isChiral)
    {
        if (entries.IsDefault)
        {
            return -1;
        }

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            if (entry is not null &&
                entry.CenterParticleId == center &&
                entry.PartnerParticleId == partner)
            {
                return index;
            }

            if (isChiral &&
                entry is not null &&
                entry.CenterParticleId == partner &&
                entry.PartnerParticleId == center)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryFindGroupSet(
        ParameterizationTemplate template,
        string clusterEnergyId,
        out int index,
        out GroupEnergySet set)
    {
        var sets = template.GroupSets.IsDefault ? ImmutableArray<GroupEnergySet>.Empty : template.GroupSets;
        for (var candidate = 0; candidate < sets.Length; candidate++)
        {
            if (sets[candidate] is not null &&
                string.Equals(sets[candidate].ClusterEnergyId, clusterEnergyId, StringComparison.Ordinal))
            {
                index = candidate;
                set = sets[candidate];
                return true;
            }
        }

        index = -1;
        set = null!;
        return false;
    }

    private static ParameterizationTemplate ReplaceGroupSet(
        ParameterizationTemplate template,
        int index,
        GroupEnergySet set)
    {
        var sets = (template.GroupSets.IsDefault
                ? ImmutableArray<GroupEnergySet>.Empty
                : template.GroupSets)
            .ToBuilder();
        sets[index] = set;
        return template with { GroupSets = sets.ToImmutable() };
    }

    private static int FindGroupEntryIndex(
        ImmutableArray<GroupEnergyEntry> entries,
        ImmutableArray<ParticleId> occupation)
    {
        if (entries.IsDefault)
        {
            return -1;
        }

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            if (entry is null)
            {
                continue;
            }

            var entryOccupation = entry.OccupationParticleIds.IsDefault
                ? ImmutableArray<ParticleId>.Empty
                : entry.OccupationParticleIds;
            if (entryOccupation.SequenceEqual(occupation))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TryFindRuleSet(
        ParameterizationTemplate template,
        TransitionKind kind,
        Guid transitionId,
        out int index,
        out TransitionRuleSet set)
    {
        var sets = template.TransitionRuleSets.IsDefault
            ? ImmutableArray<TransitionRuleSet>.Empty
            : template.TransitionRuleSets;
        for (var candidate = 0; candidate < sets.Length; candidate++)
        {
            if (sets[candidate] is not null &&
                sets[candidate].TransitionKind == kind &&
                sets[candidate].TransitionId == transitionId)
            {
                index = candidate;
                set = sets[candidate];
                return true;
            }
        }

        index = -1;
        set = null!;
        return false;
    }

    private static ParameterizationTemplate ReplaceRuleSet(
        ParameterizationTemplate template,
        int index,
        TransitionRuleSet set)
    {
        var sets = (template.TransitionRuleSets.IsDefault
                ? ImmutableArray<TransitionRuleSet>.Empty
                : template.TransitionRuleSets)
            .ToBuilder();
        sets[index] = set;
        return template with { TransitionRuleSets = sets.ToImmutable() };
    }

    private static int FindRuleEntryIndex(ImmutableArray<RuleFrequencyEntry> rules, string ruleGeometryId)
    {
        if (rules.IsDefault)
        {
            return -1;
        }

        for (var index = 0; index < rules.Length; index++)
        {
            if (rules[index] is not null &&
                string.Equals(rules[index].RuleGeometryId, ruleGeometryId, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindSiteOverrideIndex(
        ImmutableArray<SiteEnergyOverride> overrides,
        CellSiteId siteId,
        ParticleId particleId)
    {
        if (overrides.IsDefault)
        {
            return -1;
        }

        for (var index = 0; index < overrides.Length; index++)
        {
            if (overrides[index] is not null &&
                overrides[index].SiteId == siteId &&
                overrides[index].ParticleId == particleId)
            {
                return index;
            }
        }

        return -1;
    }

    private static ImmutableArray<T> ReplaceAt<T>(ImmutableArray<T> values, int index, T value)
    {
        var builder = (values.IsDefault ? ImmutableArray<T>.Empty : values).ToBuilder();
        builder[index] = value;
        return builder.ToImmutable();
    }

    private static ImmutableArray<T> RemoveAt<T>(ImmutableArray<T> values, int index)
    {
        var builder = (values.IsDefault ? ImmutableArray<T>.Empty : values).ToBuilder();
        builder.RemoveAt(index);
        return builder.ToImmutable();
    }
}
