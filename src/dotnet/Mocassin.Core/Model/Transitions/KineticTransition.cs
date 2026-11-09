// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// A kinetic transition bound to an abstract chain and a reference path.
/// </summary>
/// <param name="Id">The stable kinetic-transition identifier.</param>
/// <param name="Name">The user-visible transition name.</param>
/// <param name="ChainId">The abstract chain used by the transition.</param>
/// <param name="ReferencePath">The ordered reference path in unit-cell fractional coordinates.</param>
/// <param name="ReverseTransitionId">The optional explicitly defined reverse transition.</param>
/// <remarks>
/// <see cref="ReferencePath"/> is a semantic-order array and is never re-sorted by canonicalization.
/// </remarks>
public sealed record KineticTransition(
    KineticTransitionId Id,
    string Name,
    TransitionChainId ChainId,
    ImmutableArray<Fractional3> ReferencePath,
    KineticTransitionId? ReverseTransitionId);
