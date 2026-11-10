// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Lattice;

[Trait("Tier", "PR-fast")]
public sealed class OccupationExchangeTests
{
    [Fact]
    public void UnknownSiteIsReported()
    {
        var exchange = LatticeTestModels.CeReduction with { SiteId = CellSiteId.New() };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void UnknownSourceParticleIsReported()
    {
        var exchange = LatticeTestModels.CeReduction with { SourceParticleId = ParticleId.New() };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void UnknownTargetParticleIsReported()
    {
        var exchange = LatticeTestModels.CeReduction with { TargetParticleId = ParticleId.New() };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void UnstableSiteIsReported()
    {
        var exchange = LatticeTestModels.OxygenVacancyFormation with
        {
            SiteId = LatticeTestModels.UnstableSite.Id,
        };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnstableSite);
    }

    [Fact]
    public void SourceParticleOutsideTheSiteSetIsReported()
    {
        var exchange = LatticeTestModels.CeReduction with
        {
            SourceParticleId = LatticeTestModels.O2Minus.Id,
        };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.SourceParticleNotInSet);
    }

    [Fact]
    public void TargetParticleOutsideTheSiteSetIsReported()
    {
        var exchange = LatticeTestModels.CeReduction with
        {
            TargetParticleId = LatticeTestModels.O2Minus.Id,
        };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.TargetParticleNotInSet);
    }

    [Fact]
    public void IdenticalSourceAndTargetAreReported()
    {
        var exchange = LatticeTestModels.CeReduction with
        {
            TargetParticleId = LatticeTestModels.Ce4Plus.Id,
        };

        var diagnostics = ValidateWithExchange(exchange);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.SourceEqualsTarget);
    }

    [Fact]
    public void CeriaReductionHasOppositeNonZeroChargeDeltas()
    {
        var model = LatticeTestModels.CreateLatticeOnlyModel();
        var primary = model.OccupationExchanges.Single(
            static exchange => exchange.Id == LatticeTestModels.CeReduction.Id);
        var dependent = model.OccupationExchanges.Single(
            static exchange => exchange.Id == LatticeTestModels.OxygenVacancyFormation.Id);

        Assert.Equal(-1.0, ChargeDelta(primary, model));
        Assert.Equal(2.0, ChargeDelta(dependent, model));
        Assert.Empty(LatticeModelValidator.Validate(model));
    }

    [Fact]
    public void IsovalentExchangeHasZeroChargeDeltaAndIsAcceptedWithoutCompensation()
    {
        var model = LatticeTestModels.CreateLatticeModel();
        var exchange = model.OccupationExchanges.Single(
            static candidate => candidate.Id == LatticeTestModels.CeIsovalent.Id);

        Assert.Equal(0.0, ChargeDelta(exchange, model));
        Assert.Empty(LatticeModelValidator.Validate(model));
    }

    private static ImmutableArray<Diagnostic> ValidateWithExchange(OccupationExchange exchange)
    {
        var model = LatticeTestModels.CreateLatticeModel();
        return LatticeModelValidator.Validate(model with
        {
            OccupationExchanges = [.. model.OccupationExchanges, exchange],
        });
    }

    private static double ChargeDelta(OccupationExchange exchange, ModelDefinition model)
    {
        var source = model.Particles.Single(particle => particle.Id == exchange.SourceParticleId);
        var target = model.Particles.Single(particle => particle.Id == exchange.TargetParticleId);
        return target.Charge.Value - source.Charge.Value;
    }
}
