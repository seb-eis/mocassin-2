// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// A build-time generated initial lattice.
/// </summary>
/// <remarks>
/// The particle array uses row-major cell order:
/// <c>index = ((a * B + b) * C + c) * PositionCount + P</c>, with <c>a</c> outermost and <c>P</c> in canonical
/// P1 order. Each entry is the compiled particle index of the position. <see cref="Report"/> is build-time and
/// preview data; <see cref="DopingInfo"/> is the canonical package summary that Step 11 stores in the
/// <c>.msl</c> <c>doping_info</c> field.
/// </remarks>
/// <param name="SizeA">The number of unit cells along <c>a</c>.</param>
/// <param name="SizeB">The number of unit cells along <c>b</c>.</param>
/// <param name="SizeC">The number of unit cells along <c>c</c>.</param>
/// <param name="PositionCount">The number of canonical P1 positions per unit cell.</param>
/// <param name="Particles">The immutable compiled particle index of every linearized supercell position.</param>
/// <param name="Report">The build-time doping report of the lattice.</param>
/// <param name="DopingInfo">The canonical priority-ordered doping summary, or an empty string.</param>
public sealed record GeneratedLattice(
    int SizeA,
    int SizeB,
    int SizeC,
    int PositionCount,
    ImmutableArray<byte> Particles,
    DopingReport Report,
    string DopingInfo);
