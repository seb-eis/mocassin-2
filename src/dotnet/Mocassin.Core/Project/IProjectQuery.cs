// SPDX-License-Identifier: MIT

using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Project;

/// <summary>
/// Read-only, same-snapshot composition of particles, structure, and transitions.
/// </summary>
public interface IProjectQuery : IParticleQuery, IStructureQuery, ITransitionQuery
{
}
