// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Applies the canonical simulation values used before saving and hashing.
/// </summary>
/// <remarks>
/// <c>minSuccessRate</c> 0 canonicalizes to <see langword="null"/>, an empty <c>rngSeed</c> canonicalizes to
/// <see langword="null"/>, and KMC <c>normalizationEnergyEv</c> 0.0 canonicalizes to <see langword="null"/>.
/// Default transition arrays are treated as empty. Transition order is never changed.
/// </remarks>
internal static class SimulationCanonicalizer
{
    /// <summary>
    /// Normalizes one simulation without changing its authored transition order.
    /// </summary>
    /// <param name="simulation">The simulation to normalize.</param>
    /// <returns>The canonical simulation value.</returns>
    public static Simulation Normalize(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        return simulation switch
        {
            KineticSimulation kinetic => kinetic with
            {
                MinSuccessRate = NormalizeZero(kinetic.MinSuccessRate),
                RngSeed = NormalizeSeed(kinetic.RngSeed),
                NormalizationEnergyEv = NormalizeZero(kinetic.NormalizationEnergyEv),
                TransitionIds = kinetic.TransitionIds.IsDefault ? [] : kinetic.TransitionIds,
            },
            MetropolisSimulation metropolis => metropolis with
            {
                MinSuccessRate = NormalizeZero(metropolis.MinSuccessRate),
                RngSeed = NormalizeSeed(metropolis.RngSeed),
                TransitionIds = metropolis.TransitionIds.IsDefault ? [] : metropolis.TransitionIds,
            },
            _ => throw new InvalidOperationException(
                $"Unsupported simulation record type '{simulation.GetType().Name}'."),
        };
    }

    private static double? NormalizeZero(double? value) => value == 0.0 ? null : value;

    private static string? NormalizeSeed(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;
}
