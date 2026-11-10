// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// The concrete simulation kind, derived from the simulation record type.
/// </summary>
/// <remarks>
/// The persisted project JSON writes <c>kmc</c> and <c>mmc</c>. The kind is never user-settable on a
/// simulation; switching kind is a delete-and-create editor operation.
/// </remarks>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<SimulationKind>))]
public enum SimulationKind
{
    /// <summary>
    /// A kinetic Monte Carlo simulation.
    /// </summary>
    Kmc = 0,

    /// <summary>
    /// A Metropolis Monte Carlo simulation.
    /// </summary>
    Mmc = 1,
}
