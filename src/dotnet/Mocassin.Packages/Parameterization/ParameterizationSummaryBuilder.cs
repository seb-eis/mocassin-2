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
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Computes the required, explicit, and effective value metrics of one project, compiled model, and template
/// combination.
/// </summary>
/// <remarks>
/// The builder is pure and allocates only scalar counters, a small frequency array, and lookup maps. It does not
/// allocate the dense applied-value arrays and does not validate the template; those belong to the
/// parameterization service.
/// </remarks>
internal static class ParameterizationSummaryBuilder
{
    /// <summary>
    /// Validates the compiled model and builds a summary.
    /// </summary>
    /// <param name="project">The project snapshot that produced the compiled model.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The optional explicit template; <see langword="null"/> means the implicit default.</param>
    /// <returns>The summary.</returns>
    public static ParameterizationSummary Build(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        CompiledModelContract.Validate(model);
        return BuildCore(project, model, template, CompiledModelContract.DescribePairTables(model));
    }

    /// <summary>
    /// Builds a summary from an already validated model and descriptor list.
    /// </summary>
    /// <param name="project">The project snapshot that produced the compiled model.</param>
    /// <param name="model">The compiled model.</param>
    /// <param name="template">The optional explicit template.</param>
    /// <param name="descriptors">The pair-table descriptors.</param>
    /// <returns>The summary.</returns>
    public static ParameterizationSummary BuildCore(
        Project project,
        CompiledModel model,
        ParameterizationTemplate? template,
        ImmutableArray<PairTableDescriptor> descriptors)
    {
        var stableSets = ValidSets(template?.StablePairSets ?? default);
        var unstableSets = ValidSets(template?.UnstablePairSets ?? default);
        var groupSets = ValidGroupSets(template?.GroupSets ?? default);
        var overrides = ValidOverrides(template?.SiteEnergyOverrides ?? default);
        var ruleSets = ValidRuleSets(template?.TransitionRuleSets ?? default);

        var pairMetrics = PairMetrics.Build(model, descriptors, stableSets, unstableSets);
        var groupMetrics = GroupMetrics.Build(model, groupSets);
        var ruleMetrics = RuleMetrics.Build(model, ruleSets);
        var siteMetrics = SiteMetrics.Build(project, model, overrides);

        var explicitPairValues = pairMetrics.ExplicitCount;
        var explicitGroupValues = groupMetrics.ExplicitCount;
        var explicitRuleValues = ruleMetrics.ExplicitCount;
        var explicitSiteValues = overrides.Length;
        var hasExplicitValues = explicitPairValues > 0 ||
                                explicitGroupValues > 0 ||
                                explicitRuleValues > 0 ||
                                explicitSiteValues > 0;

        var requiredSlots = CheckedInt(
            (long)pairMetrics.SlotCount +
            groupMetrics.SlotCount +
            ruleMetrics.KineticSlotCount,
            "required value slot");
        var setValues = CheckedInt(
            (long)explicitPairValues + explicitGroupValues + explicitRuleValues,
            "explicit value");
        var unsetValues = Math.Max(0, requiredSlots - setValues);
        var nonDefaultValues = CheckedInt(
            (long)pairMetrics.NonDefaultCount +
            groupMetrics.NonDefaultCount +
            ruleMetrics.NonDefaultCount +
            siteMetrics.NonDefaultCount,
            "non-default value");

        var energies = pairMetrics.EnergyRange;
        energies.Add(groupMetrics.EnergyRange);
        energies.Add(siteMetrics.EnergyRange);

        return new ParameterizationSummary(
            pairMetrics.StableCount,
            pairMetrics.UnstableCount,
            pairMetrics.SlotCount,
            explicitPairValues,
            groupMetrics.TableCount,
            groupMetrics.SlotCount,
            explicitGroupValues,
            ruleMetrics.KineticCollectionCount,
            ruleMetrics.KineticSlotCount,
            explicitRuleValues,
            siteMetrics.SlotCount,
            explicitSiteValues,
            pairMetrics.ChiralCount,
            requiredSlots,
            setValues,
            unsetValues,
            nonDefaultValues,
            energies.HasValue ? energies.Minimum : null,
            energies.HasValue ? energies.Maximum : null,
            ruleMetrics.MinimumFrequency,
            ruleMetrics.MedianFrequency,
            ruleMetrics.MaximumFrequency,
            EstimatedBytes(model),
            IdenticalShare(
                pairMetrics.NonDefaultValueCounts,
                groupMetrics.NonDefaultValueCounts,
                explicitPairValues + explicitGroupValues),
            ruleMetrics.NonParameterizableCollectionCount,
            hasExplicitValues);
    }

    private static double IdenticalShare(
        IReadOnlyDictionary<double, int> pairValueCounts,
        IReadOnlyDictionary<double, int> groupValueCounts,
        int totalExplicitPairAndGroupValues)
    {
        if (totalExplicitPairAndGroupValues == 0 ||
            (pairValueCounts.Count == 0 && groupValueCounts.Count == 0))
        {
            return 0.0;
        }

        var maximum = 0;
        foreach (var count in pairValueCounts.Values)
        {
            maximum = Math.Max(maximum, count);
        }

        foreach (var count in groupValueCounts.Values)
        {
            maximum = Math.Max(maximum, count);
        }

        return (double)maximum / totalExplicitPairAndGroupValues;
    }

    private static long EstimatedBytes(CompiledModel model)
    {
        long bytes = 0;
        foreach (var table in model.Energy.PairTables)
        {
            bytes = checked(bytes + ((long)table.ParticleCount * table.ParticleCount * sizeof(double)));
        }

        foreach (var table in model.Energy.ClusterTables)
        {
            bytes = checked(bytes + ((long)table.OccupationCount * sizeof(double)));
        }

        bytes = checked(
            bytes +
            ((long)model.Structure.PositionCount * model.Particles.Count * sizeof(double)) +
            ((long)model.Transitions.RuleCount * sizeof(double)));
        return bytes;
    }

    private static ImmutableArray<PairEnergySet> ValidSets(ImmutableArray<PairEnergySet> sets) =>
        sets.IsDefault ? [] : [.. sets.Where(static set => set is not null)];

    private static ImmutableArray<GroupEnergySet> ValidGroupSets(ImmutableArray<GroupEnergySet> sets) =>
        sets.IsDefault ? [] : [.. sets.Where(static set => set is not null)];

    private static ImmutableArray<SiteEnergyOverride> ValidOverrides(ImmutableArray<SiteEnergyOverride> overrides) =>
        overrides.IsDefault ? [] : [.. overrides.Where(static entry => entry is not null)];

    private static ImmutableArray<TransitionRuleSet> ValidRuleSets(ImmutableArray<TransitionRuleSet> sets) =>
        sets.IsDefault ? [] : [.. sets.Where(static set => set is not null)];

    private static int CheckedInt(long value, string description)
    {
        if (value is < int.MinValue or > int.MaxValue)
        {
            throw new InvalidOperationException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The compiled-model {description} count {value} exceeds the supported integer range."));
        }

        return (int)value;
    }

    private static int Length<T>(ImmutableArray<T> values) => values.IsDefault ? 0 : values.Length;

    private static bool Contains(ulong mask, int particleIndex) =>
        particleIndex > 0 && (mask & (1UL << particleIndex)) != 0;

    private struct EnergyRange
    {
        public bool HasValue;
        public double Minimum;
        public double Maximum;

        public void Add(double value)
        {
            if (!HasValue)
            {
                HasValue = true;
                Minimum = value;
                Maximum = value;
                return;
            }

            if (value < Minimum)
            {
                Minimum = value;
            }

            if (value > Maximum)
            {
                Maximum = value;
            }
        }

        public void Add(EnergyRange other)
        {
            if (!other.HasValue)
            {
                return;
            }

            Add(other.Minimum);
            Add(other.Maximum);
        }
    }

    private sealed record PairMetrics(
        int StableCount,
        int UnstableCount,
        int SlotCount,
        int ChiralCount,
        int ExplicitCount,
        int NonDefaultCount,
        EnergyRange EnergyRange,
        IReadOnlyDictionary<double, int> NonDefaultValueCounts)
    {
        public static PairMetrics Build(
            CompiledModel model,
            ImmutableArray<PairTableDescriptor> descriptors,
            ImmutableArray<PairEnergySet> stableSets,
            ImmutableArray<PairEnergySet> unstableSets)
        {
            var stableCount = 0;
            var unstableCount = 0;
            var chiralCount = 0;
            long slots = 0;
            var byId = new Dictionary<string, PairTableDescriptor>(descriptors.Length, StringComparer.Ordinal);
            foreach (var descriptor in descriptors)
            {
                byId[descriptor.EnergyId] = descriptor;
                slots += descriptor.RequiredSlotCount;
                if (descriptor.Stability == SiteStability.Stable)
                {
                    stableCount++;
                }
                else
                {
                    unstableCount++;
                }

                if (descriptor.IsChiral)
                {
                    chiralCount++;
                }
            }

            var range = new EnergyRange();
            if (model.Energy.PairTableCount > 0)
            {
                range.Add(0.0);
            }

            var explicitCount = 0;
            var nonDefaultCount = 0;
            var valueCounts = new Dictionary<double, int>();
            foreach (var set in stableSets)
            {
                Accumulate(model, byId, set, ref explicitCount, ref nonDefaultCount, valueCounts, ref range);
            }

            foreach (var set in unstableSets)
            {
                Accumulate(model, byId, set, ref explicitCount, ref nonDefaultCount, valueCounts, ref range);
            }

            return new PairMetrics(
                stableCount,
                unstableCount,
                CheckedInt(slots, "pair slot"),
                chiralCount,
                explicitCount,
                nonDefaultCount,
                range,
                valueCounts);
        }

        private static void Accumulate(
            CompiledModel model,
            Dictionary<string, PairTableDescriptor> byId,
            PairEnergySet set,
            ref int explicitCount,
            ref int nonDefaultCount,
            Dictionary<double, int> valueCounts,
            ref EnergyRange range)
        {
            var entries = set.Entries;
            if (entries.IsDefault)
            {
                return;
            }

            explicitCount += entries.Length;
            PairTableDescriptor? descriptor = null;
            if (set.EnergyId is not null)
            {
                byId.TryGetValue(set.EnergyId, out descriptor);
            }

            foreach (var entry in entries)
            {
                if (entry is null)
                {
                    continue;
                }

                var value = entry.Energy.Value;
                if (value != 0.0)
                {
                    nonDefaultCount++;
                    valueCounts[value] = valueCounts.GetValueOrDefault(value) + 1;
                }

                if (descriptor is null ||
                    !model.Particles.TryGetIndex(entry.CenterParticleId, out var center) ||
                    !model.Particles.TryGetIndex(entry.PartnerParticleId, out var partner))
                {
                    continue;
                }

                var ordered = Contains(descriptor.CenterMask, center) && Contains(descriptor.PartnerMask, partner);
                var mirrored = descriptor.IsChiral &&
                               Contains(descriptor.CenterMask, partner) &&
                               Contains(descriptor.PartnerMask, center);
                if (ordered || mirrored)
                {
                    range.Add(value);
                }
            }
        }
    }

    private sealed record GroupMetrics(
        int TableCount,
        int SlotCount,
        int ExplicitCount,
        int NonDefaultCount,
        EnergyRange EnergyRange,
        IReadOnlyDictionary<double, int> NonDefaultValueCounts)
    {
        public static GroupMetrics Build(CompiledModel model, ImmutableArray<GroupEnergySet> sets)
        {
            var tables = model.Energy.ClusterTables;
            var byId = new Dictionary<string, (CompiledClusterTable Table, Dictionary<string, int> Occupations)>(
                tables.Length,
                StringComparer.Ordinal);
            long slots = 0;
            foreach (var table in tables)
            {
                slots += table.OccupationCount;
                var occupations = new Dictionary<string, int>(table.OccupationCount, StringComparer.Ordinal);
                for (var index = 0; index < table.Occupations.Length; index++)
                {
                    occupations[OccupationKey(table.Occupations[index].CanonicalTuple)] = index;
                }

                byId[table.ClusterEnergyId] = (table, occupations);
            }

            var range = new EnergyRange();
            if (slots > 0)
            {
                range.Add(0.0);
            }

            var explicitCount = 0;
            var nonDefaultCount = 0;
            var valueCounts = new Dictionary<double, int>();
            foreach (var set in sets)
            {
                var entries = set.Entries;
                if (entries.IsDefault)
                {
                    continue;
                }

                explicitCount += entries.Length;
                Dictionary<string, int>? occupations = null;
                if (set.ClusterEnergyId is not null && byId.TryGetValue(set.ClusterEnergyId, out var target))
                {
                    occupations = target.Occupations;
                }

                foreach (var entry in entries)
                {
                    if (entry is null)
                    {
                        continue;
                    }

                    var value = entry.Energy.Value;
                    if (value != 0.0)
                    {
                        nonDefaultCount++;
                        valueCounts[value] = valueCounts.GetValueOrDefault(value) + 1;
                    }

                    if (occupations is null)
                    {
                        continue;
                    }

                    var indices = new int[entry.OccupationParticleIds.IsDefault ? 0 : entry.OccupationParticleIds.Length];
                    var valid = true;
                    for (var item = 0; item < indices.Length; item++)
                    {
                        if (!model.Particles.TryGetIndex(entry.OccupationParticleIds[item], out indices[item]))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid && occupations.ContainsKey(OccupationKey(indices)))
                    {
                        range.Add(value);
                    }
                }
            }

            return new GroupMetrics(
                tables.Length,
                CheckedInt(slots, "group slot"),
                explicitCount,
                nonDefaultCount,
                range,
                valueCounts);
        }

        private static string OccupationKey(ImmutableArray<int> tuple) =>
            tuple.IsDefault ? string.Empty : string.Join('/', tuple);

        private static string OccupationKey(int[] tuple) => string.Join('/', tuple);
    }

    private sealed record RuleMetrics(
        int KineticCollectionCount,
        int KineticSlotCount,
        int ExplicitCount,
        int NonDefaultCount,
        int NonParameterizableCollectionCount,
        double? MinimumFrequency,
        double? MedianFrequency,
        double? MaximumFrequency)
    {
        public static RuleMetrics Build(CompiledModel model, ImmutableArray<TransitionRuleSet> sets)
        {
            var collections = model.Transitions.Collections;
            var kineticRuleIds = new HashSet<string>(StringComparer.Ordinal);
            var kineticCollectionCount = 0;
            var nonParameterizableCount = 0;
            var kineticSlots = 0;
            foreach (var collection in collections)
            {
                if (collection.Transition.Kind == TransitionKind.Kinetic)
                {
                    kineticCollectionCount++;
                    kineticSlots += Length(collection.RuleIndices);
                    foreach (var ruleIndex in collection.RuleIndices)
                    {
                        kineticRuleIds.Add(model.Transitions.Rules[ruleIndex].RuleGeometryId);
                    }
                }
                else
                {
                    nonParameterizableCount++;
                }
            }

            var frequencies = new double[model.Transitions.RuleCount];
            for (var index = 0; index < frequencies.Length; index++)
            {
                frequencies[index] = model.Transitions.Rules[index].FrequencyHz;
            }

            var ruleByGeometryId = new Dictionary<string, int>(
                model.Transitions.RuleCount,
                StringComparer.Ordinal);
            for (var index = 0; index < model.Transitions.Rules.Length; index++)
            {
                ruleByGeometryId[model.Transitions.Rules[index].RuleGeometryId] = index;
            }

            var explicitCount = 0;
            var nonDefaultCount = 0;
            foreach (var set in sets)
            {
                var entries = set.Rules;
                if (entries.IsDefault)
                {
                    continue;
                }

                explicitCount += entries.Length;
                var kineticSet = set.TransitionKind == TransitionKind.Kinetic;
                foreach (var entry in entries)
                {
                    if (entry is null)
                    {
                        continue;
                    }

                    var value = entry.Frequency.Value;
                    if (entry.RuleGeometryId is not null &&
                        ruleByGeometryId.TryGetValue(entry.RuleGeometryId, out var ruleIndex))
                    {
                        if (value != model.Transitions.Rules[ruleIndex].FrequencyHz)
                        {
                            nonDefaultCount++;
                        }

                        if (kineticSet && kineticRuleIds.Contains(entry.RuleGeometryId))
                        {
                            frequencies[ruleIndex] = value;
                        }
                    }
                    else if (value != CompiledJumpRule.DefaultFrequencyHz)
                    {
                        nonDefaultCount++;
                    }
                }
            }

            if (frequencies.Length == 0)
            {
                return new RuleMetrics(
                    kineticCollectionCount,
                    CheckedInt(kineticSlots, "kinetic rule slot"),
                    explicitCount,
                    nonDefaultCount,
                    nonParameterizableCount,
                    null,
                    null,
                    null);
            }

            Array.Sort(frequencies);
            var middle = frequencies.Length / 2;
            var median = frequencies.Length % 2 == 1
                ? frequencies[middle]
                : 0.5 * (frequencies[middle - 1] + frequencies[middle]);
            return new RuleMetrics(
                kineticCollectionCount,
                CheckedInt(kineticSlots, "kinetic rule slot"),
                explicitCount,
                nonDefaultCount,
                nonParameterizableCount,
                frequencies[0],
                median,
                frequencies[^1]);
        }
    }

    private sealed record SiteMetrics(int SlotCount, int NonDefaultCount, EnergyRange EnergyRange)
    {
        public static SiteMetrics Build(
            Project project,
            CompiledModel model,
            ImmutableArray<SiteEnergyOverride> overrides)
        {
            var authored = new Dictionary<CellSiteId, Dictionary<ParticleId, double>>();
            var siteEnergies = project.Model.SiteEnergies;
            if (!siteEnergies.IsDefault)
            {
                foreach (var set in siteEnergies)
                {
                    if (set is null)
                    {
                        continue;
                    }

                    if (!authored.TryGetValue(set.SiteId, out var entries))
                    {
                        entries = [];
                        authored[set.SiteId] = entries;
                    }

                    if (set.Entries.IsDefault)
                    {
                        continue;
                    }

                    foreach (var entry in set.Entries)
                    {
                        if (entry is not null)
                        {
                            entries.TryAdd(entry.ParticleId, entry.Energy.Value);
                        }
                    }
                }
            }

            var bySite = new Dictionary<CellSiteId, Dictionary<ParticleId, double>>();
            foreach (var entry in overrides)
            {
                if (!bySite.TryGetValue(entry.SiteId, out var entries))
                {
                    entries = [];
                    bySite[entry.SiteId] = entries;
                }

                entries.TryAdd(entry.ParticleId, entry.Energy.Value);
            }

            var siteIndexById = new Dictionary<CellSiteId, int>(model.Structure.Sites.Length);
            for (var index = 0; index < model.Structure.Sites.Length; index++)
            {
                siteIndexById[model.Structure.Sites[index].Id] = index;
            }

            long slots = 0;
            var range = new EnergyRange();
            if (((long)model.Structure.PositionCount * model.Particles.Count) > 0)
            {
                range.Add(0.0);
            }

            var nonDefaultCount = 0;
            foreach (var site in model.Structure.Sites)
            {
                var mask = CompiledModelContract.SiteMask(model.Structure, site.Index);
                slots += CompiledModelContract.PopCount(mask);
                var hasSiteAuthored = authored.TryGetValue(site.Id, out var siteAuthored);
                var hasSiteOverrides = bySite.TryGetValue(site.Id, out var siteOverrides);
                if (!hasSiteAuthored && !hasSiteOverrides)
                {
                    continue;
                }

                for (var particleIndex = 0; particleIndex < model.Particles.Count; particleIndex++)
                {
                    if (!Contains(mask, particleIndex))
                    {
                        continue;
                    }

                    var particleId = model.Particles.Particles[particleIndex].Id;
                    double authoredValue = 0.0;
                    var hasAuthored = hasSiteAuthored && siteAuthored!.TryGetValue(particleId, out authoredValue);
                    double overrideValue = 0.0;
                    var hasOverride = hasSiteOverrides && siteOverrides!.TryGetValue(particleId, out overrideValue);
                    if (hasOverride)
                    {
                        range.Add(overrideValue);
                        if (!hasAuthored || authoredValue != overrideValue)
                        {
                            nonDefaultCount++;
                        }
                    }
                    else if (hasAuthored)
                    {
                        range.Add(authoredValue);
                    }
                }
            }

            foreach (var entry in overrides)
            {
                if (!siteIndexById.TryGetValue(entry.SiteId, out var siteIndex) ||
                    !model.Particles.TryGetIndex(entry.ParticleId, out var particleIndex) ||
                    !Contains(CompiledModelContract.SiteMask(model.Structure, siteIndex), particleIndex))
                {
                    if (entry.Energy.Value != 0.0)
                    {
                        nonDefaultCount++;
                    }
                }
            }

            return new SiteMetrics(CheckedInt(slots, "site-energy slot"), nonDefaultCount, range);
        }
    }
}
