// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Holds the sparse site-energy entries of one reference site.
/// </summary>
/// <param name="SiteId">The reference site owning the entries.</param>
/// <param name="Entries">
/// The sparse particle/energy entries. Missing entries default to <c>0 eV</c>. At most one entry per
/// <see cref="ParticleId"/> is valid; canonical serialization sorts entries by particle identifier.
/// </param>
public sealed record SiteEnergySet(
    CellSiteId SiteId,
    ImmutableArray<SiteEnergyEntry> Entries);
