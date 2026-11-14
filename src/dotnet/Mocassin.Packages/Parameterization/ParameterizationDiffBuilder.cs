// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Compares a parameterization template against a compiled model by stable identity.
/// </summary>
/// <remarks>
/// The comparison is shared by the <c>PRM001</c> diff summary that <c>Apply</c> returns and, in a later Step 09
/// feature, by the explicit diff/rebase service. It never copies values and never mutates its inputs.
/// </remarks>
internal static class ParameterizationDiffBuilder
{
    private const char separator = '\u001f';

    /// <summary>
    /// Builds the diff for one template and compiled model.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The template.</param>
    /// <returns>The deterministic diff.</returns>
    public static ParameterizationDiff Build(CompiledModel model, ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);

        var descriptors = CompiledModelContract.DescribePairTables(model);
        var pairsByEnergyId = descriptors.ToDictionary(
            static descriptor => descriptor.EnergyId,
            static descriptor => descriptor,
            StringComparer.Ordinal);
        var clustersByEnergyId = model.Energy.ClusterTables.ToDictionary(
            static table => table.ClusterEnergyId,
            static table => table,
            StringComparer.Ordinal);
        var collections = model.Transitions.Collections.ToDictionary(
            static collection => (collection.Transition.Kind, collection.Transition.Id));
        var rulesByGeometryId = model.Transitions.Rules.ToDictionary(
            static rule => rule.RuleGeometryId,
            static rule => rule,
            StringComparer.Ordinal);

        var modelSlots = BuildModelSlots(model, descriptors);
        var modelSlotsByIdentity = modelSlots.ToDictionary(
            static slot => Identity(slot.Key),
            static slot => slot,
            StringComparer.Ordinal);

        var templateEntries = new List<TemplateEntry>();
        AddPairEntries(template, pairsByEnergyId, templateEntries);
        AddGroupEntries(model, template, clustersByEnergyId, templateEntries);
        AddRuleEntries(template, collections, rulesByGeometryId, templateEntries);
        AddSiteEntries(model, template, templateEntries);

        var matched = new Dictionary<string, ParameterizationDiffEntry>(StringComparer.Ordinal);
        var removed = new List<ParameterizationDiffEntry>();
        var removedLogical = new List<(ParameterizationDiffEntry Entry, string LogicalKey)>();
        foreach (var entry in templateEntries)
        {
            var identity = Identity(entry.Key);
            if (entry.DescriptorAgrees && modelSlotsByIdentity.TryGetValue(identity, out var slot))
            {
                matched[identity] = new ParameterizationDiffEntry(slot.Key, entry.Value, slot.Descriptor);
                continue;
            }

            var removedEntry = new ParameterizationDiffEntry(
                entry.Key,
                entry.Value,
                RemovedDescriptor(entry.Key, entry.Descriptor));
            removed.Add(removedEntry);
            removedLogical.Add((removedEntry, entry.LogicalKey));
        }

        var addedSlots = modelSlots
            .Where(slot => !matched.ContainsKey(Identity(slot.Key)))
            .ToArray();
        var added = addedSlots
            .Select(static slot => new ParameterizationDiffEntry(slot.Key, null, slot.Descriptor))
            .ToArray();

        var changes = BuildGeometryChanges(addedSlots, removedLogical);
        return new ParameterizationDiff(
            template.ModelSignature,
            model.ModelSignature,
            Sort(added),
            Sort(removed),
            Sort(matched.Values),
            [.. changes
                .OrderBy(static change => change.RemovedKey.Kind)
                .ThenBy(static change => Identity(change.RemovedKey), StringComparer.Ordinal)
                .ThenBy(static change => Identity(change.AddedKey), StringComparer.Ordinal)]);
    }

    private static List<ModelSlot> BuildModelSlots(
        CompiledModel model,
        ImmutableArray<PairTableDescriptor> descriptors)
    {
        var slots = new List<ModelSlot>();
        var particleIds = model.Particles.Particles.Select(static particle => particle.Id).ToArray();
        foreach (var descriptor in descriptors)
        {
            var kind = descriptor.Stability == SiteStability.Stable
                ? ParameterizationValueKind.StablePair
                : ParameterizationValueKind.UnstablePair;
            var descriptorText = PairDescriptor(descriptor);
            var logical = PairLogicalKey(descriptor.CenterSiteId, descriptor.PartnerSiteId, descriptor.IsChiral);
            foreach (var (center, partner) in EnumeratePairSlots(descriptor, model.Particles.Count))
            {
                var (first, second) = CanonicalPair(
                    particleIds[center],
                    particleIds[partner],
                    descriptor.IsChiral);
                var key = new ParameterizationValueKey(
                    kind,
                    descriptor.EnergyId,
                    null,
                    null,
                    PairEntryKey(first, second),
                    null,
                    null);
                slots.Add(new ModelSlot(key, descriptorText, logical));
            }
        }

        foreach (var table in model.Energy.ClusterTables)
        {
            var centerSiteId = model.Structure.Sites[table.CenterSiteIndex].Id;
            var descriptorText = GroupDescriptor(table.ClusterId, centerSiteId);
            var logical = GroupLogicalKey(table.ClusterId, centerSiteId);
            foreach (var occupation in table.Occupations)
            {
                var key = new ParameterizationValueKey(
                    ParameterizationValueKind.Group,
                    table.ClusterEnergyId,
                    null,
                    null,
                    GroupEntryKey(occupation.CanonicalTuple.Select(index => particleIds[index])),
                    null,
                    null);
                slots.Add(new ModelSlot(key, descriptorText, logical));
            }
        }

        foreach (var collection in model.Transitions.Collections)
        {
            if (collection.Transition.Kind != TransitionKind.Kinetic)
            {
                continue;
            }

            foreach (var ruleIndex in collection.RuleIndices)
            {
                var rule = model.Transitions.Rules[ruleIndex];
                var key = new ParameterizationValueKey(
                    ParameterizationValueKind.RuleFrequency,
                    collection.Transition.Id.ToString("D", CultureInfo.InvariantCulture),
                    collection.Transition.Kind,
                    collection.Transition.Id,
                    rule.RuleGeometryId,
                    null,
                    null);
                slots.Add(new ModelSlot(key, $"{collection.Transition.Kind} {collection.Transition.Id:D}", null));
            }
        }

        return slots;
    }

    private static IEnumerable<(int Center, int Partner)> EnumeratePairSlots(
        PairTableDescriptor descriptor,
        int particleCount)
    {
        if (!descriptor.IsChiral)
        {
            for (var center = 1; center < particleCount; center++)
            {
                if (!Contains(descriptor.CenterMask, center))
                {
                    continue;
                }

                for (var partner = 1; partner < particleCount; partner++)
                {
                    if (Contains(descriptor.PartnerMask, partner))
                    {
                        yield return (center, partner);
                    }
                }
            }

            yield break;
        }

        var seen = new HashSet<(int Center, int Partner)>();
        for (var center = 1; center < particleCount; center++)
        {
            if (!Contains(descriptor.CenterMask, center))
            {
                continue;
            }

            for (var partner = 1; partner < particleCount; partner++)
            {
                if (!Contains(descriptor.PartnerMask, partner))
                {
                    continue;
                }

                var pair = center <= partner ? (center, partner) : (partner, center);
                if (seen.Add(pair))
                {
                    yield return pair;
                }
            }
        }
    }

    private static void AddPairEntries(
        ParameterizationTemplate template,
        Dictionary<string, PairTableDescriptor> pairsByEnergyId,
        List<TemplateEntry> entries)
    {
        AddPairArray(template.StablePairSets, ParameterizationValueKind.StablePair);
        AddPairArray(template.UnstablePairSets, ParameterizationValueKind.UnstablePair);

        void AddPairArray(ImmutableArray<PairEnergySet> sets, ParameterizationValueKind kind)
        {
            if (sets.IsDefault)
            {
                return;
            }

            foreach (var set in sets)
            {
                if (set is null)
                {
                    continue;
                }

                var info = set.EnergyId is null
                    ? null
                    : new PairLogicalDescriptor(set.CenterSiteId, set.PartnerSiteId, set.IsChiral);

                var agrees = set.EnergyId is not null &&
                             pairsByEnergyId.TryGetValue(set.EnergyId, out var modelDescriptor) &&
                             modelDescriptor.CenterSiteId == set.CenterSiteId &&
                             modelDescriptor.PartnerSiteId == set.PartnerSiteId &&
                             modelDescriptor.DistanceBucketFm == set.DistanceBucketFm &&
                             modelDescriptor.IsChiral == set.IsChiral;
                var logical = info is null
                    ? string.Empty
                    : PairLogicalKey(info.CenterSiteId, info.PartnerSiteId, info.IsChiral);
                var descriptorText = info is null
                    ? null
                    : string.Create(CultureInfo.InvariantCulture, $"{info.CenterSiteId:D}/{info.PartnerSiteId:D}");
                var entriesArray = set.Entries.IsDefault ? ImmutableArray<PairEnergyEntry>.Empty : set.Entries;
                foreach (var entry in entriesArray)
                {
                    if (entry is null)
                    {
                        continue;
                    }

                    var (first, second) = CanonicalPair(
                        entry.CenterParticleId,
                        entry.PartnerParticleId,
                        set.IsChiral);
                    var key = new ParameterizationValueKey(
                        kind,
                        set.EnergyId,
                        null,
                        null,
                        PairEntryKey(first, second),
                        null,
                        null);
                    entries.Add(new TemplateEntry(key, entry.Energy.Value, agrees, descriptorText, logical));
                }
            }
        }
    }

    private static void AddGroupEntries(
        CompiledModel model,
        ParameterizationTemplate template,
        Dictionary<string, CompiledClusterTable> clustersByEnergyId,
        List<TemplateEntry> entries)
    {
        if (template.GroupSets.IsDefault)
        {
            return;
        }

        foreach (var set in template.GroupSets)
        {
            if (set is null)
            {
                continue;
            }

            var agrees = false;
            var centerSiteId = set.CenterSiteId;
            if (set.ClusterEnergyId is not null &&
                clustersByEnergyId.TryGetValue(set.ClusterEnergyId, out var table))
            {
                centerSiteId = model.Structure.Sites[table.CenterSiteIndex].Id;
                agrees = table.ClusterId == set.ClusterId && centerSiteId == set.CenterSiteId;
            }

            var descriptorText = GroupDescriptor(set.ClusterId, set.CenterSiteId);
            var logical = set.ClusterEnergyId is not null
                ? GroupLogicalKey(set.ClusterId, set.CenterSiteId)
                : string.Empty;
            var entriesArray = set.Entries.IsDefault ? ImmutableArray<GroupEnergyEntry>.Empty : set.Entries;
            foreach (var entry in entriesArray)
            {
                if (entry is null)
                {
                    continue;
                }

                var key = new ParameterizationValueKey(
                    ParameterizationValueKind.Group,
                    set.ClusterEnergyId,
                    null,
                    null,
                    GroupEntryKey(entry.OccupationParticleIds),
                    null,
                    null);
                entries.Add(new TemplateEntry(key, entry.Energy.Value, agrees, descriptorText, logical));
            }
        }
    }

    private static void AddRuleEntries(
        ParameterizationTemplate template,
        Dictionary<(TransitionKind Kind, Guid Id), CompiledJumpCollection> collections,
        Dictionary<string, CompiledJumpRule> rulesByGeometryId,
        List<TemplateEntry> entries)
    {
        if (template.TransitionRuleSets.IsDefault)
        {
            return;
        }

        foreach (var set in template.TransitionRuleSets)
        {
            if (set is null)
            {
                continue;
            }

            var collectionExists = collections.TryGetValue((set.TransitionKind, set.TransitionId), out var collection);
            var rules = set.Rules.IsDefault ? ImmutableArray<RuleFrequencyEntry>.Empty : set.Rules;
            foreach (var entry in rules)
            {
                if (entry is null || entry.RuleGeometryId is null)
                {
                    continue;
                }

                var agrees = collectionExists &&
                             collection is not null &&
                             rulesByGeometryId.TryGetValue(entry.RuleGeometryId, out var rule) &&
                             rule.CollectionIndex == collection.Index;
                var key = new ParameterizationValueKey(
                    ParameterizationValueKind.RuleFrequency,
                    set.TransitionId.ToString("D", CultureInfo.InvariantCulture),
                    set.TransitionKind,
                    set.TransitionId,
                    entry.RuleGeometryId,
                    null,
                    null);
                entries.Add(new TemplateEntry(
                    key,
                    entry.Frequency.Value,
                    agrees,
                    agrees ? $"{set.TransitionKind} {set.TransitionId:D}" : null,
                    string.Empty));
            }
        }
    }

    private static void AddSiteEntries(
        CompiledModel model,
        ParameterizationTemplate template,
        List<TemplateEntry> entries)
    {
        if (template.SiteEnergyOverrides.IsDefault)
        {
            return;
        }

        var siteIndices = new Dictionary<CellSiteId, int>(model.Structure.Sites.Length);
        for (var index = 0; index < model.Structure.Sites.Length; index++)
        {
            siteIndices[model.Structure.Sites[index].Id] = index;
        }

        foreach (var entry in template.SiteEnergyOverrides)
        {
            if (entry is null)
            {
                continue;
            }

            var agrees = siteIndices.TryGetValue(entry.SiteId, out var siteIndex) &&
                         model.Particles.TryGetIndex(entry.ParticleId, out var particleIndex) &&
                         Contains(CompiledModelContract.SiteMask(model.Structure, siteIndex), particleIndex);
            var key = new ParameterizationValueKey(
                ParameterizationValueKind.SiteEnergyOverride,
                null,
                null,
                null,
                null,
                entry.SiteId,
                entry.ParticleId);
            entries.Add(new TemplateEntry(
                key,
                entry.Energy.Value,
                agrees,
                $"{entry.SiteId:D}/{entry.ParticleId:D}",
                string.Empty));
        }
    }

    private static ImmutableArray<ParameterizationGeometryChange> BuildGeometryChanges(
        IReadOnlyList<ModelSlot> addedSlots,
        IReadOnlyList<(ParameterizationDiffEntry Entry, string LogicalKey)> removed)
    {
        var used = new HashSet<int>();
        var changes = ImmutableArray.CreateBuilder<ParameterizationGeometryChange>();
        foreach (var (entry, logical) in removed)
        {
            if (logical.Length == 0 ||
                entry.Key.Kind is not (ParameterizationValueKind.StablePair or
                    ParameterizationValueKind.UnstablePair or
                    ParameterizationValueKind.Group))
            {
                continue;
            }

            for (var index = 0; index < addedSlots.Count; index++)
            {
                if (used.Contains(index) || addedSlots[index].LogicalKey != logical)
                {
                    continue;
                }

                used.Add(index);
                changes.Add(new ParameterizationGeometryChange(
                    entry.Key.Kind,
                    entry.Key,
                    addedSlots[index].Key,
                    entry.Key.Kind == ParameterizationValueKind.Group
                        ? "cluster geometry changed but the logical descriptor is unchanged"
                        : "pair geometry changed but the site pair is unchanged"));
                break;
            }
        }

        return changes.ToImmutable();
    }

    private static ImmutableArray<ParameterizationDiffEntry> Sort(IEnumerable<ParameterizationDiffEntry> entries) =>
        [.. entries
            .OrderBy(static entry => entry.Key.Kind)
            .ThenBy(static entry => entry.Key.SetId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Key.TransitionKind)
            .ThenBy(static entry => entry.Key.TransitionId ?? Guid.Empty)
            .ThenBy(static entry => entry.Key.EntryId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Key.SiteId?.Value ?? Guid.Empty)
            .ThenBy(static entry => entry.Key.ParticleId?.Value ?? Guid.Empty)];

    private static string Identity(ParameterizationValueKey key) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)key.Kind}{separator}{key.SetId}{separator}{(key.TransitionKind is null ? string.Empty : ((int)key.TransitionKind.Value).ToString(CultureInfo.InvariantCulture))}{separator}{key.TransitionId?.ToString("D", CultureInfo.InvariantCulture)}{separator}{key.EntryId}{separator}{key.SiteId?.Value.ToString("D", CultureInfo.InvariantCulture)}{separator}{key.ParticleId?.Value.ToString("D", CultureInfo.InvariantCulture)}");

    private static (ParticleId First, ParticleId Second) CanonicalPair(
        ParticleId center,
        ParticleId partner,
        bool isChiral) =>
        isChiral && partner.Value.CompareTo(center.Value) < 0 ? (partner, center) : (center, partner);

    private static string PairEntryKey(ParticleId center, ParticleId partner) =>
        string.Create(CultureInfo.InvariantCulture, $"{center.Value:D}/{partner.Value:D}");

    private static string GroupEntryKey(IEnumerable<ParticleId> particles) =>
        string.Join(',', particles.Select(static particle => particle.Value.ToString("D", CultureInfo.InvariantCulture)));

    private static string PairDescriptor(PairTableDescriptor descriptor) =>
        $"{descriptor.CenterSiteId:D}/{descriptor.PartnerSiteId:D} {descriptor.DistanceBucketFm.ToString(CultureInfo.InvariantCulture)} fm";

    private static string GroupDescriptor(ClusterId clusterId, CellSiteId centerSiteId) =>
        $"{clusterId:D}/{centerSiteId:D}";

    private static string PairLogicalKey(CellSiteId center, CellSiteId partner, bool isChiral)
    {
        var low = center.Value.CompareTo(partner.Value) <= 0 ? center : partner;
        var high = low == center ? partner : center;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"pair{separator}{low.Value:D}{separator}{high.Value:D}{separator}{(isChiral ? "chiral" : "achiral")}");
    }

    private static string GroupLogicalKey(ClusterId clusterId, CellSiteId centerSiteId) =>
        string.Create(CultureInfo.InvariantCulture, $"group{separator}{clusterId.Value:D}{separator}{centerSiteId.Value:D}");

    private static bool Contains(ulong mask, int particleIndex) =>
        particleIndex > 0 && (mask & (1UL << particleIndex)) != 0;

    private static string RemovedDescriptor(ParameterizationValueKey key, string? descriptor) =>
        descriptor ??
        key.Kind switch
        {
            ParameterizationValueKind.SiteEnergyOverride =>
                $"{key.SiteId?.Value:D}/{key.ParticleId?.Value:D}",
            ParameterizationValueKind.RuleFrequency when key.TransitionId is not null =>
                $"{key.TransitionKind} {key.TransitionId:D}",
            _ => key.EntryId ?? string.Empty,
        };

    private sealed record PairLogicalDescriptor(CellSiteId CenterSiteId, CellSiteId PartnerSiteId, bool IsChiral);

    private sealed record ModelSlot(ParameterizationValueKey Key, string Descriptor, string? LogicalKey);

    private sealed record TemplateEntry(
        ParameterizationValueKey Key,
        double Value,
        bool DescriptorAgrees,
        string? Descriptor,
        string LogicalKey);
}
