// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Lattice;

[Trait("Tier", "PR-fast")]
public sealed class DopingTests
{
    [Fact]
    public void DuplicatePriorityIsReported()
    {
        var doping = LatticeTestModels.IsovalentDoping with { Priority = 0 };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.DuplicatePriority);
    }

    [Fact]
    public void UnknownPrimaryExchangeIsReported()
    {
        var doping = LatticeTestModels.CeriaReductionDoping with
        {
            PrimaryExchangeId = OccupationExchangeId.New(),
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnknownExchange);
    }

    [Fact]
    public void UnknownDependentExchangeIsReported()
    {
        var doping = LatticeTestModels.CeriaReductionDoping with
        {
            DependentExchangeId = OccupationExchangeId.New(),
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.UnknownExchange);
    }

    [Fact]
    public void DependentExchangeWithoutCompensationIsReported()
    {
        var doping = LatticeTestModels.IsovalentDoping with
        {
            DependentExchangeId = LatticeTestModels.OxygenVacancyFormation.Id,
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.CompensationShape);
    }

    [Fact]
    public void CompensationWithoutDependentExchangeIsReported()
    {
        var doping = LatticeTestModels.CeriaReductionDoping with
        {
            DependentExchangeId = null,
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.CompensationShape);
    }

    [Fact]
    public void ZeroDependentChargeDeltaIsReported()
    {
        var doping = LatticeTestModels.CeriaReductionDoping with
        {
            PrimaryExchangeId = LatticeTestModels.CeIsovalent.Id,
            DependentExchangeId = LatticeTestModels.OxygenVacancyFormation.Id,
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.CompensationCharge);
    }

    [Fact]
    public void SameSignChargeDeltasAreReported()
    {
        var doping = LatticeTestModels.CeriaReductionDoping with
        {
            DependentExchangeId = LatticeTestModels.CeReduction.Id,
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.CompensationCharge);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void PriorityOutsideTheRangeIsReported(int priority)
    {
        var doping = LatticeTestModels.IsovalentDoping with { Priority = priority };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.PriorityOutOfRange);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    public void PriorityAtTheBoundaryIsAccepted(int priority)
    {
        var doping = LatticeTestModels.IsovalentDoping with { Priority = priority };

        var diagnostics = ValidateWithDoping(doping);

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.PriorityOutOfRange);
    }

    [Fact]
    public void EmptyExchangeNameIsReported()
    {
        var exchange = LatticeTestModels.CeIsovalent with { Name = " " };
        var model = LatticeTestModels.CreateLatticeModel();

        var diagnostics = LatticeModelValidator.Validate(model with
        {
            OccupationExchanges = [.. model.OccupationExchanges, exchange],
        });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.InvalidName);
    }

    [Fact]
    public void DuplicateExchangeNameIsReportedCaseInsensitively()
    {
        var exchange = LatticeTestModels.CeIsovalent with { Name = "ce4+ -> ce3+" };
        var model = LatticeTestModels.CreateLatticeModel();

        var diagnostics = LatticeModelValidator.Validate(model with
        {
            OccupationExchanges = [.. model.OccupationExchanges, exchange],
        });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.InvalidName);
    }

    [Fact]
    public void DuplicateDopingNameIsReportedCaseInsensitively()
    {
        var doping = LatticeTestModels.IsovalentDoping with
        {
            Priority = 2,
            Name = "reduce CERIA",
        };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.InvalidName);
    }

    [Fact]
    public void EmptyDopingNameIsReported()
    {
        var doping = LatticeTestModels.IsovalentDoping with { Name = string.Empty };

        var diagnostics = ValidateWithDoping(doping);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == LatticeDiagnosticCodes.InvalidName);
    }

    private static ImmutableArray<Diagnostic> ValidateWithDoping(Doping doping)
    {
        var model = LatticeTestModels.CreateLatticeModel();
        return LatticeModelValidator.Validate(model with
        {
            Dopings = [.. model.Dopings, doping],
        });
    }
}
