// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// Read-only access to the authored lattice and doping domain of a project snapshot.
/// </summary>
public interface ILatticeQuery
{
    /// <summary>
    /// Gets the authored occupation exchanges.
    /// </summary>
    public ImmutableArray<OccupationExchange> OccupationExchanges { get; }

    /// <summary>
    /// Gets the authored dopings in ascending priority order.
    /// </summary>
    public ImmutableArray<Doping> Dopings { get; }

    /// <summary>
    /// Gets the derived default building block.
    /// </summary>
    /// <remarks>
    /// A stable site uses the first particle of its particle set; an unstable site uses the implicit void
    /// particle. The value is recomputed from the snapshot and is never persisted.
    /// </remarks>
    public ImmutableArray<BuildingBlockEntry> DefaultBuildingBlock { get; }

    /// <summary>
    /// Finds an authored occupation exchange.
    /// </summary>
    /// <param name="id">The occupation-exchange identifier.</param>
    /// <returns>The matching exchange, or <see langword="null"/> when the identifier is unknown.</returns>
    public OccupationExchange? Find(OccupationExchangeId id);

    /// <summary>
    /// Finds an authored doping.
    /// </summary>
    /// <param name="id">The doping identifier.</param>
    /// <returns>The matching doping, or <see langword="null"/> when the identifier is unknown.</returns>
    public Doping? Find(DopingId id);
}
