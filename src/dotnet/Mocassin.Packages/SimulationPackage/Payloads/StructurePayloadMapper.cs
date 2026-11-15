// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Maps the compiled P1 structure and its environment links to the model payload.
/// </summary>
internal static class StructurePayloadMapper
{
    /// <summary>
    /// Maps the compiled structure.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The structure payload.</returns>
    /// <exception cref="InvalidOperationException">The compiled indices are inconsistent.</exception>
    public static StructurePayload Map(CompiledModel model)
    {
        var structure = model.Structure;
        var particleCount = model.Particles.Count;
        var positionCount = structure.PositionCount;
        if (positionCount <= 0)
        {
            throw new InvalidOperationException("A compiled model must contain canonical P1 positions.");
        }

        var positions = new PositionPayload[positionCount];
        var environments = new EnvironmentPayload[positionCount];
        for (var index = 0; index < positionCount; index++)
        {
            var position = structure.Positions[index];
            if (position.P != index)
            {
                throw new InvalidOperationException("Compiled positions must be in ascending P order.");
            }

            if (position.EnvironmentIndex < 0 || position.EnvironmentIndex >= structure.Environments.Length)
            {
                throw new InvalidOperationException(
                    $"Position {position.P} references environment {position.EnvironmentIndex} out of range.");
            }

            var environment = structure.Environments[position.EnvironmentIndex];
            positions[index] = MapPosition(position, particleCount);
            environments[index] = new EnvironmentPayload(
                environment.AllowedParticleMask,
                MapPairs(position.PairLinks, positionCount),
                MapClusters(position.ClusterLinks),
                MapParticleIds(environment.UpdateParticleIndices, particleCount, "environment update particle"));
        }

        return new StructurePayload(
            [.. structure.CellVectors.Select(static vector => ImmutableArray.Create(vector.X, vector.Y, vector.Z))],
            [.. positions],
            (uint)positionCount,
            (uint)structure.SiteCount,
            [.. environments],
            structure.InteractionRange,
            checked((uint)(positionCount * (particleCount - 1))),
            (uint)model.Transitions.GlobalTrackers.Length);
    }

    private static PositionPayload MapPosition(CompiledPosition position, int particleCount) =>
        new(
            ImmutableArray.Create(
                position.Encoding.A,
                position.Encoding.B,
                position.Encoding.C,
                position.Encoding.P),
            (uint)position.SiteIndex,
            position.Stability == SiteStability.Stable,
            MapParticleIds(SetParticles(position.AllowedParticleMask), particleCount, "position particle"));

    private static ImmutableArray<PairLinkPayload> MapPairs(
        ImmutableArray<CompiledPairLink> links,
        int positionCount)
    {
        var mapped = new PairLinkPayload[links.Length];
        for (var index = 0; index < links.Length; index++)
        {
            var link = links[index];
            if (link.TargetP < 0 || link.TargetP >= positionCount)
            {
                throw new InvalidOperationException($"Pair link target {link.TargetP} is outside the position range.");
            }

            if (link.PairTableIndex < 0)
            {
                throw new InvalidOperationException("A pair link references a negative table index.");
            }

            if (link.Offset.P != link.TargetP)
            {
                throw new InvalidOperationException("A pair link's offset and target position disagree.");
            }

            mapped[index] = new PairLinkPayload(
                (uint)link.TargetP,
                ImmutableArray.Create(link.Offset.A, link.Offset.B, link.Offset.C),
                (uint)link.PairTableIndex);
        }

        return [.. mapped];
    }

    private static ImmutableArray<ClusterLinkPayload> MapClusters(ImmutableArray<CompiledClusterLink> links)
    {
        var mapped = new ClusterLinkPayload[links.Length];
        for (var index = 0; index < links.Length; index++)
        {
            var link = links[index];
            if (link.ClusterTableIndex < 0 || link.SiteSlot is < 0 or > byte.MaxValue)
            {
                throw new InvalidOperationException("A cluster link is outside the payload range.");
            }

            mapped[index] = new ClusterLinkPayload((uint)link.ClusterTableIndex, (byte)link.SiteSlot);
        }

        return [.. mapped];
    }

    private static ImmutableArray<byte> MapParticleIds(
        ImmutableArray<int> indices,
        int particleCount,
        string description)
    {
        var mapped = new byte[indices.Length];
        for (var index = 0; index < indices.Length; index++)
        {
            var value = indices[index];
            if (value < 0 || value >= particleCount)
            {
                throw new InvalidOperationException($"The {description} index {value} is outside the particle range.");
            }

            mapped[index] = (byte)value;
        }

        return [.. mapped];
    }

    private static ImmutableArray<int> SetParticles(ulong mask)
    {
        var indices = ImmutableArray.CreateBuilder<int>();
        for (var particle = 0; particle < 64; particle++)
        {
            if ((mask & (1UL << particle)) != 0)
            {
                indices.Add(particle);
            }
        }

        return indices.ToImmutable();
    }
}
