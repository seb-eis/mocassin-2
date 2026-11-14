// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Describes the required, explicitly set, and effective values of one compiled model and parameterization
/// template pair.
/// </summary>
/// <param name="StablePairTableCount">The number of compiled pair tables whose centre reference site is stable.</param>
/// <param name="UnstablePairTableCount">The number of compiled pair tables whose centre reference site is unstable.</param>
/// <param name="PairSlotCount">The number of required pair values over both stable and unstable tables.</param>
/// <param name="ExplicitPairValueCount">The number of pair values explicitly present in the template and inside the required slot space.</param>
/// <param name="GroupTableCount">The number of compiled cluster tables.</param>
/// <param name="GroupSlotCount">The number of canonical cluster occupations, i.e. required group values.</param>
/// <param name="ExplicitGroupValueCount">The number of group values explicitly present in the template and inside the required slot space.</param>
/// <param name="KineticTransitionRuleSetCount">The number of compiled kinetic transition rule collections.</param>
/// <param name="KineticRuleSlotCount">The number of parameterizable kinetic jump rules.</param>
/// <param name="ExplicitRuleFrequencyCount">The number of rule frequencies explicitly present in the template and inside the required slot space.</param>
/// <param name="SiteEnergySlotCount">The number of admissible reference-site/particle site-energy slots.</param>
/// <param name="SiteEnergyOverrideCount">The number of site-energy overrides present in the template.</param>
/// <param name="ChiralPairTableCount">The number of reciprocal chiral pair tables; each contributes one shared value per particle pair.</param>
/// <param name="RequiredSlotCount">The number of required pair, group, and kinetic-rule values.</param>
/// <param name="SetValueCount">The number of explicit pair, group, and kinetic-rule values inside the required slot space.</param>
/// <param name="UnsetValueCount">The number of required slots that have no explicit value.</param>
/// <param name="NonDefaultValueCount">The number of explicit values that differ from their effective default.</param>
/// <param name="EffectiveEnergyMinimumEv">The smallest effective energy in electronvolt, or <see langword="null"/> when the model has no energy slots.</param>
/// <param name="EffectiveEnergyMaximumEv">The largest effective energy in electronvolt, or <see langword="null"/> when the model has no energy slots.</param>
/// <param name="EffectiveFrequencyMinimumHz">The smallest effective attempt frequency in hertz, or <see langword="null"/> when the model has no rules.</param>
/// <param name="EffectiveFrequencyMedianHz">The median effective attempt frequency in hertz, or <see langword="null"/> when the model has no rules.</param>
/// <param name="EffectiveFrequencyMaximumHz">The largest effective attempt frequency in hertz, or <see langword="null"/> when the model has no rules.</param>
/// <param name="EstimatedValueBytes">The estimated size of the dense applied-value arrays in bytes.</param>
/// <param name="IdenticalValueShare">
/// The share of explicit pair and group values taken by their most frequent non-default value, in <c>0..1</c>.
/// Zero when the template has no explicit pair or group values or no non-default value.
/// </param>
/// <param name="NonParameterizableRuleSetCount">The number of Metropolis rule collections; their compiled defaults always remain.</param>
/// <param name="HasExplicitValues">Whether the template contains at least one explicit pair, group, site, or rule value.</param>
/// <remarks>
/// Effective values are the values an apply operation would produce: unset pair and group values are
/// <c>0 eV</c>, unset kinetic rule frequencies use the compiled default, and the site-energy background is the
/// authored model background plus template overrides. The summary does not validate the template; compiled-bound
/// diagnostics belong to the parameterization validation feature.
/// </remarks>
public sealed record ParameterizationSummary(
    int StablePairTableCount,
    int UnstablePairTableCount,
    int PairSlotCount,
    int ExplicitPairValueCount,
    int GroupTableCount,
    int GroupSlotCount,
    int ExplicitGroupValueCount,
    int KineticTransitionRuleSetCount,
    int KineticRuleSlotCount,
    int ExplicitRuleFrequencyCount,
    int SiteEnergySlotCount,
    int SiteEnergyOverrideCount,
    int ChiralPairTableCount,
    int RequiredSlotCount,
    int SetValueCount,
    int UnsetValueCount,
    int NonDefaultValueCount,
    double? EffectiveEnergyMinimumEv,
    double? EffectiveEnergyMaximumEv,
    double? EffectiveFrequencyMinimumHz,
    double? EffectiveFrequencyMedianHz,
    double? EffectiveFrequencyMaximumHz,
    long EstimatedValueBytes,
    double IdenticalValueShare,
    int NonParameterizableRuleSetCount,
    bool HasExplicitValues);
