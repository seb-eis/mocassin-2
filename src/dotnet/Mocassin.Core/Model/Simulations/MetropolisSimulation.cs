// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// A Metropolis Monte Carlo simulation.
/// </summary>
public sealed record MetropolisSimulation : Simulation
{
    /// <inheritdoc />
    public override SimulationKind Kind => SimulationKind.Mmc;

    /// <summary>
    /// Gets the equilibrium break tolerance; 0 disables the break check.
    /// </summary>
    public double BreakTolerance { get; init; }

    /// <summary>
    /// Gets the equilibrium break sample length in MCSP.
    /// </summary>
    public long BreakSampleLengthMcsp { get; init; } = 1000;

    /// <summary>
    /// Gets the equilibrium break sample interval in MCSP.
    /// </summary>
    public long BreakSampleIntervalMcsp { get; init; } = 100;

    /// <summary>
    /// Gets the result sample length in MCSP.
    /// </summary>
    public long ResultSampleMcsp { get; init; } = 200;
}
