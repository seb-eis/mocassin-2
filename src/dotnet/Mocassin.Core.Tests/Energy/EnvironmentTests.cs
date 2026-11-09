// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Energy;

[Trait("Tier", "PR-fast")]
public sealed class EnvironmentTests
{
    [Fact]
    public void ValidEnvironmentsProduceNoEnergyDiagnostics()
    {
        var diagnostics = Validate(EnergyTestModels.CreateCombinedModel());

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Code.StartsWith("ENM", StringComparison.Ordinal));
    }

    [Fact]
    public void InvalidStableAndUnstableCutoffsProduceEnm001()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var invalidStable = model with
        {
            StableEnvironment = model.StableEnvironment with { Cutoff = new LengthAngstrom(0.0) },
        };
        var invalidUnstable = model with
        {
            UnstableEnvironments =
            [
                model.UnstableEnvironments[0] with { Cutoff = new LengthAngstrom(0.0) },
            ],
        };

        Assert.Contains(Validate(invalidStable), Code(EnergyDiagnosticCodes.InvalidCutoff));
        Assert.Contains(Validate(invalidUnstable), Code(EnergyDiagnosticCodes.InvalidCutoff));
    }

    [Fact]
    public void InvertedOrOutOfCutoffFilterRangeProducesEnm003()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var inverted = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters =
                [
                    new InteractionFilter(
                        new LengthAngstrom(4.0),
                        new LengthAngstrom(2.0),
                        EnergyTestModels.CeriumSiteId,
                        EnergyTestModels.OxygenSiteId),
                ],
            },
        };
        var outside = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters =
                [
                    new InteractionFilter(
                        new LengthAngstrom(0.0),
                        new LengthAngstrom(7.0),
                        EnergyTestModels.CeriumSiteId,
                        EnergyTestModels.OxygenSiteId),
                ],
            },
        };

        Assert.Contains(Validate(inverted), Code(EnergyDiagnosticCodes.InvalidFilterRange));
        Assert.Contains(Validate(outside), Code(EnergyDiagnosticCodes.InvalidFilterRange));
    }

    [Fact]
    public void UnknownIdenticalAndMiscenteredFiltersProduceEnm004()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var unknown = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters =
                [
                    new InteractionFilter(
                        new LengthAngstrom(0.0),
                        new LengthAngstrom(2.0),
                        CellSiteId.New(),
                        EnergyTestModels.OxygenSiteId),
                ],
            },
        };
        var identical = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters =
                [
                    new InteractionFilter(
                        new LengthAngstrom(0.0),
                        new LengthAngstrom(2.0),
                        EnergyTestModels.OxygenSiteId,
                        EnergyTestModels.OxygenSiteId),
                ],
            },
        };
        var miscentered = model with
        {
            UnstableEnvironments =
            [
                model.UnstableEnvironments[0] with
                {
                    Filters =
                    [
                        new InteractionFilter(
                            new LengthAngstrom(0.0),
                            new LengthAngstrom(2.0),
                            EnergyTestModels.OxygenSiteId,
                            EnergyTestModels.UnstableSiteId),
                    ],
                },
            ],
        };

        Assert.Contains(Validate(unknown), Code(EnergyDiagnosticCodes.InvalidFilterSites));
        Assert.Contains(Validate(identical), Code(EnergyDiagnosticCodes.InvalidFilterSites));
        Assert.Contains(Validate(miscentered), Code(EnergyDiagnosticCodes.InvalidFilterSites));
    }

    [Fact]
    public void DuplicateAndMirroredStableFiltersProduceEnm005()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var duplicate = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters = [model.StableEnvironment.Filters[0], model.StableEnvironment.Filters[0]],
            },
        };
        var mirrored = model with
        {
            StableEnvironment = model.StableEnvironment with
            {
                Filters =
                [
                    model.StableEnvironment.Filters[0],
                    model.StableEnvironment.Filters[0] with
                    {
                        SiteAId = model.StableEnvironment.Filters[0].SiteBId,
                        SiteBId = model.StableEnvironment.Filters[0].SiteAId,
                    },
                ],
            },
        };

        Assert.Contains(Validate(duplicate), Code(EnergyDiagnosticCodes.DuplicateFilter));
        Assert.Contains(Validate(mirrored), Code(EnergyDiagnosticCodes.DuplicateFilter));
    }

    [Fact]
    public void UnknownStableOrDuplicateUnstableEnvironmentsProduceEnm006()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var unknown = model with
        {
            UnstableEnvironments =
            [
                model.UnstableEnvironments[0] with { SiteId = CellSiteId.New() },
            ],
        };
        var onStable = model with
        {
            UnstableEnvironments =
            [
                model.UnstableEnvironments[0] with { SiteId = EnergyTestModels.CeriumSiteId },
            ],
        };
        var duplicate = model with
        {
            UnstableEnvironments =
            [
                model.UnstableEnvironments[0],
                model.UnstableEnvironments[0],
            ],
        };

        Assert.Contains(Validate(unknown), Code(EnergyDiagnosticCodes.InvalidUnstableEnvironment));
        Assert.Contains(Validate(onStable), Code(EnergyDiagnosticCodes.InvalidUnstableEnvironment));
        Assert.Contains(Validate(duplicate), Code(EnergyDiagnosticCodes.InvalidUnstableEnvironment));
    }

    private static ImmutableArray<Diagnostic> Validate(ModelDefinition model) =>
        ProjectValidator.Validate(EnergyTestModels.CreateProject(model));

    private static Predicate<Diagnostic> Code(string code) => diagnostic => diagnostic.Code == code;
}
