// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// An ordered doping definition that references one primary and an optional dependent occupation exchange.
/// </summary>
/// <param name="Id">The stable doping identifier.</param>
/// <param name="Name">The user-visible doping name.</param>
/// <param name="Priority">The unique application priority in <c>0..9999</c>.</param>
/// <param name="PrimaryExchangeId">The exchange that defines the requested doping fraction.</param>
/// <param name="DependentExchangeId">The optional exchange applied to maintain charge neutrality.</param>
/// <param name="ChargeCompensation">Whether the dependent exchange compensates the primary charge delta.</param>
/// <remarks>
/// Dopings are applied in ascending <see cref="Priority"/> order. The fraction and the integer counts belong to
/// Step 10; Step 06 validates only the model-level shape and charge signs.
/// </remarks>
public sealed record Doping(
    DopingId Id,
    string Name,
    int Priority,
    OccupationExchangeId PrimaryExchangeId,
    OccupationExchangeId? DependentExchangeId,
    bool ChargeCompensation);
