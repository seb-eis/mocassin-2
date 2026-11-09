// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// Describes whether a connector between two chain positions performs an exchange.
/// </summary>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<TransitionLinker>))]
public enum TransitionLinker
{
    /// <summary>
    /// The connector performs a state exchange.
    /// </summary>
    Dynamic = 0,

    /// <summary>
    /// The connector is a geometric offset to the next position without an exchange.
    /// </summary>
    Static = 1,
}
