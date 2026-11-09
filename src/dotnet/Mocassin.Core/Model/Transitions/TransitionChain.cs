// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// An abstract chain of state-change groups and connectors.
/// </summary>
/// <param name="Id">The stable chain identifier.</param>
/// <param name="Name">The user-visible chain name.</param>
/// <param name="Pattern">The abstract mechanism shape.</param>
/// <param name="GroupIds">The ordered state-change-group identifiers, one per position.</param>
/// <param name="Linkers">The ordered connectors between adjacent positions.</param>
/// <param name="GeneratesReverse">Whether the chain generates an automatic inverse rule.</param>
/// <remarks>
/// <see cref="GroupIds"/> and <see cref="Linkers"/> are semantic-order arrays. Canonicalization never re-sorts
/// them.
/// </remarks>
public sealed record TransitionChain(
    TransitionChainId Id,
    string Name,
    TransitionPattern Pattern,
    ImmutableArray<StateChangeGroupId> GroupIds,
    ImmutableArray<TransitionLinker> Linkers,
    bool GeneratesReverse);
