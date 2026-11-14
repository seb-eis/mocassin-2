// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Contracts.Parameterization;

/// <summary>
/// The dense, quantized numerical values that parameterize a compiled model.
/// </summary>
/// <param name="PairTables">
/// Pair-energy tables in ascending compiled table order. Each table is row-major
/// <c>[centerParticle][partnerParticle]</c> with <c>nParticles</c> including void.
/// </param>
/// <param name="GroupSets">
/// Cluster-energy values in ascending compiled cluster-table order. Each table is in compiled occupation order.
/// </param>
/// <param name="SiteEnergies">
/// The dense <c>[position][particle]</c> site-energy background in ascending P order, row-major, with void at
/// index zero of each row.
/// </param>
/// <param name="TransitionRuleFrequencies">
/// The effective absolute attempt frequency in hertz for each compiled rule in ascending rule order.
/// </param>
/// <remarks>
/// The record contains values only. Stable IDs, template identity and metadata, set headers, unset markers, and
/// runtime settings are deliberately absent so the parameterization signature cannot depend on them. Do not
/// compare records with record equality: nested <see cref="ImmutableArray{T}"/> equality is reference-based.
/// </remarks>
public sealed record ParameterizationValues(
    ImmutableArray<ImmutableArray<double>> PairTables,
    ImmutableArray<ImmutableArray<double>> GroupSets,
    ImmutableArray<double> SiteEnergies,
    ImmutableArray<double> TransitionRuleFrequencies);
