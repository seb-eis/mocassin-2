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
public sealed class SiteEnergyTests
{
    [Fact]
    public void ValidSiteEnergiesProduceNoSiteEnergyDiagnostics()
    {
        var diagnostics = Validate(EnergyTestModels.CreateCombinedModel());

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code is
                EnergyDiagnosticCodes.InvalidSiteEnergy or
                EnergyDiagnosticCodes.SiteEnergyOutOfRange or
                EnergyDiagnosticCodes.ForbiddenSiteEnergy);
    }

    [Fact]
    public void UnknownReferencesDuplicateEntriesAndDuplicateSetsProduceEnm014()
    {
        var model = EnergyTestModels.CreateCombinedModel();
        var unknownSite = model with
        {
            SiteEnergies =
            [
                new SiteEnergySet(
                    CellSiteId.New(),
                    [new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(0.1))]),
            ],
        };
        var unknownParticle = model with
        {
            SiteEnergies =
            [
                new SiteEnergySet(
                    EnergyTestModels.OxygenSiteId,
                    [new SiteEnergyEntry(ParticleId.New(), new EnergyEv(0.1))]),
            ],
        };
        var duplicateEntry = model with
        {
            SiteEnergies =
            [
                new SiteEnergySet(
                    EnergyTestModels.OxygenSiteId,
                    [
                        new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(0.1)),
                        new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(0.2)),
                    ]),
            ],
        };
        var duplicateSet = model with
        {
            SiteEnergies =
            [
                new SiteEnergySet(EnergyTestModels.OxygenSiteId, []),
                new SiteEnergySet(EnergyTestModels.OxygenSiteId, []),
            ],
        };

        Assert.Contains(Validate(unknownSite), Code(EnergyDiagnosticCodes.InvalidSiteEnergy));
        Assert.Contains(Validate(unknownParticle), Code(EnergyDiagnosticCodes.InvalidSiteEnergy));
        Assert.Contains(Validate(duplicateEntry), Code(EnergyDiagnosticCodes.InvalidSiteEnergy));
        Assert.Contains(Validate(duplicateSet), Code(EnergyDiagnosticCodes.InvalidSiteEnergy));
    }

    [Theory]
    [InlineData(-101.0)]
    [InlineData(101.0)]
    public void OutOfRangeSiteEnergyProducesEnm015(double value)
    {
        var model = EnergyTestModels.CreateCombinedModel() with
        {
            SiteEnergies =
            [
                new SiteEnergySet(
                    EnergyTestModels.OxygenSiteId,
                    [new SiteEnergyEntry(EnergyTestModels.OxygenId, new EnergyEv(value))]),
            ],
        };

        Assert.Contains(Validate(model), Code(EnergyDiagnosticCodes.SiteEnergyOutOfRange));
    }

    [Fact]
    public void ForbiddenOccupancyProducesEnm016Warning()
    {
        var model = EnergyTestModels.CreateCombinedModel() with
        {
            SiteEnergies =
            [
                new SiteEnergySet(
                    EnergyTestModels.OxygenSiteId,
                    [new SiteEnergyEntry(EnergyTestModels.CeriumId, new EnergyEv(0.1))]),
            ],
        };

        var diagnostic = Assert.Single(
            Validate(model).Where(static diagnostic => diagnostic.Code == EnergyDiagnosticCodes.ForbiddenSiteEnergy));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    private static ImmutableArray<Diagnostic> Validate(ModelDefinition model) =>
        ProjectValidator.Validate(EnergyTestModels.CreateProject(model));

    private static Predicate<Diagnostic> Code(string code) => diagnostic => diagnostic.Code == code;
}
