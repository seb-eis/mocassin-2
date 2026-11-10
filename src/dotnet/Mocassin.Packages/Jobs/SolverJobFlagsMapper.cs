// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Solver;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Maps authored simulation flags and derived job intent to the <c>.msl</c> <see cref="SolverJobFlags"/> word.
/// </summary>
/// <remarks>
/// This is the composition boundary: the compiler carries the derived intent, the package/job builder composes
/// the word, and <c>Mocassin.Contracts</c> owns the bits. <c>Mocassin.Compilation</c> does not reference
/// <c>Mocassin.Contracts</c>.
/// </remarks>
public static class SolverJobFlagsMapper
{
    /// <summary>
    /// Maps the four user-authored execution flags to their solver bits.
    /// </summary>
    /// <param name="flags">The authored execution flags.</param>
    /// <returns>The authored solver bits only.</returns>
    public static SolverJobFlags FromAuthored(ExecutionFlags flags)
    {
        var result = SolverJobFlags.None;
        if ((flags & ExecutionFlags.NoSaving) != 0)
        {
            result |= SolverJobFlags.NoSaving;
        }

        if ((flags & ExecutionFlags.NoJumpLogging) != 0)
        {
            result |= SolverJobFlags.NoJumpLogging;
        }

        if ((flags & ExecutionFlags.UseFastExp) != 0)
        {
            result |= SolverJobFlags.UseFastExp;
        }

        if ((flags & ExecutionFlags.Deterministic) != 0)
        {
            result |= SolverJobFlags.Deterministic;
        }

        return result;
    }

    /// <summary>
    /// Composes the complete solver flag word from authored flags and derived job intent.
    /// </summary>
    /// <param name="authored">The authored execution flags.</param>
    /// <param name="kind">The simulation kind.</param>
    /// <param name="preRunMcsp">The KMC pre-run MCSP; 0 disables the pre-run.</param>
    /// <param name="useDualDofCorrection">Whether the compiler/optimizer enables the dual-DOF correction.</param>
    /// <returns>The composed solver flag word.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is unknown or
    /// <paramref name="preRunMcsp"/> is negative.</exception>
    public static SolverJobFlags Compose(
        ExecutionFlags authored,
        SimulationKind kind,
        long preRunMcsp,
        bool useDualDofCorrection)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(preRunMcsp);
        if (kind is not SimulationKind.Kmc and not SimulationKind.Mmc)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown simulation kind.");
        }

        var result = FromAuthored(authored);
        if (kind == SimulationKind.Kmc)
        {
            if (preRunMcsp > 0)
            {
                result |= SolverJobFlags.UsePrerun;
            }

            if (useDualDofCorrection)
            {
                result |= SolverJobFlags.UseDualDofCorrection;
            }

            result |= SolverJobFlags.KmcSimulation;
        }
        else
        {
            result |= SolverJobFlags.MmcSimulation;
        }

        return result;
    }

    /// <summary>
    /// Projects the authored subset out of a solver flag word.
    /// </summary>
    /// <param name="flags">The solver flag word.</param>
    /// <returns>The authored execution flags; derived bits are ignored.</returns>
    public static ExecutionFlags ToAuthored(SolverJobFlags flags)
    {
        var result = ExecutionFlags.None;
        if ((flags & SolverJobFlags.NoSaving) != 0)
        {
            result |= ExecutionFlags.NoSaving;
        }

        if ((flags & SolverJobFlags.NoJumpLogging) != 0)
        {
            result |= ExecutionFlags.NoJumpLogging;
        }

        if ((flags & SolverJobFlags.UseFastExp) != 0)
        {
            result |= ExecutionFlags.UseFastExp;
        }

        if ((flags & SolverJobFlags.Deterministic) != 0)
        {
            result |= ExecutionFlags.Deterministic;
        }

        return result;
    }
}
