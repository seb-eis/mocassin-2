// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Read-only access to the authored simulation domain of a project snapshot.
/// </summary>
public interface ISimulationQuery
{
    /// <summary>
    /// Gets the authored simulations in stable identifier order.
    /// </summary>
    public ImmutableArray<Simulation> Simulations { get; }

    /// <summary>
    /// Finds an authored simulation.
    /// </summary>
    /// <param name="id">The simulation identifier.</param>
    /// <returns>The matching simulation, or <see langword="null"/> when the identifier is unknown.</returns>
    public Simulation? Find(SimulationId id);
}
