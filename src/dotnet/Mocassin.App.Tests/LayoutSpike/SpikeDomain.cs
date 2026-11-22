// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One model domain exercised by the Step 24 layout spike.
/// </summary>
internal enum SpikeDomain
{
    /// <summary>The authored particles, including the pinned implicit void row.</summary>
    Particles,

    /// <summary>The authored particle sets.</summary>
    ParticleSets,

    /// <summary>The reference cell sites.</summary>
    Sites,

    /// <summary>The authored state changes.</summary>
    StateChanges,

    /// <summary>The authored transition chains.</summary>
    Chains,

    /// <summary>The authored kinetic and Metropolis transitions.</summary>
    Transitions,
}
