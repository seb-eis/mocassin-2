// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Packages;
using Mocassin.Compilation.Compiler;
using Mocassin.Contracts.Parameterization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Project;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;
using Mocassin.Symmetry.Energy;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Exercises the real Step 08/09 model payload mapping, Zstd+MessagePack round-trip, signature re-encode, and
/// per-image pair-link/tracker invariants.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ModelPayloadEncodingTests
{
    [Fact]
    public void ModelPayloadRoundTripsAndReencodesTheParameterizationSignature()
    {
        var (_, compiled, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var payload = ModelPayloadMapper.Map(applied);
        var encoder = new PayloadEncoder(3);

        var bytes = encoder.EncodeModel(payload);
        var decoded = PayloadEncoder.DecodeModel(bytes);

        Assert.Equal(applied.Signature, decoded.Signature);
        Assert.Equal(applied.ModelSignature, decoded.ModelSignature);
        Assert.Equal(compiled.SymmetryContractVersion, decoded.SymmetryContractVersion);
        Assert.Equal(compiled.SpaceGroupDataVersion, decoded.SpaceGroupDataVersion);
        Assert.Equal(compiled.SpaceGroupDataSha256, decoded.SpaceGroupDataSha256);
        Assert.Equal(PackageFormatConstants.ModelPayloadFormatVersion, decoded.FormatVersion);

        var values = ModelPayloadMapper.ToParameterizationValues(decoded);
        Assert.Equal(
            applied.Signature,
            ParameterizationSignatureV1.Compute(decoded.ModelSignature, values));
        Assert.Equal(applied.Values.SiteEnergies.ToArray(), values.SiteEnergies.ToArray());
        Assert.Equal(applied.Values.TransitionRuleFrequencies.ToArray(), values.TransitionRuleFrequencies.ToArray());
        Assert.Equal(
            applied.Values.PairTables.Select(static table => table.ToArray()).ToArray(),
            values.PairTables.Select(static table => table.ToArray()).ToArray());
        Assert.Equal(
            applied.Values.GroupSets.Select(static table => table.ToArray()).ToArray(),
            values.GroupSets.Select(static table => table.ToArray()).ToArray());
        Assert.Equal(
            applied.Values.TransitionRuleFrequencies.ToArray(),
            decoded.Transitions.JumpRules.Select(static rule => rule.FrequencyHz).ToArray());
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void PayloadPairLinksAreOneToOneWithTheCompiledPerImageLinks()
    {
        var (_, compiled, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var payload = ModelPayloadMapper.Map(applied);

        Assert.Equal(compiled.Structure.PositionCount, payload.Structure.Environments.Length);
        for (var position = 0; position < compiled.Structure.PositionCount; position++)
        {
            var compiledLinks = compiled.Structure.Positions[position].PairLinks;
            var payloadPairs = payload.Structure.Environments[position].Pairs;
            Assert.Equal(compiledLinks.Length, payloadPairs.Length);
            for (var index = 0; index < compiledLinks.Length; index++)
            {
                var link = compiledLinks[index];
                var pair = payloadPairs[index];
                Assert.Equal((uint)link.TargetP, pair.TargetPosition);
                Assert.Equal(link.Offset.A, pair.Offset[0]);
                Assert.Equal(link.Offset.B, pair.Offset[1]);
                Assert.Equal(link.Offset.C, pair.Offset[2]);
                Assert.Equal((uint)link.PairTableIndex, pair.EnergyId);
            }

            var keys = payloadPairs
                .Select(static pair => (pair.TargetPosition, pair.Offset[0], pair.Offset[1], pair.Offset[2]))
                .ToArray();
            Assert.Equal(keys.Length, keys.Distinct().Count());
        }
    }

    [Fact]
    public void MultiImageShellExpandsToEveryEquivalentImage()
    {
        var (project, compiled, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var sampler = new EnergyGeometrySampler();
        Assert.True(sampler.TrySamplePairs(project.Model, 1, 1, 1, out var previews, out var diagnostics));
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var multi = previews.FirstOrDefault(static preview =>
            !preview.EquivalentImages.IsDefaultOrEmpty && preview.EquivalentImages.Length > 1);
        Assert.NotNull(multi);
        var bucket = Bucket(multi!.DistanceAngstrom);
        var tableIndex = compiled.Energy.PairTables.Single(table => table.EnergyId == multi.EnergyId).Index;
        var expectedImages = previews
            .Where(preview =>
                preview.CenterP == multi.CenterP &&
                preview.EnergyId == multi.EnergyId &&
                Bucket(preview.DistanceAngstrom) == bucket)
            .SelectMany(static preview => preview.EquivalentImages)
            .ToArray();
        var links = compiled.Structure.Positions[multi.CenterP].PairLinks
            .Where(link => link.PairTableIndex == tableIndex && link.DistanceBucketFm == bucket)
            .ToArray();

        Assert.Equal(expectedImages.Length, links.Length);
        Assert.All(expectedImages, image => Assert.Contains(
            links,
            link => link.TargetP == image.P && link.Offset == image));
        var payload = ModelPayloadMapper.Map(applied);
        Assert.Contains(
            payload.Structure.Environments.SelectMany(static environment => environment.Pairs),
            pair => pair.Offset[0] != 0 || pair.Offset[1] != 0 || pair.Offset[2] != 0);
    }

    [Fact]
    public void TrackerOrderIsVariableLengthAndMatchesTheRepresentativePath()
    {
        var (_, compiled, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var payload = ModelPayloadMapper.Map(applied);

        Assert.Equal(compiled.Transitions.Rules.Length, payload.Transitions.JumpRules.Length);
        for (var index = 0; index < compiled.Transitions.Rules.Length; index++)
        {
            var rule = compiled.Transitions.Rules[index];
            var payloadRule = payload.Transitions.JumpRules[index];
            var representative = compiled.Transitions.Directions[rule.DirectionIndex];
            Assert.Equal(representative.Path.Length, payloadRule.TrackerOrder.Length);
            Assert.Equal(rule.TrackerOrder, payloadRule.TrackerOrder);
            Assert.All(
                payloadRule.TrackerOrder,
                tracker => Assert.True(tracker == 0xFF || tracker < compiled.Transitions.GlobalTrackers.Length));
        }
    }

    [Fact]
    public void EncodingIsDeterministic()
    {
        var (_, _, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var payload = ModelPayloadMapper.Map(applied);
        var encoder = new PayloadEncoder(3);

        var first = encoder.EncodeModel(payload);
        var second = encoder.EncodeModel(payload);
        var decoded = PayloadEncoder.DecodeModel(first);
        var reencoded = encoder.EncodeModel(decoded);

        Assert.Equal(first, second);
        Assert.Equal(first, reencoded);
    }

    [Fact]
    public void DecodeGuardRejectsOversizedAndCorruptPayloads()
    {
        var (_, _, applied) = LoadParameterized(ParameterizationTestSupport.Ceria);
        var bytes = new PayloadEncoder(3).EncodeModel(ModelPayloadMapper.Map(applied));

        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel(bytes, maxDecompressedBytes: 8));
        Assert.Throws<InvalidDataException>(() => PayloadEncoder.DecodeModel([0x01, 0x02, 0x03]));
    }

    [Fact]
    public void FieldDirectionUsesTheCartesianCellTransform()
    {
        var a = new Cartesian3(2.0, 0.0, 0.0);
        var b = new Cartesian3(0.5, 3.0, 0.0);
        var c = new Cartesian3(0.25, 0.5, 4.0);
        var unit = FieldDirectionConverter.ToCartesianUnit(
            new Fractional3(1.0, -0.25, 0.5),
            [a, b, c]);

        var expected =
            (a * 1.0) +
            (b * -0.25) +
            (c * 0.5);
        var length = expected.Length;
        Assert.Equal(expected.X / length, unit[0], 12);
        Assert.Equal(expected.Y / length, unit[1], 12);
        Assert.Equal(expected.Z / length, unit[2], 12);
        Assert.Equal(1.0, Math.Sqrt((unit[0] * unit[0]) + (unit[1] * unit[1]) + (unit[2] * unit[2])), 12);
        Assert.Equal<uint>([1u, 0u, 0u], [.. FieldDirectionConverter
            .ToCartesianUnit(Fractional3.Zero, [a, b, c])
            .Select(static value => (uint)value)]);
    }

    private static int Bucket(double distanceAngstrom) =>
        checked((int)Math.Round(
            distanceAngstrom / GeometryTolerances.PairDistanceBucket,
            MidpointRounding.AwayFromZero));

    private static (Project Project, CompiledModel Compiled, ParameterizedModel Applied)
        LoadParameterized(string projectPath)
    {
        var (project, compiled) = ParameterizationTestSupport.LoadAndCompile(projectPath);
        var result = new ParameterizationService().Apply(project, compiled, template: null);
        Assert.True(
            result.IsSuccess,
            string.Join(Environment.NewLine, result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return (project, compiled, result.Model!);
    }
}
