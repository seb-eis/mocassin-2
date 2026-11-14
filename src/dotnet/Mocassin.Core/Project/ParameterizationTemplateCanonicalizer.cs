// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Units;

namespace Mocassin.Core.Project;

/// <summary>
/// Applies the canonical template order, the shared-chiral orientation fold, and signed-zero normalization
/// used by the project serializer.
/// </summary>
/// <remarks>
/// This is the authoritative write-time guarantee. Records are immutable but expose <c>init</c> properties, so
/// editor code can naturally produce out-of-order arrays through <c>with</c>; the serializer always normalizes
/// them before writing or hashing. Mirrored chiral entries with the same value collapse onto the smaller
/// particle-pair orientation; conflicting values are kept so the validator reports them. Null nested elements
/// are invalid user data and are removed rather than crashing the serializer;
/// <see cref="ParameterizationTemplateValidator"/> reports them.
/// </remarks>
internal static class ParameterizationTemplateCanonicalizer
{
    /// <summary>
    /// Returns a canonical copy of one template.
    /// </summary>
    /// <param name="template">The template to normalize.</param>
    /// <returns>The normalized template.</returns>
    public static ParameterizationTemplate Normalize(ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return template with
        {
            Name = template.Name ?? string.Empty,
            ModelSignature = template.ModelSignature ?? string.Empty,
            StablePairSets = NormalizePairSets(template.StablePairSets),
            UnstablePairSets = NormalizePairSets(template.UnstablePairSets),
            GroupSets = NormalizeGroupSets(template.GroupSets),
            SiteEnergyOverrides = NormalizeSiteEnergyOverrides(template.SiteEnergyOverrides),
            TransitionRuleSets = NormalizeTransitionRuleSets(template.TransitionRuleSets),
        };
    }

    private static ImmutableArray<PairEnergySet> NormalizePairSets(ImmutableArray<PairEnergySet> sets)
    {
        if (sets.IsDefault)
        {
            return [];
        }

        return
        [
            .. sets
                .Where(static set => set is not null)
                .OrderBy(static set => set.EnergyId ?? string.Empty, StringComparer.Ordinal)
                .Select(static set => set with
                {
                    EnergyId = set.EnergyId ?? string.Empty,
                    Entries = NormalizePairEntries(set.Entries, set.IsChiral),
                }),
        ];
    }

    private static ImmutableArray<PairEnergyEntry> NormalizePairEntries(
        ImmutableArray<PairEnergyEntry> entries,
        bool isChiral)
    {
        if (entries.IsDefault)
        {
            return [];
        }

        var normalized = entries
            .Where(static entry => entry is not null)
            .Select(static entry => entry with { Energy = Normalize(entry.Energy) })
            .ToArray();
        if (!isChiral)
        {
            return SortedPairEntries(normalized);
        }

        // A chiral pair has one shared value. Fold mirrored duplicates with the same value onto the smaller
        // (ParticleId, ParticleId) orientation so the canonical document depends only on semantic content.
        // Conflicting values are deliberately kept so ParameterizationTemplateValidator can report PRM014;
        // the canonicalizer never chooses between them.
        var canonical = new List<PairEnergyEntry>(normalized.Length);
        foreach (var group in normalized.GroupBy(static entry => PairOrientation(entry)))
        {
            var items = group
                .OrderBy(static entry => entry.CenterParticleId.Value)
                .ThenBy(static entry => entry.PartnerParticleId.Value)
                .ToArray();
            if (items.Length == 2 &&
                items[0].CenterParticleId != items[1].CenterParticleId &&
                items[0].Energy.Value == items[1].Energy.Value)
            {
                canonical.Add(items[0]);
                continue;
            }

            canonical.AddRange(items);
        }

        return SortedPairEntries(canonical);
    }

    private static ImmutableArray<PairEnergyEntry> SortedPairEntries(IEnumerable<PairEnergyEntry> entries) =>
    [
        .. entries
            .OrderBy(static entry => entry.CenterParticleId.Value)
            .ThenBy(static entry => entry.PartnerParticleId.Value),
    ];

    private static (Guid Center, Guid Partner) PairOrientation(PairEnergyEntry entry) =>
        entry.PartnerParticleId.Value.CompareTo(entry.CenterParticleId.Value) < 0
            ? (entry.PartnerParticleId.Value, entry.CenterParticleId.Value)
            : (entry.CenterParticleId.Value, entry.PartnerParticleId.Value);

    private static ImmutableArray<GroupEnergySet> NormalizeGroupSets(ImmutableArray<GroupEnergySet> sets)
    {
        if (sets.IsDefault)
        {
            return [];
        }

        return
        [
            .. sets
                .Where(static set => set is not null)
                .OrderBy(static set => set.ClusterEnergyId ?? string.Empty, StringComparer.Ordinal)
                .Select(static set => set with
                {
                    ClusterEnergyId = set.ClusterEnergyId ?? string.Empty,
                    Entries = NormalizeGroupEntries(set.Entries),
                }),
        ];
    }

    private static ImmutableArray<GroupEnergyEntry> NormalizeGroupEntries(ImmutableArray<GroupEnergyEntry> entries)
    {
        if (entries.IsDefault)
        {
            return [];
        }

        return
        [
            .. entries
                .Where(static entry => entry is not null)
                .OrderBy(
                    static entry => entry.OccupationParticleIds.IsDefault
                        ? ImmutableArray<ParticleId>.Empty
                        : entry.OccupationParticleIds,
                    OccupationComparer.Instance)
                .Select(static entry => entry with
                {
                    OccupationParticleIds = entry.OccupationParticleIds.IsDefault
                        ? ImmutableArray<ParticleId>.Empty
                        : entry.OccupationParticleIds,
                    Energy = Normalize(entry.Energy),
                }),
        ];
    }

    private static ImmutableArray<SiteEnergyOverride> NormalizeSiteEnergyOverrides(
        ImmutableArray<SiteEnergyOverride> overrides)
    {
        if (overrides.IsDefault)
        {
            return [];
        }

        return
        [
            .. overrides
                .Where(static entry => entry is not null)
                .OrderBy(static entry => entry.SiteId.Value)
                .ThenBy(static entry => entry.ParticleId.Value)
                .Select(static entry => entry with { Energy = Normalize(entry.Energy) }),
        ];
    }

    private static ImmutableArray<TransitionRuleSet> NormalizeTransitionRuleSets(
        ImmutableArray<TransitionRuleSet> sets)
    {
        if (sets.IsDefault)
        {
            return [];
        }

        return
        [
            .. sets
                .Where(static set => set is not null)
                .OrderBy(static set => set.TransitionKind)
                .ThenBy(static set => set.TransitionId)
                .Select(static set => set with
                {
                    Rules = NormalizeRuleEntries(set.Rules),
                }),
        ];
    }

    private static ImmutableArray<RuleFrequencyEntry> NormalizeRuleEntries(ImmutableArray<RuleFrequencyEntry> entries)
    {
        if (entries.IsDefault)
        {
            return [];
        }

        return
        [
            .. entries
                .Where(static entry => entry is not null)
                .OrderBy(static entry => entry.RuleGeometryId ?? string.Empty, StringComparer.Ordinal)
                .Select(static entry => entry with
                {
                    RuleGeometryId = entry.RuleGeometryId ?? string.Empty,
                    Frequency = Normalize(entry.Frequency),
                }),
        ];
    }

    private static EnergyEv Normalize(EnergyEv energy) => energy.Value == 0.0 ? new EnergyEv(0.0) : energy;

    private static FrequencyHz Normalize(FrequencyHz frequency) =>
        frequency.Value == 0.0 ? new FrequencyHz(0.0) : frequency;

    private sealed class OccupationComparer : IComparer<ImmutableArray<ParticleId>>
    {
        public static OccupationComparer Instance { get; } = new();

        public int Compare(ImmutableArray<ParticleId> x, ImmutableArray<ParticleId> y)
        {
            var left = x.IsDefault ? [] : x;
            var right = y.IsDefault ? [] : y;
            var commonLength = Math.Min(left.Length, right.Length);
            for (var index = 0; index < commonLength; index++)
            {
                var comparison = left[index].Value.CompareTo(right[index].Value);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
