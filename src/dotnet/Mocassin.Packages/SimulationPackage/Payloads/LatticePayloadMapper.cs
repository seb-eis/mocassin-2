// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Contracts.Packages;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the real generated lattice to the persisted lattice payload.
/// </summary>
internal static class LatticePayloadMapper
{
    /// <summary>
    /// Maps a generated initial lattice.
    /// </summary>
    /// <param name="lattice">The generated lattice.</param>
    /// <returns>The lattice payload.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lattice"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The lattice dimensions and particle array disagree.</exception>
    public static LatticePayload Map(GeneratedLattice lattice)
    {
        ArgumentNullException.ThrowIfNull(lattice);
        if (lattice.SizeA <= 0 || lattice.SizeB <= 0 || lattice.SizeC <= 0 || lattice.PositionCount <= 0)
        {
            throw new InvalidOperationException("The generated lattice dimensions must be positive.");
        }

        var expected = (long)lattice.SizeA * lattice.SizeB * lattice.SizeC * lattice.PositionCount;
        if (expected > int.MaxValue)
        {
            throw new InvalidOperationException(
                "The linearized lattice length exceeds the signed 32-bit index limit.");
        }

        if (lattice.Particles.IsDefault || lattice.Particles.Length != expected)
        {
            throw new InvalidOperationException(
                $"The lattice has {lattice.Particles.Length} particles, expected {expected}.");
        }

        return new LatticePayload(
            PackageFormatConstants.LatticePayloadFormatVersion,
            ImmutableArray.Create(
                (uint)lattice.SizeA,
                (uint)lattice.SizeB,
                (uint)lattice.SizeC),
            (uint)lattice.PositionCount,
            lattice.Particles);
    }
}
