// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Energy;

/// <summary>
/// Assigns a site energy to one particle on one reference site.
/// </summary>
/// <param name="ParticleId">The particle that occupies the site.</param>
/// <param name="Energy">The site energy in electronvolt.</param>
public sealed record SiteEnergyEntry(
    ParticleId ParticleId,
    EnergyEv Energy);
