// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Energy;
using Mocassin.Contracts.Packages;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Packages.Parameterization;
using Mocassin.Packages.SimulationPackage.Payloads;
using Mocassin.Packages.Tests.Parameterization;

namespace Mocassin.Packages.Tests.SimulationPackage;

/// <summary>
/// Exercises the writer's cluster occupation code expansion map and its reduction coverage guards.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ClusterExpansionPayloadTests
{
    private const string clusterExpansionSample =
        "samples/step-11-cluster-expansion/step-11-cluster-expansion.mocprj";

    [Fact]
    public void NonSingletonClusterEmitsTheCompleteEightToSixMap()
    {
        var (_, _, applied) = LoadParameterized(clusterExpansionSample);
        var payload = ModelPayloadMapper.Map(applied);
        var table = Assert.Single(payload.Energy.ClusterTables);
        var expansions = Assert.IsType<CodeExpansionPayload>(table.CodeExpansions);

        Assert.Equal(6, table.OccupationCodes.Length);
        Assert.Equal(
            new ulong[] { 8322, 8323, 12418, 12419, 12482, 12483 },
            table.OccupationCodes.ToArray());
        Assert.Equal(
            new ulong[] { 8322, 8323, 8386, 8387, 12418, 12419, 12482, 12483 },
            expansions.ExpandedCodes.ToArray());
        Assert.Equal(
            new uint[] { 0, 1, 2, 3, 2, 3, 4, 5 },
            expansions.CanonicalIndices.ToArray());

        Assert.Equal(2u, expansions.CanonicalIndices[Array.IndexOf(expansions.ExpandedCodes.ToArray(), 8386UL)]);
        Assert.Equal(3u, expansions.CanonicalIndices[Array.IndexOf(expansions.ExpandedCodes.ToArray(), 8387UL)]);
        for (var index = 0; index < table.OccupationCodes.Length; index++)
        {
            var position = Array.IndexOf(expansions.ExpandedCodes.ToArray(), table.OccupationCodes[index]);
            Assert.Equal((uint)index, expansions.CanonicalIndices[position]);
        }
    }

    [Fact]
    public void SingletonClusterEmitsNull()
    {
        var (_, _, applied) = LoadParameterized("samples/step-13-populated/step-13-populated.mocprj");
        var payload = ModelPayloadMapper.Map(applied);
        var table = Assert.Single(payload.Energy.ClusterTables);

        Assert.Equal(12, table.OccupationCodes.Length);
        Assert.Null(table.CodeExpansions);
    }

    [Fact]
    public void ACodeClaimedByTwoOrbitsIsRejected()
    {
        var table = SyntheticTable(
            [
                new CompiledClusterOccupation(1, [1], [new CompiledClusterExpansion(1, 0)]),
                new CompiledClusterOccupation(
                    2,
                    [2],
                    [new CompiledClusterExpansion(1, 0), new CompiledClusterExpansion(2, 0)]),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => EnergyPayloadMapper.MapCodeExpansions(table, particleCount: 8));
        Assert.Contains("two orbits", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingCanonicalSelfEntryIsRejected()
    {
        var table = SyntheticTable(
            [
                new CompiledClusterOccupation(1, [1], [new CompiledClusterExpansion(2, 0)]),
                new CompiledClusterOccupation(3, [3], [new CompiledClusterExpansion(3, 0)]),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => EnergyPayloadMapper.MapCodeExpansions(table, particleCount: 8));
        Assert.Contains("canonical self-entry", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOutOfRangeExpansionCodeIsRejected()
    {
        var table = SyntheticTable(
            [
                new CompiledClusterOccupation(1, [1], [new CompiledClusterExpansion(1, 0)]),
                new CompiledClusterOccupation(
                    3,
                    [3],
                    [new CompiledClusterExpansion(3, 0), new CompiledClusterExpansion(1UL << 60, 0)]),
            ]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => EnergyPayloadMapper.MapCodeExpansions(table, particleCount: 8));
        Assert.Contains("bits above", exception.Message, StringComparison.Ordinal);
    }

    private static CompiledClusterTable SyntheticTable(
        ImmutableArray<CompiledClusterOccupation> occupations) =>
        new(
            0,
            new string('a', 64),
            ClusterId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-0000000011a0")),
            0,
            0,
            [0, 0],
            [Vector4I.Zero, Vector4I.Zero],
            occupations,
            [.. occupations.Select(static _ => 0.0)],
            [.. Enumerable.Range(1, 8).Select(static value => value)]);

    private static (Mocassin.Core.Project.Project Project, Mocassin.Compilation.Compiler.CompiledModel Compiled, ParameterizedModel Applied)
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
