// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Projects.Validation.Diagnostics;

/// <summary>
/// Descriptors for the diagnostic codes emitted by Step 07 validation stages.
/// </summary>
internal static class Step07Descriptors
{
    /// <summary>
    /// Gets the Step 07 descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> All { get; } =
    [
        new(
            Step07DiagnosticCodes.KmcSimulationWithoutMobileStart,
            DiagnosticSeverity.Warning,
            "Step 07 — validation and diagnostics",
            "A KMC simulation with transitions has no mobile start particle",
            "Add a mobile particle to the simulation's start pool or remove the simulation.",
            "#sim038"),
        new(
            Step07DiagnosticCodes.NoMobileStartParticle,
            DiagnosticSeverity.Warning,
            "Step 07 — validation and diagnostics",
            "No particle is mobile in any KMC simulation",
            "Make at least one particle mobile in a KMC simulation so the model can start.",
            "#sim039"),
        new(
            Step07DiagnosticCodes.EstimatedRuleCountExceeded,
            DiagnosticSeverity.Error,
            "Step 07 — validation and diagnostics",
            "The estimated compiled rule count exceeds the advisory budget",
            "Reduce the transition or particle-set count, or raise MaxEstimatedRuleCount knowingly.",
            "#sim040"),
        new(
            Step07DiagnosticCodes.EstimatedMemoryExceeded,
            DiagnosticSeverity.Error,
            "Step 07 — validation and diagnostics",
            "The estimated energy-table memory exceeds the advisory budget",
            "Reduce clusters or table sizes, or raise MaxEstimatedMemoryBytes knowingly.",
            "#sim041"),
    ];
}
