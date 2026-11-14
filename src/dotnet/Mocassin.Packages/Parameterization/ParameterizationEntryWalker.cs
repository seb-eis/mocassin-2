// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Resolves template entries against compiled identities and optionally writes the dense applied targets.
/// </summary>
internal sealed class ParameterizationEntryWalker
{
    private readonly Project project;
    private readonly CompiledModel model;
    private readonly ParameterizationTemplate template;
    private readonly DiagnosticBag bag;
    private readonly ParameterizationQualityOptions quality;
    private readonly ParameterizationTargets? targets;
    private readonly Dictionary<CellSiteId, int> siteIndexById;
    private readonly ulong[] siteMasks;
    private readonly Dictionary<string, int> pairTableByEnergyId;
    private readonly Dictionary<int, PairTableDescriptor> descriptorByTable;
    private readonly Dictionary<string, int> clusterTableByEnergyId;
    private readonly Dictionary<string, int> ruleIndexByGeometryId;
    private readonly Dictionary<(TransitionKind Kind, Guid Id), int> collectionIndexByTransition;
    private readonly int particleCount;
    private readonly Guid templateId;

    public ParameterizationEntryWalker(
        Project project,
        CompiledModel model,
        ParameterizationTemplate template,
        DiagnosticBag bag,
        ParameterizationQualityOptions quality,
        ImmutableArray<PairTableDescriptor> descriptors,
        ParameterizationTargets? targets)
    {
        this.project = project;
        this.model = model;
        this.template = template;
        this.bag = bag;
        this.quality = quality;
        this.targets = targets;
        templateId = template.Id.Value;
        particleCount = model.Particles.Count;
        siteIndexById = new Dictionary<CellSiteId, int>(model.Structure.Sites.Length);
        for (var index = 0; index < model.Structure.Sites.Length; index++)
        {
            siteIndexById[model.Structure.Sites[index].Id] = index;
        }

        siteMasks = new ulong[model.Structure.Sites.Length];
        for (var index = 0; index < siteMasks.Length; index++)
        {
            siteMasks[index] = CompiledModelContract.SiteMask(model.Structure, index);
        }

        pairTableByEnergyId = new Dictionary<string, int>(model.Energy.PairTables.Length, StringComparer.Ordinal);
        foreach (var table in model.Energy.PairTables)
        {
            pairTableByEnergyId.TryAdd(table.EnergyId, table.Index);
        }

        descriptorByTable = descriptors.ToDictionary(static descriptor => descriptor.TableIndex);
        clusterTableByEnergyId = new Dictionary<string, int>(model.Energy.ClusterTables.Length, StringComparer.Ordinal);
        foreach (var table in model.Energy.ClusterTables)
        {
            clusterTableByEnergyId.TryAdd(table.ClusterEnergyId, table.Index);
        }

        ruleIndexByGeometryId = new Dictionary<string, int>(model.Transitions.RuleCount, StringComparer.Ordinal);
        for (var index = 0; index < model.Transitions.Rules.Length; index++)
        {
            ruleIndexByGeometryId[model.Transitions.Rules[index].RuleGeometryId] = index;
        }

        collectionIndexByTransition = new Dictionary<(TransitionKind Kind, Guid Id), int>(
            model.Transitions.Collections.Length);
        foreach (var collection in model.Transitions.Collections)
        {
            collectionIndexByTransition[(collection.Transition.Kind, collection.Transition.Id)] = collection.Index;
        }
    }

    public void Run()
    {
        ResolveAuthoredBackground();
        ApplySiteOverrides();
        ApplyPairSets(template.StablePairSets, "stablePairSets");
        ApplyPairSets(template.UnstablePairSets, "unstablePairSets");
        ApplyGroupSets();
        var overriddenRules = ApplyRuleFrequencies();
        ValidateCompiledRuleFrequencies(overriddenRules);
    }

    private void ResolveAuthoredBackground()
    {
        var authored = project.Model.SiteEnergies;
        if (authored.IsDefault)
        {
            return;
        }

        foreach (var set in authored)
        {
            if (set is null)
            {
                continue;
            }

            if (!siteIndexById.TryGetValue(set.SiteId, out var siteIndex))
            {
                if (!set.Entries.IsDefault)
                {
                    foreach (var entry in set.Entries)
                    {
                        if (entry is not null)
                        {
                            AddInvalidSiteDiagnostic(
                                set.SiteId,
                                entry.ParticleId,
                                "The authored site-energy entry references an unknown reference site.");
                        }
                    }
                }

                continue;
            }

            if (set.Entries.IsDefault)
            {
                continue;
            }

            var mask = siteMasks[siteIndex];
            foreach (var entry in set.Entries)
            {
                if (entry is null)
                {
                    continue;
                }

                if (!model.Particles.TryGetIndex(entry.ParticleId, out var particleIndex) ||
                    !Contains(mask, particleIndex))
                {
                    AddInvalidSiteDiagnostic(
                        set.SiteId,
                        entry.ParticleId,
                        "The authored site-energy entry targets a particle the reference site cannot occupy.");
                    continue;
                }

                var value = entry.Energy.Value;
                ValidateEnergy(value, $"siteEnergyOverrides/{set.SiteId:D}/{entry.ParticleId:D}/energyEv");
                if (targets is null)
                {
                    continue;
                }

                foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
                {
                    targets.SiteEnergies[(model.Structure.Positions[positionIndex].P * particleCount) + particleIndex] = value;
                }
            }
        }
    }

    private void ApplySiteOverrides()
    {
        if (template.SiteEnergyOverrides.IsDefault)
        {
            return;
        }

        foreach (var entry in template.SiteEnergyOverrides)
        {
            if (entry is null)
            {
                continue;
            }

            if (!siteIndexById.TryGetValue(entry.SiteId, out var siteIndex) ||
                !model.Particles.TryGetIndex(entry.ParticleId, out var particleIndex) ||
                !Contains(siteMasks[siteIndex], particleIndex))
            {
                AddInvalidSiteDiagnostic(
                    entry.SiteId,
                    entry.ParticleId,
                    "The site-energy override targets an unknown site/particle or a particle the site cannot occupy.");
                continue;
            }

            var value = entry.Energy.Value;
            ValidateEnergy(value, $"siteEnergyOverrides/{entry.SiteId:D}/{entry.ParticleId:D}/energyEv");
            if (targets is null)
            {
                continue;
            }

            foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
            {
                targets.SiteEnergies[(model.Structure.Positions[positionIndex].P * particleCount) + particleIndex] = value;
            }
        }
    }

    private void ApplyPairSets(ImmutableArray<PairEnergySet> sets, string property)
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

            if (set.EnergyId is null ||
                !pairTableByEnergyId.TryGetValue(set.EnergyId, out var tableIndex) ||
                !descriptorByTable.TryGetValue(tableIndex, out var descriptor) ||
                descriptor.CenterSiteId != set.CenterSiteId ||
                descriptor.PartnerSiteId != set.PartnerSiteId ||
                descriptor.DistanceBucketFm != set.DistanceBucketFm ||
                descriptor.IsChiral != set.IsChiral)
            {
                AddUnknownReference(
                    $"{property}/{set.EnergyId}",
                    "The template pair set references an unknown or changed compiled pair table.");
                continue;
            }

            if (set.Entries.IsDefault)
            {
                continue;
            }

            var table = model.Energy.PairTables[tableIndex];
            var values = targets?.PairValues[tableIndex];
            foreach (var entry in set.Entries)
            {
                if (entry is null)
                {
                    continue;
                }

                if (!model.Particles.TryGetIndex(entry.CenterParticleId, out var center) ||
                    !model.Particles.TryGetIndex(entry.PartnerParticleId, out var partner))
                {
                    AddUnknownReference(
                        $"{property}/{set.EnergyId}/entries",
                        "A pair entry references an unknown compiled particle.");
                    continue;
                }

                var ordered = Contains(descriptor.CenterMask, center) &&
                              Contains(descriptor.PartnerMask, partner);
                var mirrored = descriptor.IsChiral &&
                               Contains(descriptor.CenterMask, partner) &&
                               Contains(descriptor.PartnerMask, center);
                if (!ordered && !mirrored)
                {
                    AddForbiddenParticle(
                        $"{property}/{set.EnergyId}/entries/{entry.CenterParticleId:D}/{entry.PartnerParticleId:D}/energyEv",
                        "A pair entry names a particle the reference site cannot occupy.");
                    continue;
                }

                var value = entry.Energy.Value;
                ValidateEnergy(
                    value,
                    $"{property}/{set.EnergyId}/entries/{entry.CenterParticleId:D}/{entry.PartnerParticleId:D}/energyEv");
                if (values is null)
                {
                    continue;
                }

                values[(center * table.ParticleCount) + partner] = value;
                if (descriptor.IsChiral)
                {
                    values[(partner * table.ParticleCount) + center] = value;
                }
            }
        }
    }

    private void ApplyGroupSets()
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

            if (set.ClusterEnergyId is null ||
                !clusterTableByEnergyId.TryGetValue(set.ClusterEnergyId, out var tableIndex))
            {
                AddUnknownReference(
                    $"groupSets/{set.ClusterEnergyId}",
                    "The template group set references an unknown compiled cluster table.");
                continue;
            }

            var table = model.Energy.ClusterTables[tableIndex];
            var centerSiteId = model.Structure.Sites[table.CenterSiteIndex].Id;
            if (table.ClusterId != set.ClusterId || centerSiteId != set.CenterSiteId)
            {
                AddUnknownReference(
                    $"groupSets/{set.ClusterEnergyId}",
                    "The template group set descriptor disagrees with the compiled cluster table.");
                continue;
            }

            if (set.Entries.IsDefault)
            {
                continue;
            }

            var occupations = BuildOccupationIndex(table);
            var values = targets?.GroupValues[tableIndex];
            foreach (var entry in set.Entries)
            {
                if (entry is null)
                {
                    continue;
                }

                var occupation = entry.OccupationParticleIds.IsDefault
                    ? ImmutableArray<ParticleId>.Empty
                    : entry.OccupationParticleIds;
                if (occupation.Length != table.SitePIndices.Length)
                {
                    AddUnknownReference(
                        $"groupSets/{set.ClusterEnergyId}/entries",
                        "A group entry has an occupation tuple of the wrong length.");
                    continue;
                }

                var indices = new int[occupation.Length];
                var hasUnknownParticle = false;
                for (var slot = 0; slot < indices.Length; slot++)
                {
                    if (!model.Particles.TryGetIndex(occupation[slot], out indices[slot]))
                    {
                        hasUnknownParticle = true;
                        break;
                    }
                }

                if (hasUnknownParticle)
                {
                    AddUnknownReference(
                        $"groupSets/{set.ClusterEnergyId}/entries",
                        "A group entry references an unknown compiled particle.");
                    continue;
                }

                var hasForbiddenParticle = false;
                for (var slot = 0; slot < indices.Length; slot++)
                {
                    var positionIndex = table.SitePIndices[slot];
                    var siteIndex = model.Structure.Positions[positionIndex].SiteIndex;
                    if (!Contains(siteMasks[siteIndex], indices[slot]))
                    {
                        hasForbiddenParticle = true;
                        break;
                    }
                }

                var entryKey = OccupationKey(occupation);
                if (hasForbiddenParticle)
                {
                    AddForbiddenParticle(
                        $"groupSets/{set.ClusterEnergyId}/entries/{entryKey}/energyEv",
                        "A group entry names a particle a cluster site cannot occupy.");
                    continue;
                }

                if (!occupations.TryGetValue(OccupationKey(indices), out var occupationIndex))
                {
                    AddUnknownReference(
                        $"groupSets/{set.ClusterEnergyId}/entries/{entryKey}",
                        "A group entry names a canonical occupation the compiled table does not contain.");
                    continue;
                }

                var value = entry.Energy.Value;
                ValidateEnergy(value, $"groupSets/{set.ClusterEnergyId}/entries/{entryKey}/energyEv");
                if (values is not null)
                {
                    values[occupationIndex] = value;
                }
            }
        }
    }

    private HashSet<int> ApplyRuleFrequencies()
    {
        var overridden = new HashSet<int>();
        if (template.TransitionRuleSets.IsDefault)
        {
            return overridden;
        }

        foreach (var set in template.TransitionRuleSets)
        {
            if (set is null)
            {
                continue;
            }

            if (set.TransitionKind == TransitionKind.Metropolis)
            {
                bag.Add(new Diagnostic(
                    ParameterizationDiagnosticCodes.MetropolisRuleSetIgnored,
                    DiagnosticSeverity.Warning,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The template contains a Metropolis rule set for transition '{set.TransitionId:D}'. MMC rules are not parameterizable; the compiled default remains."),
                    new ModelPath(
                        "parameterization",
                        templateId,
                        $"transitionRuleSets/{set.TransitionKind}/{set.TransitionId:D}"),
                    ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.MetropolisRuleSetIgnored)));
                continue;
            }

            if (set.TransitionKind != TransitionKind.Kinetic)
            {
                continue;
            }

            if (!collectionIndexByTransition.TryGetValue((set.TransitionKind, set.TransitionId), out var collectionIndex))
            {
                AddUnknownReference(
                    $"transitionRuleSets/{set.TransitionKind}/{set.TransitionId:D}",
                    "The template rule set references an unknown compiled kinetic collection.");
                continue;
            }

            if (set.Rules.IsDefault)
            {
                continue;
            }

            foreach (var entry in set.Rules)
            {
                if (entry is null || entry.RuleGeometryId is null)
                {
                    continue;
                }

                if (!ruleIndexByGeometryId.TryGetValue(entry.RuleGeometryId, out var ruleIndex) ||
                    model.Transitions.Rules[ruleIndex].CollectionIndex != collectionIndex)
                {
                    AddUnknownReference(
                        $"transitionRuleSets/{set.TransitionKind}/{set.TransitionId:D}/{entry.RuleGeometryId}/frequencyHz",
                        "A rule-frequency entry references an unknown rule or a rule outside the set's collection.");
                    continue;
                }

                var value = entry.Frequency.Value;
                ValidateFrequency(
                    value,
                    $"transitionRuleSets/{set.TransitionKind}/{set.TransitionId:D}/{entry.RuleGeometryId}/frequencyHz");
                overridden.Add(ruleIndex);
                if (targets is not null)
                {
                    targets.RuleFrequencies[ruleIndex] = value;
                }
            }
        }

        return overridden;
    }

    private void ValidateCompiledRuleFrequencies(HashSet<int> overridden)
    {
        for (var index = 0; index < model.Transitions.Rules.Length; index++)
        {
            if (overridden.Contains(index))
            {
                continue;
            }

            var rule = model.Transitions.Rules[index];
            var collection = model.Transitions.Collections[rule.CollectionIndex];
            var transition = collection.Transition;
            ValidateFrequency(
                rule.FrequencyHz,
                $"transitionRuleSets/{transition.Kind}/{transition.Id:D}/{rule.RuleGeometryId}/frequencyHz");
        }
    }

    private void ValidateEnergy(double value, string property)
    {
        if (double.IsFinite(value) &&
            value >= EnergyLimits.MinEnergy.Value &&
            value <= EnergyLimits.MaxEnergy.Value)
        {
            return;
        }

        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.InvalidEnergy,
            DiagnosticSeverity.Error,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The effective energy {value} eV is not finite or is outside the accepted {EnergyLimits.MinEnergy.Value}..{EnergyLimits.MaxEnergy.Value} eV range."),
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidEnergy)));
    }

    private void ValidateFrequency(double value, string property)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.InvalidFrequency,
                DiagnosticSeverity.Error,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The effective rule frequency {value} Hz is not finite or is not positive."),
                new ModelPath("parameterization", templateId, property),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidFrequency)));
            return;
        }

        var minimum = quality.MinimumPlausibleFrequency.Value;
        var maximum = quality.MaximumPlausibleFrequency.Value;
        if (value >= minimum && value <= maximum)
        {
            return;
        }

        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.ImplausibleFrequency,
            DiagnosticSeverity.Warning,
            string.Create(
                CultureInfo.InvariantCulture,
                $"The effective rule frequency {value} Hz is outside the plausible {minimum}..{maximum} Hz range."),
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.ImplausibleFrequency)));
    }

    private void AddUnknownReference(string property, string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.UnknownReference,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.UnknownReference)));

    private void AddForbiddenParticle(string property, string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.ForbiddenParticle,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.ForbiddenParticle)));

    private void AddInvalidSiteDiagnostic(CellSiteId siteId, ParticleId particleId, string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, $"siteEnergyOverrides/{siteId:D}/{particleId:D}/energyEv"),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget)));

    private static Dictionary<string, int> BuildOccupationIndex(CompiledClusterTable table)
    {
        var map = new Dictionary<string, int>(table.OccupationCount, StringComparer.Ordinal);
        for (var index = 0; index < table.Occupations.Length; index++)
        {
            map[OccupationKey(table.Occupations[index].CanonicalTuple)] = index;
        }

        return map;
    }

    private static string OccupationKey(ImmutableArray<int> tuple) =>
        tuple.IsDefault ? string.Empty : string.Join('/', tuple);

    private static string OccupationKey(int[] tuple) => string.Join('/', tuple);

    private static string OccupationKey(ImmutableArray<ParticleId> tuple) =>
        tuple.IsDefault
            ? string.Empty
            : string.Join('/', tuple.Select(static particle => particle.Value.ToString("D", CultureInfo.InvariantCulture)));

    private static bool Contains(ulong mask, int particleIndex) =>
        particleIndex > 0 && (mask & (1UL << particleIndex)) != 0;
}
