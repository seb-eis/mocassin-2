// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Defines the sampling range and filters shared by all stable reference sites.
/// </summary>
/// <param name="Cutoff">The maximum interaction range in ångström.</param>
/// <param name="Filters">The symmetric distance filters applied to sampled pairs.</param>
/// <remarks>
/// A missing <c>stableEnvironment</c> project section loads <see cref="Default"/>. The default value is omitted
/// when serialized so projects without authored energy data keep their previous canonical hashes.
/// </remarks>
public sealed record StableEnvironment(
    LengthAngstrom Cutoff,
    ImmutableArray<InteractionFilter> Filters)
{
    /// <summary>
    /// Gets the default environment: a <c>1.0 Å</c> cutoff with no filters.
    /// </summary>
    public static StableEnvironment Default { get; } = new(new LengthAngstrom(1.0), []);
}
