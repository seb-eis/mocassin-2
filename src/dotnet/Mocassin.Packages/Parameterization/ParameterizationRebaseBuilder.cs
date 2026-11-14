// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Units;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Rebuilds a template against a current compiled model from a deterministic slot comparison.
/// </summary>
/// <remarks>
/// Rebase copies only exact identity matches or, in nearest mode, unique reported pair descriptor matches. It
/// never reuses a value across a different identity without a report entry, never mutates its inputs, and never
/// saves the result. Chiral pair entries are exact-ID only because the template stores no reciprocal-link
/// identity; a changed chiral link therefore always defaults.
/// </remarks>
internal static class ParameterizationRebaseBuilder
{
    /// <summary>
    /// Rebuilds one template against a current compiled model.
    /// </summary>
    /// <param name="model">The current compiled model.</param>
    /// <param name="template">The template to rebase.</param>
    /// <param name="options">The strategy and nearest tolerance.</param>
    /// <param name="cancellationToken">Cancels the operation before the nearest scan.</param>
    /// <returns>The rebound template and its report.</returns>
    /// <exception cref="InvalidOperationException">The compiled model violates a contract invariant.</exception>
    public static ParameterizationRebaseResult Rebuild(
        CompiledModel model,
        ParameterizationTemplate template,
        ParameterizationRebaseOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();
        var descriptors = CompiledModelContract.DescribePairTables(model);
        var comparison = ParameterizationDiffBuilder.Compare(model, template);
        var headers = ParameterizationFactory.CreateHeaders(
            model,
            descriptors,
            template.Id,
            template.Name,
            template.CreatedUtc);
        var copies = new List<CopiedValue>();
        var details = ImmutableArray.CreateBuilder<ParameterizationDescriptorMatch>();
        var ambiguousMatches = 0;
        int copiedValues;
        int defaultedValues;
        int droppedValues;

        if (options.Strategy == ParameterizationRebaseStrategy.Reset)
        {
            copiedValues = 0;
            defaultedValues = RequiredSlotCount(comparison);
            droppedValues = comparison.Matched.Length + comparison.Removed.Length;
        }
        else
        {
            foreach (var matched in comparison.Matched)
            {
                copies.Add(new CopiedValue(matched.Target, matched.Source));
            }

            copiedValues = comparison.Matched.Length;
            defaultedValues = comparison.Added.Length;
            droppedValues = comparison.Removed.Length;
            if (options.Strategy == ParameterizationRebaseStrategy.NearestGeometry)
            {
                var nearest = FindNearestCopies(
                    comparison,
                    options.NearestDistanceToleranceFm,
                    cancellationToken);
                copies.AddRange(nearest.Copies);
                details.AddRange(nearest.Details);
                copiedValues += nearest.Copies.Length;
                defaultedValues -= nearest.Copies.Length;
                droppedValues -= nearest.Copies.Length;
                ambiguousMatches = nearest.AmbiguousMatches;
            }
        }

        var rebound = BuildTemplate(headers, copies);
        var report = new ParameterizationRebaseReport(
            options.Strategy,
            comparison.Diff,
            copiedValues,
            defaultedValues,
            droppedValues,
            ambiguousMatches,
            details.Count)
        {
            DescriptorMatchDetails = details.ToImmutable(),
        };
        var bag = new DiagnosticBag();
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.RebaseReport,
            DiagnosticSeverity.Info,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Template rebase report: {report.CopiedValues} copied, {report.DefaultedValues} defaulted, {report.DroppedValues} dropped, {report.AmbiguousMatches} ambiguous."),
            new ModelPath("parameterization", template.Id.Value, "modelSignature"),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.RebaseReport)));
        return new ParameterizationRebaseResult(rebound, report, bag.Sorted);
    }

    private static int RequiredSlotCount(ParameterizationComparison comparison)
    {
        var matchedRequired = 0;
        foreach (var matched in comparison.Matched)
        {
            if (matched.Target.Key.Kind != ParameterizationValueKind.SiteEnergyOverride)
            {
                matchedRequired++;
            }
        }

        return comparison.Added.Length + matchedRequired;
    }

    private static NearestCopyResult FindNearestCopies(
        ParameterizationComparison comparison,
        int tolerance,
        CancellationToken cancellationToken)
    {
        var candidatesByDescriptor = new Dictionary<(string LogicalKey, string EntryId, ParameterizationValueKind Kind), List<ParameterizationValueSlot>>();
        foreach (var slot in comparison.Added)
        {
            if (slot.Pair is null ||
                slot.Pair.IsChiral ||
                string.IsNullOrEmpty(slot.LogicalKey) ||
                slot.Key.Kind is not (ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair) ||
                slot.Key.EntryId is null)
            {
                continue;
            }

            var key = (slot.LogicalKey, slot.Key.EntryId, slot.Key.Kind);
            if (!candidatesByDescriptor.TryGetValue(key, out var list))
            {
                list = [];
                candidatesByDescriptor[key] = list;
            }

            list.Add(slot);
        }

        var tentative = new List<(ParameterizationValueSlot Source, ParameterizationValueSlot Target)>();
        var ambiguousMatches = 0;
        foreach (var removed in comparison.Removed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (removed.Pair is null ||
                removed.Pair.IsChiral ||
                string.IsNullOrEmpty(removed.LogicalKey) ||
                removed.Key.Kind is not (ParameterizationValueKind.StablePair or ParameterizationValueKind.UnstablePair) ||
                removed.Key.EntryId is null ||
                !candidatesByDescriptor.TryGetValue(
                    (removed.LogicalKey, removed.Key.EntryId, removed.Key.Kind),
                    out var candidates))
            {
                continue;
            }

            ParameterizationValueSlot? unique = null;
            foreach (var candidate in candidates)
            {
                if (Math.Abs((long)candidate.Pair!.DistanceBucketFm - removed.Pair.DistanceBucketFm) > tolerance)
                {
                    continue;
                }

                if (unique is not null)
                {
                    unique = null;
                    ambiguousMatches++;
                    break;
                }

                unique = candidate;
            }

            if (unique is not null)
            {
                tentative.Add((removed, unique));
            }
        }

        var ownersByTarget = new Dictionary<string, List<(ParameterizationValueSlot Source, ParameterizationValueSlot Target)>>(StringComparer.Ordinal);
        foreach (var item in tentative)
        {
            var identity = ParameterizationDiffBuilder.Identity(item.Target.Key);
            if (!ownersByTarget.TryGetValue(identity, out var owners))
            {
                owners = [];
                ownersByTarget[identity] = owners;
            }

            owners.Add(item);
        }

        var copies = new List<CopiedValue>();
        var details = ImmutableArray.CreateBuilder<ParameterizationDescriptorMatch>();
        foreach (var owners in ownersByTarget.Values)
        {
            if (owners.Count > 1)
            {
                ambiguousMatches += owners.Count;
                continue;
            }

            var (source, target) = owners[0];
            copies.Add(new CopiedValue(target, source));
            details.Add(new ParameterizationDescriptorMatch(
                source.Key,
                target.Key,
                source.Value!.Value,
                source.Pair!.DistanceBucketFm,
                target.Pair!.DistanceBucketFm));
        }

        copies.Sort(static (left, right) => ParameterizationDiffBuilder.CompareKeys(left.Target.Key, right.Target.Key));
        return new NearestCopyResult(
            [.. copies],
            ambiguousMatches,
            [.. details
                .OrderBy(static detail => detail.RemovedKey, ParameterizationValueKeyComparer.Instance)
                .ThenBy(static detail => detail.AddedKey, ParameterizationValueKeyComparer.Instance)]);
    }

    private static ParameterizationTemplate BuildTemplate(
        ParameterizationTemplate headers,
        IReadOnlyList<CopiedValue> copies)
    {
        var stablePairs = BuildPairSets(headers.StablePairSets, copies, ParameterizationValueKind.StablePair);
        var unstablePairs = BuildPairSets(headers.UnstablePairSets, copies, ParameterizationValueKind.UnstablePair);
        var groups = BuildGroupSets(headers.GroupSets, copies);
        var rules = BuildRuleSets(headers.TransitionRuleSets, copies);
        var overrides = BuildSiteOverrides(copies);
        return headers with
        {
            StablePairSets = stablePairs,
            UnstablePairSets = unstablePairs,
            GroupSets = groups,
            TransitionRuleSets = rules,
            SiteEnergyOverrides = overrides,
        };
    }

    private static ImmutableArray<PairEnergySet> BuildPairSets(
        ImmutableArray<PairEnergySet> sets,
        IReadOnlyList<CopiedValue> copies,
        ParameterizationValueKind kind) =>
        [
            .. sets.Select(set => set with
            {
                Entries = BuildPairEntries(
                    set,
                    copies.Where(
                        copy => copy.Target.Key.Kind == kind &&
                                string.Equals(copy.Target.Key.SetId, set.EnergyId, StringComparison.Ordinal))),
            }),
        ];

    private static ImmutableArray<PairEnergyEntry> BuildPairEntries(
        PairEnergySet set,
        IEnumerable<CopiedValue> copies) =>
        [
            .. copies
                .Select(copy => new PairEnergyEntry(
                    copy.Target.Pair!.CenterParticleId,
                    copy.Target.Pair.PartnerParticleId,
                    new EnergyEv(copy.Source.Value!.Value)))
                .OrderBy(static entry => entry.CenterParticleId.Value)
                .ThenBy(static entry => entry.PartnerParticleId.Value),
        ];

    private static ImmutableArray<GroupEnergySet> BuildGroupSets(
        ImmutableArray<GroupEnergySet> sets,
        IReadOnlyList<CopiedValue> copies) =>
        [
            .. sets.Select(set => set with
            {
                Entries = BuildGroupEntries(
                    set,
                    copies.Where(
                        copy => copy.Target.Key.Kind == ParameterizationValueKind.Group &&
                                string.Equals(copy.Target.Key.SetId, set.ClusterEnergyId, StringComparison.Ordinal))),
            }),
        ];

    private static ImmutableArray<GroupEnergyEntry> BuildGroupEntries(
        GroupEnergySet set,
        IEnumerable<CopiedValue> copies) =>
        [
            .. copies
                .Select(copy => new GroupEnergyEntry(
                    copy.Target.GroupOccupation,
                    new EnergyEv(copy.Source.Value!.Value)))
                .OrderBy(static entry => entry.OccupationParticleIds, OccupationComparer.Instance),
        ];

    private static ImmutableArray<TransitionRuleSet> BuildRuleSets(
        ImmutableArray<TransitionRuleSet> sets,
        IReadOnlyList<CopiedValue> copies) =>
        [
            .. sets.Select(set => set with
            {
                Rules = BuildRuleEntries(
                    set,
                    copies.Where(
                        copy => copy.Target.Key.Kind == ParameterizationValueKind.RuleFrequency &&
                                copy.Target.Key.TransitionKind == set.TransitionKind &&
                                copy.Target.Key.TransitionId == set.TransitionId)),
            }),
        ];

    private static ImmutableArray<RuleFrequencyEntry> BuildRuleEntries(
        TransitionRuleSet set,
        IEnumerable<CopiedValue> copies) =>
        [
            .. copies
                .Select(copy => new RuleFrequencyEntry(
                    copy.Target.Key.EntryId!,
                    new FrequencyHz(copy.Source.Value!.Value)))
                .OrderBy(static entry => entry.RuleGeometryId, StringComparer.Ordinal),
        ];

    private static ImmutableArray<SiteEnergyOverride> BuildSiteOverrides(IEnumerable<CopiedValue> copies) =>
        [
            .. copies
                .Where(static copy => copy.Target.Key.Kind == ParameterizationValueKind.SiteEnergyOverride)
                .Select(copy => new SiteEnergyOverride(
                    copy.Target.Key.SiteId!.Value,
                    copy.Target.Key.ParticleId!.Value,
                    new EnergyEv(copy.Source.Value!.Value)))
                .OrderBy(static entry => entry.SiteId.Value)
                .ThenBy(static entry => entry.ParticleId.Value),
        ];

    private sealed record CopiedValue(
        ParameterizationValueSlot Target,
        ParameterizationValueSlot Source);

    private sealed record NearestCopyResult(
        ImmutableArray<CopiedValue> Copies,
        int AmbiguousMatches,
        ImmutableArray<ParameterizationDescriptorMatch> Details);

    private sealed class OccupationComparer : IComparer<ImmutableArray<ParticleId>>
    {
        public static OccupationComparer Instance { get; } = new();

        public int Compare(ImmutableArray<ParticleId> x, ImmutableArray<ParticleId> y)
        {
            var left = x.IsDefault ? ImmutableArray<ParticleId>.Empty : x;
            var right = y.IsDefault ? ImmutableArray<ParticleId>.Empty : y;
            var common = Math.Min(left.Length, right.Length);
            for (var index = 0; index < common; index++)
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
