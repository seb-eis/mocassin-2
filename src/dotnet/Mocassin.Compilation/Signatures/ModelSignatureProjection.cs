// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Geometry;

namespace Mocassin.Compilation.Signatures;

/// <summary>
/// Computes the canonical Step 08 compiled-model signature over the ADR 0010 MessagePack projection.
/// </summary>
/// <remarks>
/// The projection is a fixed MessagePack array of nine fields in manifest order. Every struct is a
/// MessagePack array, every integer is fixed-width, every float is float64, and no energy values or runtime
/// settings are included. Energy values, display distances, path vectors/lengths, and diagnostics are
/// inspection-only.
/// </remarks>
public static class ModelSignatureProjection
{
    /// <summary>The signature domain prefix.</summary>
    public const string Domain = "mocassin/model-signature/v1";

    /// <summary>
    /// Computes the canonical projection bytes.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The canonical projection bytes.</returns>
    public static byte[] ComputeProjection(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new CanonicalProjectionWriter(buffer);
        writer.WriteArrayHeader(9);
        writer.WriteUInt64((ulong)model.FormatVersion);
        writer.WriteString(model.SymmetryContractVersion);
        writer.WriteString(model.SpaceGroupDataVersion);
        writer.WriteString(model.SpaceGroupDataSha256);
        WriteParticles(ref writer, model);
        WriteStructure(ref writer, model.Structure);
        WriteEnergy(ref writer, model.Energy);
        WriteTransitions(ref writer, model.Transitions);
        WriteSimulations(ref writer, model);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Computes the domain-separated SHA-256 signature.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The lowercase hexadecimal signature.</returns>
    public static string Compute(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var projection = ComputeProjection(model);
        var domain = Encoding.UTF8.GetBytes(Domain);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(domain);
        hash.AppendData([0]);
        hash.AppendData(projection);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void WriteParticles(ref CanonicalProjectionWriter writer, CompiledModel model)
    {
        writer.WriteArrayHeader(model.Particles.Count);
        foreach (var particle in model.Particles.Particles)
        {
            writer.WriteArrayHeader(5);
            writer.WriteUInt64((ulong)particle.Index);
            writer.WriteString(particle.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteString(particle.Symbol);
            writer.WriteDouble(particle.ChargeElementary);
            writer.WriteBool(particle.IsVacancy);
        }
    }

    private static void WriteStructure(ref CanonicalProjectionWriter writer, CompiledStructureModel structure)
    {
        writer.WriteArrayHeader(6);
        writer.WriteArrayHeader(structure.CellVectors.Length);
        foreach (var vector in structure.CellVectors)
        {
            WritePoint(ref writer, vector);
        }

        writer.WriteDouble(structure.CellVolumeAngstrom3);
        writer.WriteArrayHeader(structure.Sites.Length);
        foreach (var site in structure.Sites)
        {
            writer.WriteArrayHeader(7);
            writer.WriteUInt64((ulong)site.Index);
            writer.WriteString(site.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteFraction(ref writer, site.Fractional);
            writer.WriteInt64((long)site.Stability);
            writer.WriteString(site.ParticleSetId.Value.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteUInt64((ulong)site.EnvironmentIndex);
            WriteIndices(ref writer, site.PositionIndices);
        }

        writer.WriteArrayHeader(structure.Environments.Length);
        foreach (var environment in structure.Environments)
        {
            writer.WriteArrayHeader(5);
            writer.WriteUInt64((ulong)environment.Index);
            writer.WriteInt64(environment.SiteIndex);
            writer.WriteInt64((long)environment.Stability);
            writer.WriteUInt64(environment.AllowedParticleMask);
            WriteIndices(ref writer, environment.UpdateParticleIndices);
        }

        writer.WriteArrayHeader(structure.Positions.Length);
        foreach (var position in structure.Positions)
        {
            writer.WriteArrayHeader(11);
            writer.WriteUInt64((ulong)position.P);
            WriteEncoding(ref writer, position.Encoding);
            WriteFraction(ref writer, position.Fractional);
            writer.WriteUInt64((ulong)position.SiteIndex);
            writer.WriteString(position.SiteId.Value.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteInt64((long)position.Stability);
            writer.WriteUInt64(position.AllowedParticleMask);
            writer.WriteUInt64((ulong)position.EnvironmentIndex);
            writer.WriteUInt64((ulong)position.OrbitIndex);
            writer.WriteArrayHeader(position.PairLinks.Length);
            foreach (var link in position.PairLinks)
            {
                writer.WriteArrayHeader(4);
                writer.WriteUInt64((ulong)link.PairTableIndex);
                writer.WriteUInt64((ulong)link.TargetP);
                WriteEncoding(ref writer, link.Offset);
                writer.WriteUInt64((ulong)link.DistanceBucketFm);
            }

            writer.WriteArrayHeader(position.ClusterLinks.Length);
            foreach (var link in position.ClusterLinks)
            {
                writer.WriteArrayHeader(2);
                writer.WriteUInt64((ulong)link.ClusterTableIndex);
                writer.WriteUInt64((ulong)link.SiteSlot);
            }
        }

        writer.WriteArrayHeader(structure.InteractionRange.Length);
        foreach (var value in structure.InteractionRange)
        {
            writer.WriteUInt64((ulong)value);
        }
    }

    private static void WriteEnergy(ref CanonicalProjectionWriter writer, CompiledEnergyModel energy)
    {
        writer.WriteArrayHeader(3);
        writer.WriteArrayHeader(energy.PairGeometries.Length);
        foreach (var geometry in energy.PairGeometries)
        {
            writer.WriteArrayHeader(11);
            writer.WriteString(geometry.GeometryId);
            writer.WriteString(geometry.EnergyId);
            writer.WriteUInt64((ulong)geometry.PairTableIndex);
            writer.WriteUInt64((ulong)geometry.CenterSiteIndex);
            writer.WriteUInt64((ulong)geometry.CenterP);
            writer.WriteUInt64((ulong)geometry.PartnerSiteIndex);
            writer.WriteUInt64((ulong)geometry.PartnerP);
            WriteEncoding(ref writer, geometry.Offset);
            writer.WriteUInt64((ulong)geometry.DistanceBucketFm);
            writer.WriteBool(geometry.IsChiral);
            if (geometry.ChiralPartnerGeometryId is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteString(geometry.ChiralPartnerGeometryId);
            }
        }

        writer.WriteArrayHeader(energy.PairTables.Length);
        foreach (var table in energy.PairTables)
        {
            writer.WriteArrayHeader(3);
            writer.WriteUInt64((ulong)table.Index);
            writer.WriteString(table.EnergyId);
            writer.WriteUInt64((ulong)table.ParticleCount);
        }

        writer.WriteArrayHeader(energy.ClusterTables.Length);
        foreach (var table in energy.ClusterTables)
        {
            writer.WriteArrayHeader(8);
            writer.WriteString(table.ClusterEnergyId);
            writer.WriteString(table.ClusterId.Value.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteUInt64((ulong)table.CenterSiteIndex);
            writer.WriteUInt64((ulong)table.CenterP);
            WriteIndices(ref writer, table.SitePIndices);
            writer.WriteArrayHeader(table.GeometryEncodings.Length);
            foreach (var encoding in table.GeometryEncodings)
            {
                WriteEncoding(ref writer, encoding);
            }

            writer.WriteArrayHeader(table.Occupations.Length);
            foreach (var occupation in table.Occupations)
            {
                writer.WriteArrayHeader(3);
                writer.WriteUInt64(occupation.CanonicalCode);
                writer.WriteArrayHeader(occupation.CanonicalTuple.Length);
                foreach (var particle in occupation.CanonicalTuple)
                {
                    writer.WriteUInt64((ulong)particle);
                }

                writer.WriteArrayHeader(occupation.Expansions.Length);
                foreach (var expansion in occupation.Expansions)
                {
                    writer.WriteArrayHeader(2);
                    writer.WriteUInt64(expansion.OccupationCode);
                    writer.WriteUInt64((ulong)expansion.PermutationIndex);
                }
            }

            WriteIndices(ref writer, table.ParticleTableMapping);
        }
    }

    private static void WriteTransitions(ref CanonicalProjectionWriter writer, CompiledTransitionModel model)
    {
        writer.WriteArrayHeader(8);
        writer.WriteArrayHeader(model.Collections.Length);
        foreach (var collection in model.Collections)
        {
            writer.WriteArrayHeader(6);
            writer.WriteUInt64((ulong)collection.Index);
            writer.WriteInt64((long)collection.Transition.Kind);
            writer.WriteString(collection.Transition.Id.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteInt64((long)collection.Kind);
            writer.WriteUInt64(collection.MobileParticleMask);
            WriteIndices(ref writer, collection.DirectionIndices);
        }

        writer.WriteArrayHeader(model.Directions.Length);
        foreach (var direction in model.Directions)
        {
            writer.WriteArrayHeader(7);
            writer.WriteString(direction.ObjectId);
            writer.WriteUInt64((ulong)direction.CollectionIndex);
            writer.WriteUInt64((ulong)direction.PositionId);
            writer.WriteArrayHeader(direction.Path.Length);
            foreach (var encoding in direction.Path)
            {
                WriteEncoding(ref writer, encoding);
            }

            WriteMovement(ref writer, direction.Movement);
            writer.WriteBool(direction.IsInverse);
            WriteIndices(ref writer, direction.EquivalentStartPositions);
        }

        writer.WriteArrayHeader(model.Rules.Length);
        foreach (var rule in model.Rules)
        {
            writer.WriteArrayHeader(10);
            writer.WriteString(rule.RuleGeometryId);
            writer.WriteUInt64((ulong)rule.CollectionIndex);
            WriteIndices(ref writer, rule.DirectionIndices);
            writer.WriteUInt64(rule.State0);
            writer.WriteUInt64(rule.State1);
            writer.WriteUInt64(rule.State2);
            WriteMovement(ref writer, rule.Movement);
            WritePoint(ref writer, rule.ChargeTransportVector);
            writer.WriteArrayHeader(rule.TrackerOrder.Length);
            foreach (var tracker in rule.TrackerOrder)
            {
                writer.WriteUInt64(tracker);
            }

            writer.WriteBool(rule.IsInverse);
        }

        writer.WriteArrayHeader(model.GlobalTrackers.Length);
        foreach (var tracker in model.GlobalTrackers)
        {
            writer.WriteArrayHeader(3);
            writer.WriteUInt64((ulong)tracker.Index);
            writer.WriteUInt64((ulong)tracker.CollectionIndex);
            writer.WriteUInt64((ulong)tracker.ParticleIndex);
        }

        writer.WriteArrayHeader(model.StaticTrackers.Length);
        foreach (var tracker in model.StaticTrackers)
        {
            writer.WriteArrayHeader(3);
            writer.WriteUInt64((ulong)tracker.Index);
            writer.WriteUInt64((ulong)tracker.PositionIndex);
            writer.WriteUInt64((ulong)tracker.ParticleIndex);
        }

        WriteIndices(ref writer, model.JumpCounts);
        WriteIndices(ref writer, model.JumpOffsets);
        WriteIndices(ref writer, model.JumpMappings);
    }

    private static void WriteSimulations(ref CanonicalProjectionWriter writer, CompiledModel model)
    {
        writer.WriteArrayHeader(model.Simulations.Length);
        foreach (var simulation in model.Simulations)
        {
            writer.WriteArrayHeader(5);
            writer.WriteString(simulation.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteInt64((long)simulation.Kind);
            WriteIndices(ref writer, simulation.ActiveCollectionIndices);
            WriteIndices(ref writer, simulation.Trackers.GlobalTrackerIndices);
            WriteIndices(ref writer, simulation.Trackers.StaticTrackerIndices);
        }
    }

    private static void WriteEncoding(ref CanonicalProjectionWriter writer, Vector4I encoding)
    {
        writer.WriteArrayHeader(4);
        writer.WriteInt64(encoding.A);
        writer.WriteInt64(encoding.B);
        writer.WriteInt64(encoding.C);
        writer.WriteUInt64((ulong)encoding.P);
    }

    private static void WritePoint(ref CanonicalProjectionWriter writer, Cartesian3 value)
    {
        writer.WriteArrayHeader(3);
        writer.WriteDouble(value.X);
        writer.WriteDouble(value.Y);
        writer.WriteDouble(value.Z);
    }

    private static void WriteFraction(ref CanonicalProjectionWriter writer, Fractional3 value)
    {
        writer.WriteArrayHeader(3);
        writer.WriteDouble(value.A);
        writer.WriteDouble(value.B);
        writer.WriteDouble(value.C);
    }

    private static void WriteMovement(
        ref CanonicalProjectionWriter writer,
        System.Collections.Immutable.ImmutableArray<Fractional3> movement)
    {
        writer.WriteArrayHeader(movement.Length);
        foreach (var value in movement)
        {
            WriteFraction(ref writer, value);
        }
    }

    private static void WriteIndices(
        ref CanonicalProjectionWriter writer,
        System.Collections.Immutable.ImmutableArray<int> values)
    {
        writer.WriteArrayHeader(values.Length);
        foreach (var value in values)
        {
            writer.WriteUInt64((ulong)value);
        }
    }
}
