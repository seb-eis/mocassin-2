// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using MessagePack;
using Mocassin.Contracts.Packages;

namespace Mocassin.Contracts.Tests.Packages;

/// <summary>
/// Verifies that every package payload DTO exposes the documented map keys and value shapes.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class PackagePayloadDtoTests
{
    [Fact]
    public void PayloadVersionConstantsAreOne()
    {
        Assert.Equal(1u, PackageFormatConstants.ModelPayloadFormatVersion);
        Assert.Equal(1u, PackageFormatConstants.JobPayloadFormatVersion);
        Assert.Equal(1u, PackageFormatConstants.LatticePayloadFormatVersion);
    }

    [Fact]
    public void ClusterTablePayloadPreservesTheFourAndFiveArgumentSurface()
    {
        var expansions = new CodeExpansionPayload(
            ImmutableArray.Create(1UL, 2UL),
            ImmutableArray.Create(0U, 1U));
        var five = new ClusterTablePayload(
            "cluster",
            ImmutableArray.Create(1UL, 2UL),
            expansions,
            ImmutableArray.Create(0.0, 0.5),
            ImmutableArray.Create<byte>(0, 1));

        five.Deconstruct(
            out string objectId,
            out ImmutableArray<ulong> codes,
            out CodeExpansionPayload? map,
            out ImmutableArray<double> energies,
            out ImmutableArray<byte> mapping);
        Assert.Equal("cluster", objectId);
        Assert.Equal(2, codes.Length);
        Assert.Same(expansions, map);
        Assert.Equal(2, energies.Length);
        Assert.Equal(2, mapping.Length);

        var four = new ClusterTablePayload(
            "cluster",
            ImmutableArray.Create(1UL, 2UL),
            ImmutableArray.Create(0.0, 0.5),
            ImmutableArray.Create<byte>(0, 1));
        four.Deconstruct(
            out string fourObjectId,
            out ImmutableArray<ulong> fourCodes,
            out ImmutableArray<double> fourEnergies,
            out ImmutableArray<byte> fourMapping);
        Assert.Equal("cluster", fourObjectId);
        Assert.Equal(2, fourCodes.Length);
        Assert.Equal(2, fourEnergies.Length);
        Assert.Equal(2, fourMapping.Length);
        Assert.Null(four.CodeExpansions);

        var decoded = MessagePackSerializer.Deserialize<ClusterTablePayload>(
            MessagePackSerializer.Serialize(five));
        Assert.Equal(five.ObjectId, decoded.ObjectId);
        Assert.Equal(five.OccupationCodes.ToArray(), decoded.OccupationCodes.ToArray());
        Assert.NotNull(decoded.CodeExpansions);
        Assert.Equal(expansions.ExpandedCodes.ToArray(), decoded.CodeExpansions.ExpandedCodes.ToArray());
        Assert.Equal(expansions.CanonicalIndices.ToArray(), decoded.CodeExpansions.CanonicalIndices.ToArray());
        Assert.Equal(five.Energies.ToArray(), decoded.Energies.ToArray());
        Assert.Equal(five.ParticleTableMapping.ToArray(), decoded.ParticleTableMapping.ToArray());
    }

    [Fact]
    public void ModelPayloadExposesEveryDocumentedSection()
    {
        var pairLink = new PairLinkPayload(3, ImmutableArray.Create(-1, 0, 2), 4);
        var clusterLink = new ClusterLinkPayload(5, 6);
        var environment = new EnvironmentPayload(
            0b101UL,
            ImmutableArray.Create(pairLink),
            ImmutableArray.Create(clusterLink),
            ImmutableArray.Create<byte>(1, 2));
        var position = new PositionPayload(
            ImmutableArray.Create(0, 1, 0, 2),
            3,
            true,
            ImmutableArray.Create<byte>(0, 1));
        var structure = new StructurePayload(
            ImmutableArray.Create(ImmutableArray.Create(1.0, 0.0, 0.0)),
            ImmutableArray.Create(position),
            1,
            1,
            ImmutableArray.Create(environment),
            ImmutableArray.Create(1, 1, 1, 0),
            2,
            7);
        var energy = new EnergyPayload(
            ImmutableArray.Create(new PairTablePayload("pair", ImmutableArray.Create(0.0, 1.0))),
            ImmutableArray.Create(new ClusterTablePayload(
                "cluster",
                ImmutableArray.Create(1UL, 2UL),
                ImmutableArray.Create(0.0, 0.5),
                ImmutableArray.Create<byte>(0, 1))),
            ImmutableArray.Create(0.0, 0.25));
        var transitions = new TransitionPayload(
            ImmutableArray.Create(new JumpCollectionPayload(
                "kmc",
                "transition",
                0b10UL,
                ImmutableArray.Create(0u),
                ImmutableArray.Create(0u))),
            ImmutableArray.Create(new JumpDirectionPayload(
                "direction",
                1,
                0,
                ImmutableArray.Create(ImmutableArray.Create(0, 0, 0, 1)),
                ImmutableArray.Create(ImmutableArray.Create(0.0, 0.0, 0.0)),
                ImmutableArray.Create(0.0, 0.0, 1.0),
                1.0,
                false)),
            ImmutableArray.Create(new JumpRulePayload(
                0,
                0,
                1UL,
                0UL,
                2UL,
                1.0e13,
                ImmutableArray.Create(0.0, 0.0, 1.0),
                null,
                ImmutableArray.Create<byte>(0, 0xFF))),
            ImmutableArray.Create(1u),
            ImmutableArray.Create(0u),
            ImmutableArray.Create(0),
            ImmutableArray.Create(0));
        var simulations = ImmutableArray.Create(new SimulationPayload(
            "simulation",
            "kmc",
            ImmutableArray.Create(0u),
            new SimulationSettingsPayload(
                900.0,
                500,
                7,
                0,
                null,
                null,
                0.0,
                ImmutableArray.Create(0.0, 0.0, 1.0),
                null,
                null,
                0.01,
                1000,
                100,
                200,
                0b1000u),
            new TrackerModelPayload(ImmutableArray.Create(0u), ImmutableArray.Create(0u))));
        var particle = new ParticlePayload(
            ImmutableArray.Create(0.0, 1.0),
            ImmutableArray.Create("V", "A"),
            ImmutableArray.Create(true, false),
            2);

        var payload = new ModelPayload(
            1,
            "signature",
            "model-signature",
            "symmetry-1",
            "spglib-2.8.0/catalog-1",
            new string('a', 64),
            particle,
            structure,
            energy,
            transitions,
            simulations);

        Assert.Equal(1u, payload.FormatVersion);
        Assert.Equal("signature", payload.Signature);
        Assert.Equal("model-signature", payload.ModelSignature);
        Assert.Equal(2u, payload.Particle.Count);
        Assert.Equal(2, payload.Particle.Charges.Length);
        Assert.Equal(1u, payload.Structure.PositionCount);
        Assert.Equal(3u, payload.Structure.Positions[0].SiteIndex);
        Assert.Equal(4u, payload.Structure.Environments[0].Pairs[0].EnergyId);
        Assert.Equal(-1, payload.Structure.Environments[0].Pairs[0].Offset[0]);
        Assert.Equal((byte)6, payload.Structure.Environments[0].Clusters[0].BytePosition);
        Assert.Equal(1.0, payload.Energy.PairTables[0].Energies[1]);
        Assert.Equal(2UL, payload.Energy.ClusterTables[0].OccupationCodes[1]);
        Assert.Equal(0.25, payload.Energy.DefectBackground[1]);
        Assert.Equal("direction", payload.Transitions.JumpDirections[0].ObjectId);
        Assert.Equal((byte)0xFF, payload.Transitions.JumpRules[0].TrackerOrder[1]);
        Assert.Equal(1.0e13, payload.Transitions.JumpRules[0].FrequencyHz);
        Assert.Equal(7u, payload.Simulations[0].DefaultSettings.WriteOutCount);
        Assert.Equal(0u, payload.Simulations[0].TrackerModels.GlobalTrackerIndices[0]);
    }

    [Fact]
    public void JobPayloadExposesEveryResolvedSetting()
    {
        var payload = new JobPayload(
            1,
            2,
            "mmc",
            1200.0,
            10_000,
            100,
            0,
            3600,
            0.5,
            1.0e6,
            ImmutableArray.Create(0.0, 1.0, 0.0),
            0.1,
            2.0e13,
            0.01,
            1000,
            100,
            200,
            0x8A,
            ImmutableArray.Create(new SelectionOptimizerPayload(3, 4)),
            ImmutableArray.Create(new ExtensionPayload("ext", "{\"n\":12345678901234567890}")),
            "instruction",
            "v1:doping:1:2:2:0:1");

        Assert.Equal(1u, payload.FormatVersion);
        Assert.Equal(2u, payload.SimulationIndex);
        Assert.Equal("mmc", payload.Kind);
        Assert.Equal(1200.0, payload.TemperatureK);
        Assert.Equal(10_000UL, payload.TargetMcsp);
        Assert.Equal(100u, payload.WriteOutCount);
        Assert.Equal(0UL, payload.PreRunMcsp);
        Assert.Equal(3600UL, payload.TimeLimitS);
        Assert.Equal(0.5, payload.MinSuccessRate);
        Assert.Equal(1.0e6, payload.ElectricFieldVm);
        Assert.Equal(1.0, payload.ElectricFieldDirection[1]);
        Assert.Equal(0.1, payload.NormalizationEnergyEv);
        Assert.Equal(2.0e13, payload.MaxAttemptFrequencyHz);
        Assert.Equal(0.01, payload.BreakTolerance);
        Assert.Equal(1000UL, payload.BreakSampleLengthMcsp);
        Assert.Equal(100UL, payload.BreakSampleIntervalMcsp);
        Assert.Equal(200UL, payload.ResultSampleMcsp);
        Assert.Equal(0x8Au, payload.Flags);
        Assert.Equal(3u, payload.SelectionOptimizers[0].PositionId);
        Assert.Equal((byte)4, payload.SelectionOptimizers[0].ParticleId);
        Assert.Equal("{\"n\":12345678901234567890}", payload.Extensions[0].ConfigJson);
        Assert.Equal("instruction", payload.Instruction);
        Assert.Equal("v1:doping:1:2:2:0:1", payload.DopingInfo);
    }

    [Fact]
    public void LatticePayloadCarriesOnlyTheInitialState()
    {
        var payload = new LatticePayload(
            1,
            ImmutableArray.Create(2u, 3u, 4u),
            2,
            ImmutableArray.Create<byte>(1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0));

        Assert.Equal(1u, payload.FormatVersion);
        Assert.Equal<uint>([2u, 3u, 4u], payload.LatticeSize);
        Assert.Equal(2u, payload.PositionCount);
        Assert.Equal(12, payload.Lattice.Length);
    }

    [Fact]
    public void SerializedLatticePayloadUsesSnakeCaseMapKeys()
    {
        var payload = new LatticePayload(
            1,
            ImmutableArray.Create(1u, 1u, 1u),
            1,
            ImmutableArray.Create<byte>(0));

        var json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(payload));

        Assert.Contains("\"format_version\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"lattice_size\":[1,1,1]", json, StringComparison.Ordinal);
        Assert.Contains("\"position_count\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"lattice\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializedJobPayloadUsesSnakeCaseMapKeys()
    {
        var payload = new JobPayload(
            1,
            0,
            "kmc",
            900.0,
            1,
            0,
            0,
            null,
            null,
            0.0,
            ImmutableArray.Create(1.0, 0.0, 0.0),
            null,
            null,
            null,
            null,
            null,
            null,
            0x40,
            [],
            [],
            null,
            "");

        var json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(payload));

        Assert.Contains("\"format_version\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"simulation_index\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"electric_field_direction\":[1,0,0]", json, StringComparison.Ordinal);
        Assert.Contains("\"selection_optimizers\":[]", json, StringComparison.Ordinal);
        Assert.Contains("\"doping_info\":\"\"", json, StringComparison.Ordinal);
    }
}
