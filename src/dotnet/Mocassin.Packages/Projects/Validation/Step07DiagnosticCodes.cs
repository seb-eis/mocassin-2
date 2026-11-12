// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Stable diagnostic codes added by Step 07.
/// </summary>
public static class Step07DiagnosticCodes
{
    /// <summary>
    /// A KMC simulation with transitions has no mobile start particle.
    /// </summary>
    public const string KmcSimulationWithoutMobileStart = "SIM038";

    /// <summary>
    /// The model has KMC transitions but no particle mobile in any KMC simulation.
    /// </summary>
    public const string NoMobileStartParticle = "SIM039";

    /// <summary>
    /// The estimated compiled rule count exceeds the advisory budget.
    /// </summary>
    public const string EstimatedRuleCountExceeded = "SIM040";

    /// <summary>
    /// The estimated energy-table memory exceeds the advisory budget.
    /// </summary>
    public const string EstimatedMemoryExceeded = "SIM041";
}
