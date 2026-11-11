// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace Mocassin.Contracts.Solver;

/// <summary>
/// The <c>.msl</c> solver job flag word.
/// </summary>
/// <remarks>
/// Bits 0–3 are the user-authored flags persisted in the project. Bits 4–7 are compiler- or job-builder-derived:
/// <see cref="UseDualDofCorrection"/> is owned by the compiler/optimizer, <see cref="UsePrerun"/> is derived from
/// a positive KMC pre-run, and the kind bits are derived from the simulation record. These bit values are a
/// clean-break contract and do not reuse the original flag positions.
/// </remarks>
[Flags]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "SolverJobFlags is the reviewed Step 06 contract name and the suffix is meaningful.",
    Scope = "type",
    Target = "~T:Mocassin.Contracts.Solver.SolverJobFlags")]
public enum SolverJobFlags : uint
{
    /// <summary>
    /// No solver job flag.
    /// </summary>
    None = 0u,

    /// <summary>
    /// Skip <c>.mcs</c> state and checkpoint writes.
    /// </summary>
    NoSaving = 1u << 0,

    /// <summary>
    /// Disable the KMC jump histogram.
    /// </summary>
    NoJumpLogging = 1u << 1,

    /// <summary>
    /// Use the documented fast exponential approximation instead of libm <c>exp</c>.
    /// </summary>
    UseFastExp = 1u << 2,

    /// <summary>
    /// Select the canonical RNG-consumption path by disabling the frequency pre-check and optional shortcuts.
    /// </summary>
    Deterministic = 1u << 3,

    /// <summary>
    /// Apply the dual-degree-of-freedom KMC time-step correction.
    /// </summary>
    UseDualDofCorrection = 1u << 4,

    /// <summary>
    /// Run the KMC pre-run/normalization phase.
    /// </summary>
    UsePrerun = 1u << 5,

    /// <summary>
    /// The job is a kinetic Monte Carlo simulation.
    /// </summary>
    KmcSimulation = 1u << 6,

    /// <summary>
    /// The job is a Metropolis Monte Carlo simulation.
    /// </summary>
    MmcSimulation = 1u << 7,
}
