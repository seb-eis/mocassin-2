// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;

namespace Mocassin.Contracts.Packages;

/// <summary>
/// The <c>.msl</c> initial-lattice payload.
/// </summary>
/// <remarks>
/// The payload is only the initial state. It deliberately carries no doping summary: the identical particle
/// array must deduplicate across jobs, and the job payload and <c>jobs.doping_info</c> column record the applied
/// doping provenance. <see cref="Lattice"/> is the row-major particle array with
/// <c>index = ((a * B + b) * C + c) * PositionCount + P</c>.
/// </remarks>
/// <param name="FormatVersion">The payload-encoding version.</param>
/// <param name="LatticeSize">The supercell size <c>[a, b, c]</c> in unit cells.</param>
/// <param name="PositionCount">The number of canonical P1 positions per unit cell.</param>
/// <param name="Lattice">
/// The compiled particle index of every linearized supercell position; length is
/// <c>a × b × c × PositionCount</c>.
/// </param>
[MessagePackObject]
public sealed record LatticePayload(
    [property: Key("format_version")] uint FormatVersion,
    [property: Key("lattice_size")] ImmutableArray<uint> LatticeSize,
    [property: Key("position_count")] uint PositionCount,
    [property: Key("lattice")] ImmutableArray<byte> Lattice);
