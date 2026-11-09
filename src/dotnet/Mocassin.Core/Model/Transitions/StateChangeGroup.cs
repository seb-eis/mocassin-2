// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// An ordered set of state changes that share the same role at one chain position.
/// </summary>
/// <param name="Id">The stable state-change-group identifier.</param>
/// <param name="Name">The user-visible group name.</param>
/// <param name="StateChangeIds">The ordered state-change identifiers.</param>
/// <remarks>
/// The order of <see cref="StateChangeIds"/> is semantic: it describes the group role order and is never
/// re-sorted by canonicalization.
/// </remarks>
public sealed record StateChangeGroup(
    StateChangeGroupId Id,
    string Name,
    ImmutableArray<StateChangeId> StateChangeIds);
