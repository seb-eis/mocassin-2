// SPDX-License-Identifier: MIT

using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// One job descriptor paired with its generated initial lattice.
/// </summary>
/// <param name="Descriptor">The expanded job descriptor.</param>
/// <param name="Lattice">The generated initial lattice.</param>
public sealed record JobPackage(JobDescriptor Descriptor, GeneratedLattice Lattice);
