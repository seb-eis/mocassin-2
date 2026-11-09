// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class ClusterTests
{
    [Fact]
    public void ValidClustersProduceNoClusterDiagnostics()
    {
        var diagnostics = Validate(EnergyTestModels.CreateCombinedModel());

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code is
                EnergyDiagnosticCodes.InvalidClusterCenter or
                EnergyDiagnosticCodes.InvalidClusterSize or
                EnergyDiagnosticCodes.ClustersPerCenterWarning or
                EnergyDiagnosticCodes.ClustersPerCenterLimit);
    }

    [Fact]
    public void UnknownOrUnstableCenterWithoutEnvironmentProducesEnm007()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var unknown = model with
        {
            Clusters =
            [
                model.Clusters[0] with { CenterSiteId = CellSiteId.New() },
            ],
        };
        var missingEnvironment = model with
        {
            UnstableEnvironments = [],
        };

        Assert.Contains(Validate(unknown), Code(EnergyDiagnosticCodes.InvalidClusterCenter));
        Assert.Contains(Validate(missingEnvironment), Code(EnergyDiagnosticCodes.InvalidClusterCenter));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void InvalidOffsetCountProducesEnm008(int offsetCount)
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var offsets = Enumerable.Range(0, offsetCount)
            .Select(index => new Fractional3(index * 0.1, 0.0, 0.0))
            .ToImmutableArray();
        var invalid = model with
        {
            Clusters = [model.Clusters[0] with { OffsetVectors = offsets }],
        };

        Assert.Contains(Validate(invalid), Code(EnergyDiagnosticCodes.InvalidClusterSize));
    }

    [Theory]
    [InlineData(5, EnergyDiagnosticCodes.ClustersPerCenterWarning)]
    [InlineData(11, EnergyDiagnosticCodes.ClustersPerCenterLimit)]
    public void ClusterCountPerCenterProducesWarningOrError(int count, string expectedCode)
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var clusters = Enumerable.Range(0, count)
            .Select(index => new Cluster(
                ClusterId.New(),
                $"Cluster {index}",
                EnergyTestModels.CeriumSiteId,
                [new Fractional3(0.1, 0.0, 0.0), new Fractional3(0.0, 0.1, 0.0)]))
            .ToImmutableArray();
        var invalid = model with { Clusters = clusters };

        Assert.Contains(Validate(invalid), Code(expectedCode));
    }

    private static ImmutableArray<Diagnostic> Validate(ModelDefinition model) =>
        ProjectValidator.Validate(EnergyTestModels.CreateProject(model));

    private static Predicate<Diagnostic> Code(string code) => diagnostic => diagnostic.Code == code;
}
