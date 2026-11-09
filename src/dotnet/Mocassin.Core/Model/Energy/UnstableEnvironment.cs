// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Defines the sampling range and one-directional filters for one unstable reference site.
/// </summary>
/// <param name="SiteId">The unstable reference site that owns the environment.</param>
/// <param name="Cutoff">The maximum interaction range in ångström.</param>
/// <param name="Filters">The one-directional distance filters applied to unstable-to-stable pairs.</param>
public sealed record UnstableEnvironment(
    CellSiteId SiteId,
    LengthAngstrom Cutoff,
    ImmutableArray<InteractionFilter> Filters);
