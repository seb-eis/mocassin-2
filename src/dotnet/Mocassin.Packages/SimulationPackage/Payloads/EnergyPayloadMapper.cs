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
                MapCodeExpansions(table, particleCount),
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

    /// <summary>
    /// Builds the raw-code resolution map from the compiled expansion data without a second symmetry pass.
    /// </summary>
    /// <param name="table">The compiled cluster table.</param>
    /// <param name="particleCount">The compiled particle count including void.</param>
    /// <returns>
    /// <see langword="null"/> when every orbit is a singleton; otherwise the strictly ascending map.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// An expansion code is duplicated across orbits, a canonical code is missing from its own map, or a code
    /// is out of bounds.
    /// </exception>
    internal static CodeExpansionPayload? MapCodeExpansions(CompiledClusterTable table, int particleCount)
    {
        var pairs = new List<(ulong Code, uint Index)>();
        for (var index = 0; index < table.Occupations.Length; index++)
        {
            foreach (var expansion in table.Occupations[index].Expansions)
            {
                pairs.Add((expansion.OccupationCode, checked((uint)index)));
            }
        }

        pairs.Sort(static (left, right) => left.Code.CompareTo(right.Code));
        var codes = new List<ulong>(pairs.Count);
        var indices = new List<uint>(pairs.Count);
        foreach (var pair in pairs)
        {
            if (codes.Count > 0 && codes[^1] == pair.Code)
            {
                if (indices[^1] != pair.Index)
                {
                    throw new InvalidOperationException(
                        $"Cluster table '{table.ClusterEnergyId}' claims occupation code {pair.Code} for two orbits.");
                }

                continue;
            }

            if (pair.Code >> 60 != 0)
            {
                throw new InvalidOperationException(
                    $"Cluster table '{table.ClusterEnergyId}' has an expansion code with bits above the ten slots.");
            }

            for (var slot = 0; slot < 10; slot++)
            {
                var particle = (int)((pair.Code >> (6 * slot)) & 0x3F);
                if (particle >= particleCount)
                {
                    throw new InvalidOperationException(
                        $"Cluster table '{table.ClusterEnergyId}' has an expansion code outside the particle range.");
                }
            }

            codes.Add(pair.Code);
            indices.Add(pair.Index);
        }

        for (var index = 0; index < table.Occupations.Length; index++)
        {
            var canonicalCode = table.Occupations[index].CanonicalCode;
            var position = codes.BinarySearch(canonicalCode);
            if (position < 0 || indices[position] != index)
            {
                throw new InvalidOperationException(
                    $"Cluster table '{table.ClusterEnergyId}' is missing the canonical self-entry for code {canonicalCode}.");
            }
        }

        if (codes.Count == table.Occupations.Length)
        {
            return null;
        }

        return new CodeExpansionPayload([.. codes], [.. indices]);
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
