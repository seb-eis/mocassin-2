// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// User-authored simulation execution flags.
/// </summary>
/// <remarks>
/// The project JSON persists a sorted array of camelCase names; <see cref="None"/> is the empty array. The
/// <c>.msl</c> job payload uses the separate <c>SolverJobFlags</c> word. Kind, pre-run, and dual-DOF flags are
/// derived by the compiler and are not part of this authored enum.
/// </remarks>
[Flags]
[JsonConverter(typeof(ExecutionFlagsJsonConverter))]
[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "ExecutionFlags is the reviewed Step 06 contract name and the suffix is meaningful.",
    Scope = "type",
    Target = "~T:Mocassin.Core.Model.Simulations.ExecutionFlags")]
public enum ExecutionFlags
{
    /// <summary>
    /// No authored execution flag.
    /// </summary>
    None = 0,

    /// <summary>
    /// Skip <c>.mcs</c> state and checkpoint writes.
    /// </summary>
    NoSaving = 1,

    /// <summary>
    /// Disable the KMC jump histogram.
    /// </summary>
    NoJumpLogging = 2,

    /// <summary>
    /// Use the documented fast exponential approximation instead of libm <c>exp</c>.
    /// </summary>
    UseFastExp = 4,

    /// <summary>
    /// Select the canonical RNG-consumption path by disabling the frequency pre-check and optional shortcuts.
    /// </summary>
    Deterministic = 8,
}
