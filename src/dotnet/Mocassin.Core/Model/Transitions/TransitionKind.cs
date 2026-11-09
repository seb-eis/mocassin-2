// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// Identifies whether a transition belongs to the kinetic or Metropolis simulation kind.
/// </summary>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<TransitionKind>))]
public enum TransitionKind
{
    /// <summary>
    /// A kinetic Monte Carlo transition with a reference path.
    /// </summary>
    Kinetic = 0,

    /// <summary>
    /// A Metropolis Monte Carlo exchange between two reference sites.
    /// </summary>
    Metropolis = 1,
}
