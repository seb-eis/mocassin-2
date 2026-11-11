// SPDX-License-Identifier: MIT

using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Project;

/// <summary>
/// Read-only, same-snapshot composition of particles, structure, transitions, energy, lattice, and simulation
/// data.
/// </summary>
public interface IProjectQuery :
    IParticleQuery,
    IStructureQuery,
    ITransitionQuery,
    IEnergyQuery,
    ILatticeQuery,
    ISimulationQuery
{
}
