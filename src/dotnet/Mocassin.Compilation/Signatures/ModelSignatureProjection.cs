// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Geometry;

namespace Mocassin.Compilation.Signatures;

/// <summary>
/// Computes the canonical Step 08 compiled-model signature.
/// </summary>
/// <remarks>
/// The projection is hand-coded because <c>Mocassin.Compilation</c> cannot reference
/// <c>Mocassin.Contracts</c>. It follows ADR 0010: domain separation, fixed field order, fixed-width
/// integers, float64, normalized <c>-0.0</c>, and no compression, timestamps, runtime settings, or
/// parameterization values. Energy values are excluded; table identities, geometry, codes, and mappings are
/// included.
/// </remarks>
public static class ModelSignatureProjection
{
    /// <summary>
    /// The signature domain prefix.
    /// </summary>
    public const string Domain = "mocassin/model-signature/v1";

    /// <summary>
    /// Computes the canonical projection bytes.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The canonical projection bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public static byte[] ComputeProjection(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            WriteScalar(writer, model.FormatVersion);
            WriteText(writer, model.SymmetryContractVersion);
            WriteText(writer, model.SpaceGroupDataVersion);
            WriteText(writer, model.SpaceGroupDataSha256);
            WriteStructure(writer, model);
            WriteEnergy(writer, model.Energy);
            WriteTransitions(writer, model.Transitions);
            WriteSimulations(writer, model);
        }

        return stream.ToArray();
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

    private static void WriteStructure(BinaryWriter writer, CompiledModel model)
    {
        var structure = model.Structure;
        WriteScalar(writer, structure.PositionCount);
        WriteScalar(writer, structure.SiteCount);
        WriteScalar(writer, structure.EnvironmentCount);
        WriteScalar(writer, structure.CellVectors.Length);
        foreach (var vector in structure.CellVectors)
        {
            WritePoint(writer, vector);
        }

        WriteScalar(writer, structure.Positions.Length);
        foreach (var position in structure.Positions)
        {
            WriteScalar(writer, position.P);
            WriteScalar(writer, position.Encoding.A);
            WriteScalar(writer, position.Encoding.B);
            WriteScalar(writer, position.Encoding.C);
            WriteScalar(writer, position.Encoding.P);
            WriteFraction(writer, position.Fractional);
            WriteScalar(writer, position.SiteIndex);
            WriteText(writer, position.SiteId.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteScalar(writer, (int)position.Stability);
            WriteScalar(writer, unchecked((long)position.AllowedParticleMask));
            WriteScalar(writer, position.EnvironmentIndex);
            WriteScalar(writer, position.OrbitIndex);
            WriteScalar(writer, position.PairLinks.Length);
            foreach (var link in position.PairLinks)
            {
                WriteScalar(writer, link.PairTableIndex);
                WriteScalar(writer, link.TargetP);
                WriteScalar(writer, link.Offset.A);
                WriteScalar(writer, link.Offset.B);
                WriteScalar(writer, link.Offset.C);
                WriteScalar(writer, link.Offset.P);
                WriteScalar(writer, link.DistanceBucketFm);
            }

            WriteScalar(writer, position.ClusterLinks.Length);
            foreach (var link in position.ClusterLinks)
            {
                WriteScalar(writer, link.ClusterTableIndex);
                WriteScalar(writer, link.SiteSlot);
            }
        }

        foreach (var site in structure.Sites)
        {
            WriteScalar(writer, site.Index);
            WriteText(writer, site.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteFraction(writer, site.Fractional);
            WriteScalar(writer, (int)site.Stability);
            WriteText(writer, site.ParticleSetId.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteScalar(writer, site.EnvironmentIndex);
            WriteScalar(writer, site.PositionIndices.Length);
            foreach (var position in site.PositionIndices)
            {
                WriteScalar(writer, position);
            }
        }

        foreach (var environment in structure.Environments)
        {
            WriteScalar(writer, environment.Index);
            WriteScalar(writer, environment.SiteIndex);
            WriteScalar(writer, (int)environment.Stability);
            WriteScalar(writer, unchecked((long)environment.AllowedParticleMask));
            WriteScalar(writer, environment.UpdateParticleIndices.Length);
            foreach (var particle in environment.UpdateParticleIndices)
            {
                WriteScalar(writer, particle);
            }
        }

        WriteScalar(writer, structure.InteractionRange.Length);
        foreach (var value in structure.InteractionRange)
        {
            WriteScalar(writer, value);
        }

        WriteScalar(writer, model.Particles.Count);
        foreach (var particle in model.Particles.Particles)
        {
            WriteScalar(writer, particle.Index);
            WriteText(writer, particle.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteText(writer, particle.Symbol);
            WriteDouble(writer, particle.ChargeElementary);
            WriteScalar(writer, particle.IsVacancy ? 1 : 0);
        }
    }

    private static void WriteEnergy(BinaryWriter writer, CompiledEnergyModel energy)
    {
        WriteScalar(writer, energy.PairGeometries.Length);
        foreach (var geometry in energy.PairGeometries)
        {
            WriteText(writer, geometry.GeometryId);
            WriteText(writer, geometry.EnergyId);
            WriteScalar(writer, geometry.PairTableIndex);
            WriteScalar(writer, geometry.CenterSiteIndex);
            WriteScalar(writer, geometry.CenterP);
            WriteScalar(writer, geometry.PartnerSiteIndex);
            WriteScalar(writer, geometry.PartnerP);
            WriteScalar(writer, geometry.Offset.A);
            WriteScalar(writer, geometry.Offset.B);
            WriteScalar(writer, geometry.Offset.C);
            WriteScalar(writer, geometry.Offset.P);
            WriteScalar(writer, geometry.DistanceBucketFm);
            WriteScalar(writer, geometry.IsChiral ? 1 : 0);
            WriteText(writer, geometry.ChiralPartnerGeometryId ?? string.Empty);
        }

        WriteScalar(writer, energy.PairTables.Length);
        foreach (var table in energy.PairTables)
        {
            WriteScalar(writer, table.Index);
            WriteText(writer, table.EnergyId);
            WriteScalar(writer, table.ParticleCount);
        }

        WriteScalar(writer, energy.ClusterTables.Length);
        foreach (var table in energy.ClusterTables)
        {
            WriteText(writer, table.ClusterEnergyId);
            WriteText(writer, table.ClusterId.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteScalar(writer, table.CenterSiteIndex);
            WriteScalar(writer, table.CenterP);
            WriteScalar(writer, table.SitePIndices.Length);
            foreach (var p in table.SitePIndices)
            {
                WriteScalar(writer, p);
            }

            WriteScalar(writer, table.GeometryEncodings.Length);
            foreach (var encoding in table.GeometryEncodings)
            {
                WriteScalar(writer, encoding.A);
                WriteScalar(writer, encoding.B);
                WriteScalar(writer, encoding.C);
                WriteScalar(writer, encoding.P);
            }

            WriteScalar(writer, table.Occupations.Length);
            foreach (var occupation in table.Occupations)
            {
                WriteScalar(writer, unchecked((long)occupation.CanonicalCode));
                WriteScalar(writer, occupation.CanonicalTuple.Length);
                foreach (var particle in occupation.CanonicalTuple)
                {
                    WriteScalar(writer, particle);
                }

                WriteScalar(writer, occupation.Expansions.Length);
                foreach (var expansion in occupation.Expansions)
                {
                    WriteScalar(writer, unchecked((long)expansion.OccupationCode));
                    WriteScalar(writer, expansion.PermutationIndex);
                }
            }

            WriteScalar(writer, table.ParticleTableMapping.Length);
            foreach (var particle in table.ParticleTableMapping)
            {
                WriteScalar(writer, particle);
            }
        }
    }

    private static void WriteTransitions(BinaryWriter writer, CompiledTransitionModel model)
    {
        WriteScalar(writer, model.Collections.Length);
        foreach (var collection in model.Collections)
        {
            WriteScalar(writer, collection.Index);
            WriteScalar(writer, (int)collection.Transition.Kind);
            WriteText(writer, collection.Transition.Id.ToString("D", CultureInfo.InvariantCulture));
            WriteScalar(writer, (int)collection.Kind);
            WriteScalar(writer, unchecked((long)collection.MobileParticleMask));
            WriteIndices(writer, collection.DirectionIndices);
            WriteIndices(writer, collection.RuleIndices);
        }

        WriteScalar(writer, model.Directions.Length);
        foreach (var direction in model.Directions)
        {
            WriteText(writer, direction.ObjectId);
            WriteScalar(writer, direction.CollectionIndex);
            WriteScalar(writer, direction.PositionId);
            WriteScalar(writer, direction.Path.Length);
            foreach (var encoding in direction.Path)
            {
                WriteScalar(writer, encoding.A);
                WriteScalar(writer, encoding.B);
                WriteScalar(writer, encoding.C);
                WriteScalar(writer, encoding.P);
            }

            WriteScalar(writer, direction.Movement.Length);
            foreach (var movement in direction.Movement)
            {
                WriteFraction(writer, movement);
            }

            WritePoint(writer, direction.PathVector);
            WriteDouble(writer, direction.PathLength);
            WriteScalar(writer, direction.IsInverse ? 1 : 0);
            WriteIndices(writer, direction.EquivalentStartPositions);
        }

        WriteScalar(writer, model.Rules.Length);
        foreach (var rule in model.Rules)
        {
            WriteText(writer, rule.RuleGeometryId);
            WriteScalar(writer, rule.CollectionIndex);
            WriteIndices(writer, rule.DirectionIndices);
            WriteScalar(writer, unchecked((long)rule.State0));
            WriteScalar(writer, unchecked((long)rule.State1));
            WriteScalar(writer, unchecked((long)rule.State2));
            WriteScalar(writer, rule.Movement.Length);
            foreach (var movement in rule.Movement)
            {
                WriteFraction(writer, movement);
            }

            WritePoint(writer, rule.ChargeTransportVector);
            WriteScalar(writer, rule.TrackerOrder.Length);
            foreach (var tracker in rule.TrackerOrder)
            {
                WriteScalar(writer, tracker);
            }

            WriteScalar(writer, rule.IsInverse ? 1 : 0);
        }

        WriteScalar(writer, model.GlobalTrackers.Length);
        foreach (var tracker in model.GlobalTrackers)
        {
            WriteScalar(writer, tracker.Index);
            WriteScalar(writer, tracker.CollectionIndex);
            WriteScalar(writer, tracker.ParticleIndex);
        }

        WriteScalar(writer, model.StaticTrackers.Length);
        foreach (var tracker in model.StaticTrackers)
        {
            WriteScalar(writer, tracker.Index);
            WriteScalar(writer, tracker.PositionIndex);
            WriteScalar(writer, tracker.ParticleIndex);
        }

        WriteIndices(writer, model.JumpCounts);
        WriteIndices(writer, model.JumpOffsets);
        WriteIndices(writer, model.JumpMappings);
    }

    private static void WriteSimulations(BinaryWriter writer, CompiledModel model)
    {
        WriteScalar(writer, model.Simulations.Length);
        foreach (var simulation in model.Simulations)
        {
            WriteText(writer, simulation.Id.Value.ToString("D", CultureInfo.InvariantCulture));
            WriteScalar(writer, (int)simulation.Kind);
            WriteIndices(writer, simulation.ActiveCollectionIndices);
            WriteScalar(writer, simulation.Intent.Kind == Core.Model.Simulations.SimulationKind.Kmc ? 1 : 0);
            WriteScalar(writer, simulation.Trackers.GlobalTrackerIndices.Length);
            foreach (var tracker in simulation.Trackers.GlobalTrackerIndices)
            {
                WriteScalar(writer, tracker);
            }

            WriteScalar(writer, simulation.Trackers.StaticTrackerIndices.Length);
            foreach (var tracker in simulation.Trackers.StaticTrackerIndices)
            {
                WriteScalar(writer, tracker);
            }
        }
    }

    private static void WriteIndices(BinaryWriter writer, System.Collections.Immutable.ImmutableArray<int> values)
    {
        WriteScalar(writer, values.Length);
        foreach (var value in values)
        {
            WriteScalar(writer, value);
        }
    }

    private static void WritePoint(BinaryWriter writer, Cartesian3 value)
    {
        WriteDouble(writer, value.X);
        WriteDouble(writer, value.Y);
        WriteDouble(writer, value.Z);
    }

    private static void WriteFraction(BinaryWriter writer, Fractional3 value)
    {
        WriteDouble(writer, value.A);
        WriteDouble(writer, value.B);
        WriteDouble(writer, value.C);
    }

    private static void WriteText(BinaryWriter writer, string value) => writer.Write(value);

    private static void WriteScalar(BinaryWriter writer, int value) => writer.Write(value);

    private static void WriteScalar(BinaryWriter writer, long value) => writer.Write(value);

    private static void WriteDouble(BinaryWriter writer, double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException("The compiled model contains a non-finite value.");
        }

        var quantized = Math.Round(value, 12, MidpointRounding.AwayFromZero);
        writer.Write(quantized == 0.0 ? 0.0 : quantized);
    }
}
