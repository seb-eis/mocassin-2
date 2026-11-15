// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Energy;
using Mocassin.Contracts.Packages;
using Mocassin.Contracts.Parameterization;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the applied pair, cluster, and site-energy values to the model payload.
/// </summary>
internal static class EnergyPayloadMapper
{
    /// <summary>
    /// Maps the applied energy values.
    /// </summary>
    /// <param name="energy">The compiled energy model.</param>
    /// <param name="values">The applied parameterization values.</param>
    /// <param name="positionCount">The canonical P1 position count.</param>
    /// <param name="particleCount">The compiled particle count including void.</param>
    /// <returns>The energy payload.</returns>
    /// <exception cref="InvalidOperationException">The compiled model and applied values disagree.</exception>
    public static EnergyPayload Map(
        CompiledEnergyModel energy,
        ParameterizationValues values,
        int positionCount,
        int particleCount)
    {
        if (values.PairTables.IsDefault || values.PairTables.Length != energy.PairTables.Length)
        {
            throw new InvalidOperationException("The applied pair-table count does not match the compiled model.");
        }

        if (values.GroupSets.IsDefault || values.GroupSets.Length != energy.ClusterTables.Length)
        {
            throw new InvalidOperationException("The applied cluster-table count does not match the compiled model.");
        }

        var pairTables = new PairTablePayload[energy.PairTables.Length];
        for (var index = 0; index < pairTables.Length; index++)
        {
            var table = energy.PairTables[index];
            var tableValues = values.PairTables[index];
            if (tableValues.Length != table.ParticleCount * table.ParticleCount)
            {
                throw new InvalidOperationException(
                    $"Pair table {index} has {tableValues.Length} values, expected {table.ParticleCount * table.ParticleCount}.");
            }

            pairTables[index] = new PairTablePayload(table.EnergyId, tableValues);
        }

        var clusterTables = new ClusterTablePayload[energy.ClusterTables.Length];
        for (var index = 0; index < clusterTables.Length; index++)
        {
            var table = energy.ClusterTables[index];
            var tableValues = values.GroupSets[index];
            if (tableValues.Length != table.Occupations.Length)
            {
                throw new InvalidOperationException(
                    $"Cluster table {index} has {tableValues.Length} values, expected {table.Occupations.Length}.");
            }

            clusterTables[index] = new ClusterTablePayload(
                table.ClusterEnergyId,
                [.. table.Occupations.Select(static occupation => occupation.CanonicalCode)],
                tableValues,
                MapParticleMapping(table.ParticleTableMapping));
        }

        var expectedBackground = positionCount * particleCount;
        if (values.SiteEnergies.IsDefault || values.SiteEnergies.Length != expectedBackground)
        {
            throw new InvalidOperationException(
                $"The applied site-energy background has {values.SiteEnergies.Length} values, expected {expectedBackground}.");
        }

        return new EnergyPayload([.. pairTables], [.. clusterTables], values.SiteEnergies);
    }

    private static ImmutableArray<byte> MapParticleMapping(ImmutableArray<int> mapping)
    {
        var values = new byte[mapping.Length];
        for (var index = 0; index < mapping.Length; index++)
        {
            var value = mapping[index];
            if (value is < 0 or > byte.MaxValue)
            {
                throw new InvalidOperationException("A cluster particle-table mapping is outside the byte range.");
            }

            values[index] = (byte)value;
        }

        return [.. values];
    }
}
