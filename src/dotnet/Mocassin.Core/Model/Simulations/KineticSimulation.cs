// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// A kinetic Monte Carlo simulation.
/// </summary>
public sealed record KineticSimulation : Simulation
{
    /// <inheritdoc />
    public override SimulationKind Kind => SimulationKind.Kmc;

    /// <summary>
    /// Gets the relaxation or normalization pre-run in MCSP; 0 disables it.
    /// </summary>
    public long PreRunMcsp { get; init; } = 100;

    /// <summary>
    /// Gets the electric-field modulus in V/m.
    /// </summary>
    public double ElectricFieldModulusVm { get; init; } = 1.0e7;

    /// <summary>
    /// Gets the fractional electric-field direction.
    /// </summary>
    public Fractional3 ElectricFieldDirection { get; init; } = new(1.0, 0.0, 0.0);

    /// <summary>
    /// Gets the fixed normalization energy in eV, or <see langword="null"/> when normalization is disabled.
    /// </summary>
    public double? NormalizationEnergyEv { get; init; }

    /// <summary>
    /// Gets the simulation-level maximum attempt frequency in Hz, or <see langword="null"/> to use the rules.
    /// </summary>
    public double? MaxAttemptFrequencyHz { get; init; }
}
