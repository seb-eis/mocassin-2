// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Signatures;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Parameterization;

public sealed partial class ParameterizationService
{
    private static Dictionary<CellSiteId, int> BuildSiteIndexById(CompiledModel model)
    {
        var map = new Dictionary<CellSiteId, int>(model.Structure.Sites.Length);
        for (var index = 0; index < model.Structure.Sites.Length; index++)
        {
            map[model.Structure.Sites[index].Id] = index;
        }

        return map;
    }

    private static ulong[] BuildSiteMasks(CompiledModel model)
    {
        var masks = new ulong[model.Structure.Sites.Length];
        for (var index = 0; index < masks.Length; index++)
        {
            masks[index] = CompiledModelContract.SiteMask(model.Structure, index);
        }

        return masks;
    }

    private static Dictionary<string, int> BuildPairTableIndex(CompiledModel model)
    {
        var map = new Dictionary<string, int>(model.Energy.PairTables.Length, StringComparer.Ordinal);
        foreach (var table in model.Energy.PairTables)
        {
            map.TryAdd(table.EnergyId, table.Index);
        }

        return map;
    }

    private static Dictionary<string, int> BuildClusterTableIndex(CompiledModel model)
    {
        var map = new Dictionary<string, int>(model.Energy.ClusterTables.Length, StringComparer.Ordinal);
        foreach (var table in model.Energy.ClusterTables)
        {
            map.TryAdd(table.ClusterEnergyId, table.Index);
        }

        return map;
    }

    private static Dictionary<string, int> BuildRuleIndex(CompiledModel model)
    {
        var map = new Dictionary<string, int>(model.Transitions.RuleCount, StringComparer.Ordinal);
        for (var index = 0; index < model.Transitions.Rules.Length; index++)
        {
            map[model.Transitions.Rules[index].RuleGeometryId] = index;
        }

        return map;
    }

    private static Dictionary<(TransitionKind Kind, Guid Id), int> BuildCollectionIndex(CompiledModel model)
    {
        var map = new Dictionary<(TransitionKind Kind, Guid Id), int>(model.Transitions.Collections.Length);
        foreach (var collection in model.Transitions.Collections)
        {
            map[(collection.Transition.Kind, collection.Transition.Id)] = collection.Index;
        }

        return map;
    }

    private static double[][] AllocatePairValues(CompiledModel model)
    {
        var tables = new double[model.Energy.PairTables.Length][];
        for (var index = 0; index < tables.Length; index++)
        {
            var table = model.Energy.PairTables[index];
            tables[index] = new double[checked(table.ParticleCount * table.ParticleCount)];
        }

        return tables;
    }

    private static double[][] AllocateGroupValues(CompiledModel model)
    {
        var tables = new double[model.Energy.ClusterTables.Length][];
        for (var index = 0; index < tables.Length; index++)
        {
            tables[index] = new double[model.Energy.ClusterTables[index].OccupationCount];
        }

        return tables;
    }

    private static double[] AllocateRuleFrequencies(CompiledModel model)
    {
        var frequencies = new double[model.Transitions.RuleCount];
        for (var index = 0; index < frequencies.Length; index++)
        {
            frequencies[index] = model.Transitions.Rules[index].FrequencyHz;
        }

        return frequencies;
    }

    private static void ResolveAuthoredBackground(
        Project project,
        CompiledModel model,
        Dictionary<CellSiteId, int> siteIndexById,
        ulong[] siteMasks,
        double[] siteEnergies,
        int particleCount,
        Guid templateId,
        DiagnosticBag bag)
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
                                bag,
                                templateId,
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
                        bag,
                        templateId,
                        set.SiteId,
                        entry.ParticleId,
                        "The authored site-energy entry targets a particle the reference site cannot occupy.");
                    continue;
                }

                var value = entry.Energy.Value;
                foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
                {
                    siteEnergies[(model.Structure.Positions[positionIndex].P * particleCount) + particleIndex] = value;
                }
            }
        }
    }

    private static void ApplySiteOverrides(
        ParameterizationTemplate template,
        CompiledModel model,
        Dictionary<CellSiteId, int> siteIndexById,
        ulong[] siteMasks,
        double[] siteEnergies,
        int particleCount,
        Guid templateId,
        DiagnosticBag bag)
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
                    bag,
                    templateId,
                    entry.SiteId,
                    entry.ParticleId,
                    "The site-energy override targets an unknown site/particle or a particle the site cannot occupy.");
                continue;
            }

            var value = entry.Energy.Value;
            foreach (var positionIndex in model.Structure.Sites[siteIndex].PositionIndices)
            {
                siteEnergies[(model.Structure.Positions[positionIndex].P * particleCount) + particleIndex] = value;
            }
        }
    }

    private static void ApplyPairValues(
        ParameterizationTemplate template,
        CompiledModel model,
        Dictionary<int, PairTableDescriptor> descriptorByTable,
        Dictionary<string, int> pairTableByEnergyId,
        double[][] pairValues,
        Guid templateId,
        DiagnosticBag bag)
    {
        AddPairs(template.StablePairSets, "stablePairSets");
        AddPairs(template.UnstablePairSets, "unstablePairSets");

        void AddPairs(ImmutableArray<PairEnergySet> sets, string property)
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
                    AddUnknownReferenceDiagnostic(
                        bag,
                        templateId,
                        $"{property}/{set.EnergyId}",
                        "The template pair set references an unknown or changed compiled pair table.");
                    continue;
                }

                var table = model.Energy.PairTables[tableIndex];
                var values = pairValues[tableIndex];
                var particleCount = table.ParticleCount;
                if (set.Entries.IsDefault)
                {
                    continue;
                }

                foreach (var entry in set.Entries)
                {
                    if (entry is null)
                    {
                        continue;
                    }

                    if (!model.Particles.TryGetIndex(entry.CenterParticleId, out var center) ||
                        !model.Particles.TryGetIndex(entry.PartnerParticleId, out var partner))
                    {
                        AddUnknownReferenceDiagnostic(
                            bag,
                            templateId,
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
                        AddForbiddenParticleDiagnostic(
                            bag,
                            templateId,
                            $"{property}/{set.EnergyId}/entries/{entry.CenterParticleId:D}/{entry.PartnerParticleId:D}/energyEv",
                            "A pair entry names a particle the reference site cannot occupy.");
                        continue;
                    }

                    var value = entry.Energy.Value;
                    values[(center * particleCount) + partner] = value;
                    if (descriptor.IsChiral)
                    {
                        values[(partner * particleCount) + center] = value;
                    }
                }
            }
        }
    }

    private static void ApplyGroupValues(
        ParameterizationTemplate template,
        CompiledModel model,
        Dictionary<string, int> clusterTableByEnergyId,
        ulong[] siteMasks,
        double[][] groupValues,
        Guid templateId,
        DiagnosticBag bag)
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
                AddUnknownReferenceDiagnostic(
                    bag,
                    templateId,
                    $"groupSets/{set.ClusterEnergyId}",
                    "The template group set references an unknown compiled cluster table.");
                continue;
            }

            var table = model.Energy.ClusterTables[tableIndex];
            var centerSiteId = model.Structure.Sites[table.CenterSiteIndex].Id;
            if (table.ClusterId != set.ClusterId || centerSiteId != set.CenterSiteId)
            {
                AddUnknownReferenceDiagnostic(
                    bag,
                    templateId,
                    $"groupSets/{set.ClusterEnergyId}",
                    "The template group set descriptor disagrees with the compiled cluster table.");
                continue;
            }

            var occupations = BuildOccupationIndex(table);
            var values = groupValues[tableIndex];
            if (set.Entries.IsDefault)
            {
                continue;
            }

            foreach (var entry in set.Entries)
            {
                if (entry is null)
                {
                    continue;
                }

                if (entry.OccupationParticleIds.IsDefault ||
                    entry.OccupationParticleIds.Length != table.SitePIndices.Length)
                {
                    AddUnknownReferenceDiagnostic(
                        bag,
                        templateId,
                        $"groupSets/{set.ClusterEnergyId}/entries",
                        "A group entry has an occupation tuple of the wrong length.");
                    continue;
                }

                var indices = new int[entry.OccupationParticleIds.Length];
                var valid = true;
                for (var slot = 0; slot < indices.Length; slot++)
                {
                    if (!model.Particles.TryGetIndex(entry.OccupationParticleIds[slot], out indices[slot]))
                    {
                        valid = false;
                        break;
                    }

                    var positionIndex = table.SitePIndices[slot];
                    var siteIndex = model.Structure.Positions[positionIndex].SiteIndex;
                    if (!Contains(siteMasks[siteIndex], indices[slot]))
                    {
                        AddForbiddenParticleDiagnostic(
                            bag,
                            templateId,
                            $"groupSets/{set.ClusterEnergyId}/entries/{OccupationKey(entry.OccupationParticleIds)}/energyEv",
                            "A group entry names a particle a cluster site cannot occupy.");
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    if (entry.OccupationParticleIds.Any(
                            particle => !model.Particles.TryGetIndex(particle, out _)))
                    {
                        AddUnknownReferenceDiagnostic(
                            bag,
                            templateId,
                            $"groupSets/{set.ClusterEnergyId}/entries",
                            "A group entry references an unknown compiled particle.");
                    }

                    continue;
                }

                if (!occupations.TryGetValue(OccupationKey(indices), out var occupationIndex))
                {
                    AddUnknownReferenceDiagnostic(
                        bag,
                        templateId,
                        $"groupSets/{set.ClusterEnergyId}/entries/{OccupationKey(entry.OccupationParticleIds)}",
                        "A group entry names a canonical occupation the compiled table does not contain.");
                    continue;
                }

                values[occupationIndex] = entry.Energy.Value;
            }
        }
    }

    private static void ApplyRuleFrequencies(
        ParameterizationTemplate template,
        CompiledModel model,
        Dictionary<(TransitionKind Kind, Guid Id), int> collectionIndexByTransition,
        Dictionary<string, int> ruleIndexByGeometryId,
        double[] ruleFrequencies,
        Guid templateId,
        DiagnosticBag bag)
    {
        if (template.TransitionRuleSets.IsDefault)
        {
            return;
        }

        foreach (var set in template.TransitionRuleSets)
        {
            if (set is null || set.TransitionKind != TransitionKind.Kinetic)
            {
                continue;
            }

            if (!collectionIndexByTransition.TryGetValue((set.TransitionKind, set.TransitionId), out var collectionIndex))
            {
                AddUnknownReferenceDiagnostic(
                    bag,
                    templateId,
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
                    AddUnknownReferenceDiagnostic(
                        bag,
                        templateId,
                        $"transitionRuleSets/{set.TransitionKind}/{set.TransitionId:D}/{entry.RuleGeometryId}/frequencyHz",
                        "A rule-frequency entry references an unknown rule or a rule outside the set's collection.");
                    continue;
                }

                ruleFrequencies[ruleIndex] = entry.Frequency.Value;
            }
        }
    }

    private static bool ValidateEffectiveValues(
        CompiledModel model,
        double[][] pairValues,
        double[][] groupValues,
        double[] siteEnergies,
        double[] ruleFrequencies,
        int particleCount,
        Guid templateId,
        DiagnosticBag bag)
    {
        var hadErrors = false;
        var minimum = EnergyLimits.MinEnergy.Value;
        var maximum = EnergyLimits.MaxEnergy.Value;
        for (var tableIndex = 0; tableIndex < pairValues.Length; tableIndex++)
        {
            var table = model.Energy.PairTables[tableIndex];
            var values = pairValues[tableIndex];
            for (var index = 0; index < values.Length; index++)
            {
                if (IsValidEnergy(values[index], minimum, maximum))
                {
                    continue;
                }

                hadErrors = true;
                var center = model.Particles.Particles[index / table.ParticleCount].Id;
                var partner = model.Particles.Particles[index % table.ParticleCount].Id;
                bag.Add(new Diagnostic(
                    ParameterizationDiagnosticCodes.InvalidEnergy,
                    DiagnosticSeverity.Error,
                    string.Create(CultureInfo.InvariantCulture, $"The effective pair energy {values[index]} eV is not finite or is outside the accepted range."),
                    new ModelPath("parameterization", templateId, $"pairTables/{table.EnergyId}/entries/{center:D}/{partner:D}/energyEv"),
                    ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidEnergy)));
            }
        }

        for (var tableIndex = 0; tableIndex < groupValues.Length; tableIndex++)
        {
            var table = model.Energy.ClusterTables[tableIndex];
            var values = groupValues[tableIndex];
            for (var index = 0; index < values.Length; index++)
            {
                if (IsValidEnergy(values[index], minimum, maximum))
                {
                    continue;
                }

                hadErrors = true;
                bag.Add(new Diagnostic(
                    ParameterizationDiagnosticCodes.InvalidEnergy,
                    DiagnosticSeverity.Error,
                    string.Create(CultureInfo.InvariantCulture, $"The effective group energy {values[index]} eV is not finite or is outside the accepted range."),
                    new ModelPath("parameterization", templateId, $"groupSets/{table.ClusterEnergyId}/entries/{index}/energyEv"),
                    ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidEnergy)));
            }
        }

        for (var index = 0; index < siteEnergies.Length; index++)
        {
            if (IsValidEnergy(siteEnergies[index], minimum, maximum))
            {
                continue;
            }

            hadErrors = true;
            var positionIndex = index / particleCount;
            var particleIndex = index % particleCount;
            var siteId = model.Structure.Positions[positionIndex].SiteId;
            var particleId = model.Particles.Particles[particleIndex].Id;
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.InvalidEnergy,
                DiagnosticSeverity.Error,
                string.Create(CultureInfo.InvariantCulture, $"The effective site energy {siteEnergies[index]} eV is not finite or is outside the accepted range."),
                new ModelPath("parameterization", templateId, $"siteEnergyOverrides/{siteId:D}/{particleId:D}/energyEv"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidEnergy)));
        }

        for (var index = 0; index < ruleFrequencies.Length; index++)
        {
            var value = ruleFrequencies[index];
            if (double.IsFinite(value) && value > 0.0)
            {
                continue;
            }

            hadErrors = true;
            var rule = model.Transitions.Rules[index];
            bag.Add(new Diagnostic(
                ParameterizationDiagnosticCodes.InvalidFrequency,
                DiagnosticSeverity.Error,
                string.Create(CultureInfo.InvariantCulture, $"The effective rule frequency {value} Hz is not finite or is not positive."),
                new ModelPath("parameterization", templateId, $"transitionRules/{rule.RuleGeometryId}/frequencyHz"),
                ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidFrequency)));
        }

        return hadErrors;
    }

    private static void Quantize(double[][] tables)
    {
        foreach (var table in tables)
        {
            for (var index = 0; index < table.Length; index++)
            {
                table[index] = CanonicalProjectionWriter.Quantize(table[index]);
            }
        }
    }

    private static void Quantize(double[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = CanonicalProjectionWriter.Quantize(values[index]);
        }
    }

    private static Dictionary<string, int> BuildOccupationIndex(Mocassin.Compilation.Energy.CompiledClusterTable table)
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

    private static string OccupationKey(IEnumerable<int> tuple) => string.Join('/', tuple);

    private static string OccupationKey(ImmutableArray<ParticleId> tuple) =>
        tuple.IsDefault
            ? string.Empty
            : string.Join('/', tuple.Select(static particle => particle.Value.ToString("D", CultureInfo.InvariantCulture)));

    private static bool IsValidEnergy(double value, double minimum, double maximum) =>
        double.IsFinite(value) && value >= minimum && value <= maximum;

    private static bool Contains(ulong mask, int particleIndex) =>
        particleIndex > 0 && (mask & (1UL << particleIndex)) != 0;

    private static void AddUnknownReferenceDiagnostic(
        DiagnosticBag bag,
        Guid templateId,
        string property,
        string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.UnknownReference,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.UnknownReference)));

    private static void AddForbiddenParticleDiagnostic(
        DiagnosticBag bag,
        Guid templateId,
        string property,
        string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.ForbiddenParticle,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.ForbiddenParticle)));

    private static void AddInvalidSiteDiagnostic(
        DiagnosticBag bag,
        Guid templateId,
        CellSiteId siteId,
        ParticleId particleId,
        string message) =>
        bag.Add(new Diagnostic(
            ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget,
            DiagnosticSeverity.Warning,
            message,
            new ModelPath("parameterization", templateId, $"siteEnergyOverrides/{siteId:D}/{particleId:D}/energyEv"),
            ParameterizationDiagnosticCodes.HelpFor(ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget)));
}
