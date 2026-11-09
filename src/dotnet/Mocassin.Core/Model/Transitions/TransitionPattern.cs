// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// The abstract mechanism shape of a transition chain.
/// </summary>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<TransitionPattern>))]
public enum TransitionPattern
{
    /// <summary>
    /// A two-position Metropolis exchange with one dynamic linker.
    /// </summary>
    Metropolis2 = 0,

    /// <summary>
    /// A three-position migration with two dynamic linkers.
    /// </summary>
    Migration3 = 1,

    /// <summary>
    /// A five-position interstitialcy with four dynamic linkers.
    /// </summary>
    Interstitialcy5 = 2,

    /// <summary>
    /// A seven-position interstitialcy with six dynamic linkers.
    /// </summary>
    Interstitialcy7 = 3,

    /// <summary>
    /// A five-position vehicle with the <c>D,S,S,D</c> linker sequence.
    /// </summary>
    Vehicle5 = 4,

    /// <summary>
    /// A six-position vehicle with the <c>D,D,S,D,D</c> linker sequence.
    /// </summary>
    Vehicle6 = 5,

    /// <summary>
    /// An explicitly validated two-to-eight-position chain.
    /// </summary>
    Custom = 6,
}
