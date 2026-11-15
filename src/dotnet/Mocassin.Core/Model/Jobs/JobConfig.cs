// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Jobs;

/// <summary>
/// One job configuration with its lattice request, dopings, and optional setting overrides.
/// </summary>
/// <param name="Id">The stable configuration identifier.</param>
/// <param name="Name">The user-visible configuration name.</param>
/// <param name="Lattice">The requested supercell size.</param>
/// <param name="Overrides">The configuration-level setting overrides, or <see langword="null"/> to inherit.</param>
/// <param name="Dopings">The requested doping fractions in authored order.</param>
public sealed record JobConfig(
    JobConfigId Id,
    string Name,
    LatticeRequest Lattice,
    JobSettings? Overrides = null,
    ImmutableArray<DopingValue>? Dopings = null);
