// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Builds deterministic dense parameterization values for the Step 09 projection fixtures.
/// </summary>
/// <remarks>
/// Apply/defaulting lands in a later Step 09 feature. These values use the real compiled table and rule shapes
/// with a fixed pattern so the golden vectors exercise nonzero energies, site backgrounds, and rule
/// frequencies without depending on the template/apply implementation.
/// </remarks>
internal static class ParameterizationTestValues
{
    /// <summary>
    /// Builds deterministic values for one compiled model.
    /// </summary>
    /// <param name="model">The compiled model whose shapes and default rule frequencies are used.</param>
    /// <returns>The dense values.</returns>
    public static ParameterizationValues FromCompiledModel(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var pairTables = model.Energy.PairTables
            .Select(static table => CreateRow(table.ParticleCount * table.ParticleCount, table.Index))
            .ToImmutableArray();
        var groupSets = model.Energy.ClusterTables
            .Select(static table => CreateRow(table.OccupationCount, table.Index + 17))
            .ToImmutableArray();
        var siteEnergies = CreateRow(
            model.Structure.PositionCount * model.Particles.Count,
            seed: 31);
        var frequencies = model.Transitions.Rules
            .Select(static rule => rule.FrequencyHz)
            .ToImmutableArray();

        return new ParameterizationValues(pairTables, groupSets, siteEnergies, frequencies);
    }

    private static ImmutableArray<double> CreateRow(int count, int seed)
    {
        var values = ImmutableArray.CreateBuilder<double>(count);
        for (var index = 0; index < count; index++)
        {
            values.Add(((seed + index) % 9 - 4) * 0.125);
        }

        return values.MoveToImmutable();
    }
}
