// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Units;

namespace Mocassin.Core.Project;

/// <summary>
/// Validates the structural rules of one parameterization template without a compiled model.
/// </summary>
/// <remarks>
/// The checks cover stable-key syntax and uniqueness, value-range and positivity rules, and the untrusted-input
/// limits. Compiled-model-bound checks such as an unknown stable ID or a stale model signature belong to the
/// parameterization service in <c>Mocassin.Packages</c>.
/// </remarks>
internal static class ParameterizationTemplateValidator
{
    /// <summary>
    /// Determines whether a stable identity is 64 lowercase hexadecimal characters.
    /// </summary>
    /// <param name="id">The candidate identity.</param>
    /// <returns><see langword="true"/> when the identity is well formed.</returns>
    public static bool IsValidStableId(string? id) =>
        id is { Length: ParameterizationLimits.StableIdLength } &&
        id.All(static character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>
    /// Validates one template.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ParameterizationTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var bag = new DiagnosticBag();
        var templateId = template.Id.Value;

        if (templateId == Guid.Empty)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.InvalidKey,
                "Parameterization template ID must not be empty.",
                templateId,
                "id"));
        }

        if (template.Name is { Length: > ParameterizationLimits.TemplateNameLength })
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Parameterization template name exceeds {ParameterizationLimits.TemplateNameLength} characters."),
                templateId,
                "name"));
        }

        var pairSetCount = Length(template.StablePairSets) + Length(template.UnstablePairSets);
        if (pairSetCount > ParameterizationLimits.PairSetsPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.PairSetsPerTemplate} pair sets."),
                templateId,
                "stablePairSets"));
        }

        if (Length(template.GroupSets) > ParameterizationLimits.GroupSetsPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.GroupSetsPerTemplate} group sets."),
                templateId,
                "groupSets"));
        }

        if (Length(template.SiteEnergyOverrides) > ParameterizationLimits.SiteOverridesPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.SiteOverridesPerTemplate} site-energy overrides."),
                templateId,
                "siteEnergyOverrides"));
        }

        if (Length(template.TransitionRuleSets) > ParameterizationLimits.TransitionRuleSetsPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.TransitionRuleSetsPerTemplate} transition rule sets."),
                templateId,
                "transitionRuleSets"));
        }

        long totalEntries = 0;
        var pairIds = new HashSet<string>(StringComparer.Ordinal);
        ValidatePairSets(template, template.StablePairSets, "stablePairSets", pairIds, ref totalEntries, bag);
        ValidatePairSets(template, template.UnstablePairSets, "unstablePairSets", pairIds, ref totalEntries, bag);
        ValidateGroupSets(template, ref totalEntries, bag);
        ValidateSiteEnergyOverrides(template, ref totalEntries, bag);
        ValidateTransitionRuleSets(template, ref totalEntries, bag);

        if (totalEntries > ParameterizationLimits.TotalExplicitValueEntriesPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.TotalExplicitValueEntriesPerTemplate} explicit value entries."),
                templateId,
                property: null));
        }

        return bag.Sorted;
    }

    private static void ValidatePairSets(
        ParameterizationTemplate template,
        ImmutableArray<PairEnergySet> sets,
        string property,
        HashSet<string> seenIds,
        ref long totalEntries,
        DiagnosticBag bag)
    {
        if (sets.IsDefault)
        {
            return;
        }

        var templateId = template.Id.Value;
        foreach (var set in sets)
        {
            if (set is null)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    $"{property} contains a null set.",
                    templateId,
                    property));
                continue;
            }

            var setPath = $"{property}/{set.EnergyId}";
            if (!IsValidStableId(set.EnergyId))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "A pair energy ID must be 64 lowercase hexadecimal characters.",
                    templateId,
                    setPath));
            }
            else if (!seenIds.Add(set.EnergyId))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    $"Pair energy ID '{set.EnergyId}' is used more than once.",
                    templateId,
                    setPath));
            }

            var entries = set.Entries.IsDefault ? ImmutableArray<PairEnergyEntry>.Empty : set.Entries;
            if (entries.Length > ParameterizationLimits.PairEntriesPerSet)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.LimitExceeded,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"A pair set may contain at most {ParameterizationLimits.PairEntriesPerSet} entries."),
                    templateId,
                    $"{setPath}/entries"));
            }

            totalEntries += entries.Length;
            var keys = new HashSet<(Guid Center, Guid Partner)>();
            var values = new Dictionary<(Guid Center, Guid Partner), double>();
            foreach (var entry in entries)
            {
                if (entry is null)
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "A pair set contains a null entry.",
                        templateId,
                        $"{setPath}/entries"));
                    continue;
                }

                var key = (Center: entry.CenterParticleId.Value, Partner: entry.PartnerParticleId.Value);
                var entryPath = $"{setPath}/entries/{Key(key.Center)}/{Key(key.Partner)}/energyEv";
                if (!keys.Add(key))
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "The same pair value key occurs more than once.",
                        templateId,
                        entryPath));
                }

                ValidateEnergy(entry.Energy, templateId, entryPath, bag);

                if (set.IsChiral)
                {
                    var mirror = (Center: key.Partner, Partner: key.Center);
                    if (values.TryGetValue(mirror, out var mirrorValue) && mirrorValue != entry.Energy.Value)
                    {
                        bag.Add(Error(
                            ParameterizationDiagnosticCodes.InvalidKey,
                            "A chiral pair has independent mirrored values; the template stores one shared value.",
                            templateId,
                            entryPath));
                    }
                }

                values[key] = entry.Energy.Value;
            }
        }
    }

    private static void ValidateGroupSets(
        ParameterizationTemplate template,
        ref long totalEntries,
        DiagnosticBag bag)
    {
        var sets = template.GroupSets.IsDefault ? ImmutableArray<GroupEnergySet>.Empty : template.GroupSets;
        var templateId = template.Id.Value;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var clusterIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in sets)
        {
            if (set is null)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "groupSets contains a null set.",
                    templateId,
                    "groupSets"));
                continue;
            }

            var setPath = $"groupSets/{set.ClusterEnergyId}";
            if (!IsValidStableId(set.ClusterEnergyId))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "A cluster energy ID must be 64 lowercase hexadecimal characters.",
                    templateId,
                    setPath));
            }
            else if (!clusterIds.Add(set.ClusterEnergyId))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    $"Cluster energy ID '{set.ClusterEnergyId}' is used by more than one group set.",
                    templateId,
                    setPath));
            }

            var entries = set.Entries.IsDefault ? ImmutableArray<GroupEnergyEntry>.Empty : set.Entries;
            if (entries.Length > ParameterizationLimits.GroupEntriesPerSet)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.LimitExceeded,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"A group set may contain at most {ParameterizationLimits.GroupEntriesPerSet} entries."),
                    templateId,
                    $"{setPath}/entries"));
            }

            totalEntries += entries.Length;
            foreach (var entry in entries)
            {
                if (entry is null)
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "A group set contains a null entry.",
                        templateId,
                        $"{setPath}/entries"));
                    continue;
                }

                var occupation = entry.OccupationParticleIds.IsDefault
                    ? ImmutableArray<ParticleId>.Empty
                    : entry.OccupationParticleIds;
                var occupationKey = string.Join("/", occupation.Select(static id => Key(id.Value)));
                var entryPath = $"{setPath}/entries/{occupationKey}/energyEv";
                if (occupation.Length == 0)
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "A group occupation tuple must not be empty.",
                        templateId,
                        entryPath));
                }

                if (!keys.Add($"{set.ClusterEnergyId}/{occupationKey}"))
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "The same group value key occurs more than once.",
                        templateId,
                        entryPath));
                }

                ValidateEnergy(entry.Energy, templateId, entryPath, bag);
            }
        }
    }

    private static void ValidateSiteEnergyOverrides(
        ParameterizationTemplate template,
        ref long totalEntries,
        DiagnosticBag bag)
    {
        var overrides = template.SiteEnergyOverrides.IsDefault
            ? ImmutableArray<SiteEnergyOverride>.Empty
            : template.SiteEnergyOverrides;
        var templateId = template.Id.Value;
        if (overrides.Length > ParameterizationLimits.SiteOverridesPerTemplate)
        {
            bag.Add(Error(
                ParameterizationDiagnosticCodes.LimitExceeded,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"A template may contain at most {ParameterizationLimits.SiteOverridesPerTemplate} site-energy overrides."),
                templateId,
                "siteEnergyOverrides"));
        }

        totalEntries += overrides.Length;
        var keys = new HashSet<(Guid Site, Guid Particle)>();
        foreach (var entry in overrides)
        {
            if (entry is null)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "siteEnergyOverrides contains a null entry.",
                    templateId,
                    "siteEnergyOverrides"));
                continue;
            }

            var key = (Site: entry.SiteId.Value, Particle: entry.ParticleId.Value);
            var entryPath = $"siteEnergyOverrides/{Key(key.Site)}/{Key(key.Particle)}/energyEv";
            if (!keys.Add(key))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "The same site-energy override key occurs more than once.",
                    templateId,
                    entryPath));
            }

            ValidateEnergy(entry.Energy, templateId, entryPath, bag);
        }
    }

    private static void ValidateTransitionRuleSets(
        ParameterizationTemplate template,
        ref long totalEntries,
        DiagnosticBag bag)
    {
        var sets = template.TransitionRuleSets.IsDefault
            ? ImmutableArray<TransitionRuleSet>.Empty
            : template.TransitionRuleSets;
        var templateId = template.Id.Value;
        var setKeys = new HashSet<(TransitionKind Kind, Guid Id)>();
        foreach (var set in sets)
        {
            if (set is null)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "transitionRuleSets contains a null set.",
                    templateId,
                    "transitionRuleSets"));
                continue;
            }

            if (set.TransitionKind is not (TransitionKind.Kinetic or TransitionKind.Metropolis))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "A transition rule set has an unknown transition kind.",
                    templateId,
                    "transitionRuleSets"));
            }
            else if (set.TransitionId == Guid.Empty)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "A transition rule set has an empty transition identifier.",
                    templateId,
                    "transitionRuleSets"));
            }
            else if (!setKeys.Add((set.TransitionKind, set.TransitionId)))
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.InvalidKey,
                    "The same transition rule set occurs more than once.",
                    templateId,
                    $"transitionRuleSets/{set.TransitionKind}/{Key(set.TransitionId)}"));
            }

            var rules = set.Rules.IsDefault ? ImmutableArray<RuleFrequencyEntry>.Empty : set.Rules;
            var setPath = $"transitionRuleSets/{set.TransitionKind}/{Key(set.TransitionId)}";
            if (rules.Length > ParameterizationLimits.RuleEntriesPerSet)
            {
                bag.Add(Error(
                    ParameterizationDiagnosticCodes.LimitExceeded,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"A transition rule set may contain at most {ParameterizationLimits.RuleEntriesPerSet} entries."),
                    templateId,
                    $"{setPath}/rules"));
            }

            totalEntries += rules.Length;
            var ruleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (rule is null)
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "A transition rule set contains a null entry.",
                        templateId,
                        $"{setPath}/rules"));
                    continue;
                }

                var rulePath = $"{setPath}/{rule.RuleGeometryId}/frequencyHz";
                if (!IsValidStableId(rule.RuleGeometryId))
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        "A rule geometry ID must be 64 lowercase hexadecimal characters.",
                        templateId,
                        rulePath));
                }
                else if (!ruleIds.Add(rule.RuleGeometryId))
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidKey,
                        $"Rule geometry ID '{rule.RuleGeometryId}' is used more than once.",
                        templateId,
                        rulePath));
                }

                if (!double.IsFinite(rule.Frequency.Value) || rule.Frequency.Value <= 0.0)
                {
                    bag.Add(Error(
                        ParameterizationDiagnosticCodes.InvalidFrequency,
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Frequency {rule.Frequency.Value} Hz must be positive and finite."),
                        templateId,
                        rulePath));
                }
            }
        }
    }

    private static void ValidateEnergy(EnergyEv energy, Guid templateId, string property, DiagnosticBag bag)
    {
        var value = energy.Value;
        if (double.IsFinite(value) &&
            value >= EnergyLimits.MinEnergy.Value &&
            value <= EnergyLimits.MaxEnergy.Value)
        {
            return;
        }

        bag.Add(Error(
            ParameterizationDiagnosticCodes.InvalidEnergy,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Energy {value} eV is outside the accepted {EnergyLimits.MinEnergy.Value}..{EnergyLimits.MaxEnergy.Value} eV range."),
            templateId,
            property));
    }

    private static int Length<T>(ImmutableArray<T> values) => values.IsDefault ? 0 : values.Length;

    private static string Key(Guid value) => value.ToString("D", CultureInfo.InvariantCulture);

    private static Diagnostic Error(string code, string message, Guid templateId, string? property) =>
        new(
            code,
            DiagnosticSeverity.Error,
            message,
            new ModelPath("parameterization", templateId, property),
            ParameterizationDiagnosticCodes.HelpFor(code));
}
