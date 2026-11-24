// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.Usage;

/// <summary>
/// The kind of object that references a model object.
/// </summary>
internal enum ModelUsageKind
{
    /// <summary>
    /// A particle set that lists a particle.
    /// </summary>
    ParticleSet,

    /// <summary>
    /// A state change that names a donor or acceptor particle.
    /// </summary>
    StateChange,

    /// <summary>
    /// A kinetic or Metropolis transition that reaches the object through its chain.
    /// </summary>
    Transition,

    /// <summary>
    /// A reference cell site that selects a particle set.
    /// </summary>
    CellSite,

    /// <summary>
    /// An occupation exchange at a site or between particles.
    /// </summary>
    OccupationExchange,

    /// <summary>
    /// A doping that references an occupation exchange.
    /// </summary>
    Doping,

    /// <summary>
    /// A stable or unstable environment, a filter, a cluster centre, or a site-energy set.
    /// </summary>
    EnergyReference,
}
