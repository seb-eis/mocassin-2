// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Jobs;
using Mocassin.Packages.Jobs;
using static Mocassin.Packages.Tests.Jobs.DopingTestScenario;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-medium")]
public sealed class DopingApplicationTests
{
    [Fact]
    public void NoDopingsProducesAnEmptyReportAndSummary()
    {
        var (project, model) = CreateScenario();

        var result = SupercellGenerator.Generate(
            project,
            model,
            CreateJob(new LatticeRequest(10, 1, 1)));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        Assert.Empty(lattice.Report.Applications);
        Assert.Equal(string.Empty, lattice.DopingInfo);
        Assert.Equal(30, lattice.Report.TotalPositions);
        Assert.Equal(0.0, lattice.Report.TotalChargeBefore);
        Assert.Equal(0.0, lattice.Report.TotalChargeAfter);
        Assert.True(lattice.Report.IsChargeNeutral);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TenPercentCompensatedDopingAppliesTruncatedCounts()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 0.1));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(compensatedIntegerDoping, application.DopingId);
        Assert.Equal(10, application.Priority);
        Assert.Equal(10, application.AvailablePrimary);
        Assert.Equal(1, application.RequestedPrimary);
        Assert.Equal(1, application.AppliedPrimary);
        Assert.Equal(1, application.AppliedDependent);
        Assert.Equal(0.1, application.EffectiveFraction);
        Assert.Equal(1, CountParticle(lattice, model, particleB));
        Assert.Equal(1, CountParticle(lattice, model, particleY));
        Assert.Equal(9, CountParticle(lattice, model, particleA));
        Assert.Equal(9, CountParticle(lattice, model, particleX));
        Assert.True(lattice.Report.IsChargeNeutral);
        Assert.Equal(
            string.Create(
                CultureInfo.InvariantCulture,
                $"v1:{compensatedIntegerDoping.Value:D}:10:1:1:1:0.1"),
            lattice.DopingInfo);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ZeroPercentAppliesZeroAndKeepsTheSummaryEntry()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 0.0));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(0, application.RequestedPrimary);
        Assert.Equal(0, application.AppliedPrimary);
        Assert.Equal(0, application.AppliedDependent);
        Assert.Equal(0.0, application.EffectiveFraction);
        Assert.NotEqual(string.Empty, lattice.DopingInfo);
        Assert.EndsWith(":10:0:0:0:0", lattice.DopingInfo);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void HundredPercentCompensatedDopingReplacesEverySourceParticle()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 1.0));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(10, application.RequestedPrimary);
        Assert.Equal(10, application.AppliedPrimary);
        Assert.Equal(10, application.AppliedDependent);
        Assert.Equal(1.0, application.EffectiveFraction);
        Assert.Equal(0, CountParticle(lattice, model, particleA));
        Assert.Equal(10, CountParticle(lattice, model, particleB));
        Assert.Equal(0, CountParticle(lattice, model, particleX));
        Assert.Equal(10, CountParticle(lattice, model, particleY));
        Assert.True(lattice.Report.IsChargeNeutral);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TinyCellTruncatesToZeroAndWarnsJob011()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(1, 1, 1),
            (compensatedIntegerDoping, 0.5));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(1, application.AvailablePrimary);
        Assert.Equal(0, application.RequestedPrimary);
        Assert.Equal(0, application.AppliedPrimary);
        Assert.Equal(0.0, application.EffectiveFraction);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.FractionMismatch &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.UnrealizedDoping);
    }

    [Fact]
    public void TruncationReportsRequestedAppliedAndEffectiveFraction()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 0.25));

        Assert.True(result.IsSuccess);
        var application = Assert.Single(result.Lattice!.Report.Applications);
        Assert.Equal(10, application.AvailablePrimary);
        Assert.Equal(2, application.RequestedPrimary);
        Assert.Equal(2, application.AppliedPrimary);
        Assert.Equal(0.2, application.EffectiveFraction);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.FractionMismatch);
    }

    [Fact]
    public void DopingsApplyInResolvedPriorityOrderRegardlessOfRequestOrder()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (sameSiteDoping, 0.5),
            (plainDoping, 0.5));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        Assert.Collection(
            lattice.Report.Applications,
            application => Assert.Equal(plainDoping, application.DopingId),
            application => Assert.Equal(sameSiteDoping, application.DopingId));
        Assert.Equal(5, CountParticle(lattice, model, particleB));
        Assert.Equal(2, CountParticle(lattice, model, particleC));
        Assert.Equal(3, CountParticle(lattice, model, particleA));
        var entries = lattice.DopingInfo.Split(';');
        Assert.Equal(2, entries.Length);
        Assert.StartsWith($"v1:{plainDoping.Value:D}:30:", entries[0]);
        Assert.StartsWith($"v1:{sameSiteDoping.Value:D}:40:", entries[1]);
    }

    [Fact]
    public void SameSiteSequenceAppliesToTheRemainingParticles()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (plainDoping, 0.5),
            (sameSiteDoping, 1.0));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        Assert.Collection(
            lattice.Report.Applications,
            application =>
            {
                Assert.Equal(plainDoping, application.DopingId);
                Assert.Equal(10, application.AvailablePrimary);
                Assert.Equal(5, application.AppliedPrimary);
            },
            application =>
            {
                Assert.Equal(sameSiteDoping, application.DopingId);
                Assert.Equal(5, application.AvailablePrimary);
                Assert.Equal(5, application.RequestedPrimary);
                Assert.Equal(5, application.AppliedPrimary);
            });
        Assert.Equal(0, CountParticle(lattice, model, particleA));
        Assert.Equal(5, CountParticle(lattice, model, particleB));
        Assert.Equal(5, CountParticle(lattice, model, particleC));
    }

    [Fact]
    public void FractionalChargeRatioReducesThePrimaryCountUntilIntegral()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedFractionalDoping, 0.3));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(10, application.AvailablePrimary);
        Assert.Equal(3, application.RequestedPrimary);
        Assert.Equal(2, application.AppliedPrimary);
        Assert.Equal(3, application.AppliedDependent);
        Assert.Equal(0.2, application.EffectiveFraction);
        Assert.Equal(2, CountParticle(lattice, model, particleC));
        Assert.Equal(3, CountParticle(lattice, model, particleY));
        Assert.Equal(8, CountParticle(lattice, model, particleA));
        Assert.Equal(7, CountParticle(lattice, model, particleX));
        Assert.True(lattice.Report.IsChargeNeutral);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.NonNeutralLattice);
    }

    [Fact]
    public void DependentCapacityLimitsTheCompensatedPair()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (consumeXDoping, 0.5),
            (compensatedFractionalDoping, 0.9));

        Assert.True(result.IsSuccess);
        var application = result.Lattice!.Report.Applications[1];
        Assert.Equal(compensatedFractionalDoping, application.DopingId);
        Assert.Equal(9, application.RequestedPrimary);
        Assert.Equal(2, application.AppliedPrimary);
        Assert.Equal(3, application.AppliedDependent);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation);
    }

    [Fact]
    public void ImpossibleCompensationFailsWithJob012AndNoPartialLattice()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (consumeXDoping, 1.0),
            (compensatedIntegerDoping, 0.5));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lattice);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void OverlappingPrimaryAndDependentSourcesReduceToAFeasiblePair()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (overlappingCompensatedDoping, 1.0));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        var application = Assert.Single(lattice.Report.Applications);
        Assert.Equal(10, application.AvailablePrimary);
        Assert.Equal(10, application.RequestedPrimary);
        Assert.Equal(5, application.AppliedPrimary);
        Assert.Equal(5, application.AppliedDependent);
        Assert.Equal(0.5, application.EffectiveFraction);
        Assert.Equal(0, CountParticle(lattice, model, particleA));
        Assert.Equal(5, CountParticle(lattice, model, particleB));
        Assert.Equal(5, CountParticle(lattice, model, particleD));
        Assert.True(lattice.Report.IsChargeNeutral);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation);
    }

    [Fact]
    public void NonCompensatedChargedDopingWarnsJob016()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (plainDoping, 0.5));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        Assert.Equal(5.0, lattice.Report.TotalChargeAfter);
        Assert.False(lattice.Report.IsChargeNeutral);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.NonNeutralLattice &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void MixedCompensatedAndNonCompensatedConfigWarnsJob016()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 0.2),
            (plainDoping, 0.3));

        Assert.True(result.IsSuccess);
        Assert.Equal(2.0, result.Lattice!.Report.TotalChargeAfter);
        Assert.False(result.Lattice.Report.IsChargeNeutral);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.NonNeutralLattice);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation);
    }

    [Fact]
    public void PreExistingNonNeutralBaseIsReportedWithoutDiagnostics()
    {
        var (project, model) = CreateScenario(chargedBase: true);

        var result = SupercellGenerator.Generate(
            project,
            model,
            CreateJob(new LatticeRequest(10, 1, 1)));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        Assert.Equal(10.0, lattice.Report.TotalChargeBefore);
        Assert.Equal(10.0, lattice.Report.TotalChargeAfter);
        Assert.False(lattice.Report.IsChargeNeutral);
        Assert.Empty(lattice.Report.Applications);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void CompensatedDopingOnChargedBaseIsNotMisclassified()
    {
        var (project, model) = CreateScenario(chargedBase: true);

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (compensatedIntegerDoping, 0.2));

        Assert.True(result.IsSuccess);
        Assert.Equal(10.0, result.Lattice!.Report.TotalChargeAfter);
        Assert.False(result.Lattice.Report.IsChargeNeutral);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code is JobDiagnosticCodes.InvalidCompensation or JobDiagnosticCodes.NonNeutralLattice);
    }

    [Fact]
    public void IsovalentDopingDoesNotWarnJob016()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (isovalentDoping, 0.5));

        Assert.True(result.IsSuccess);
        Assert.True(result.Lattice!.Report.IsChargeNeutral);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.NonNeutralLattice);
    }

    [Fact]
    public void GenerationIsDeterministicForTheSameDopingDescriptor()
    {
        var (project, model) = CreateScenario();
        var job = CreateJob(
            new LatticeRequest(10, 1, 1),
            [new DopingValue(compensatedIntegerDoping, 0.3), new DopingValue(plainDoping, 0.4)]);

        var first = SupercellGenerator.Generate(project, model, job);
        var second = SupercellGenerator.Generate(project, model, job);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal<byte>(first.Lattice!.Particles, second.Lattice!.Particles);
        Assert.Equal(first.Lattice.DopingInfo, second.Lattice.DopingInfo);
        Assert.Equal(
            first.Lattice.Report.Applications.ToArray(),
            second.Lattice.Report.Applications.ToArray());
        Assert.Equal(first.Lattice.Report.TotalChargeBefore, second.Lattice.Report.TotalChargeBefore);
        Assert.Equal(first.Lattice.Report.TotalChargeAfter, second.Lattice.Report.TotalChargeAfter);
        Assert.Equal(first.Lattice.Report.IsChargeNeutral, second.Lattice.Report.IsChargeNeutral);
        Assert.Equal<byte>(first.LatticeSeed, second.LatticeSeed);
    }

    [Fact]
    public void OneSamplerStreamServesEveryDopingApplication()
    {
        var (project, model) = CreateScenario();
        var result = Generate(
            project,
            model,
            new LatticeRequest(8, 1, 1),
            (plainDoping, 0.4),
            (sameSiteDoping, 0.6));

        Assert.True(result.IsSuccess);
        var lattice = result.Lattice!;
        int[] candidates = [0, 3, 6, 9, 12, 15, 18, 21];
        var sampler = new LatticeSampler(result.LatticeSeed.AsSpan());
        var primary = sampler.SampleWithoutReplacement(candidates, 3);
        var remaining = candidates.Except(primary).ToArray();
        var dependent = sampler.SampleWithoutReplacement(remaining, 3);

        var expected = new byte[24];
        for (var offset = 0; offset < expected.Length; offset += 3)
        {
            expected[offset] = 1;
            expected[offset + 1] = 5;
            expected[offset + 2] = 0;
        }

        foreach (var position in primary)
        {
            expected[position] = 2;
        }

        foreach (var position in dependent)
        {
            expected[position] = 3;
        }

        Assert.Equal(expected, lattice.Particles.ToArray());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void NonFiniteOrOutOfRangeFractionIsJob005AndNoLattice(double fraction)
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (plainDoping, fraction));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Lattice);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidDopingFraction &&
            diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void AvailableZeroWithPositiveFractionWarnsJob010()
    {
        var (project, model) = CreateScenario();

        var result = Generate(
            project,
            model,
            new LatticeRequest(10, 1, 1),
            (plainDoping, 1.0),
            (sameSiteDoping, 0.5));

        Assert.True(result.IsSuccess);
        var application = result.Lattice!.Report.Applications[1];
        Assert.Equal(0, application.AvailablePrimary);
        Assert.Equal(0, application.RequestedPrimary);
        Assert.Equal(0, application.AppliedPrimary);
        Assert.Equal(0.0, application.EffectiveFraction);
        Assert.Contains(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.UnrealizedDoping &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(result.Diagnostics, static diagnostic =>
            diagnostic.Code == JobDiagnosticCodes.InvalidCompensation);
    }

    [Property(MaxTest = 100)]
    public bool RandomDopingCountsStayWithinTheAvailableBoundsAndStayDeterministic(double[]? fractions)
    {
        if (fractions is null)
        {
            return true;
        }

        var values = fractions
            .Where(static fraction => double.IsFinite(fraction))
            .Select(static fraction => Math.Clamp(fraction, 0.0, 1.0))
            .Take(4)
            .ToArray();
        if (values.Length == 0)
        {
            return true;
        }

        var (project, model) = CreateScenario();
        var dopings = values
            .Select((fraction, index) => new DopingValue(
                index % 2 == 0 ? compensatedIntegerDoping : plainDoping,
                fraction))
            .ToImmutableArray();
        var request = new LatticeRequest(10, 1, 1);
        var first = SupercellGenerator.Generate(project, model, CreateJob(request, dopings));
        var second = SupercellGenerator.Generate(project, model, CreateJob(request, dopings));

        if (!first.IsSuccess || !second.IsSuccess)
        {
            return false;
        }

        if (!first.Lattice!.Particles.SequenceEqual(second.Lattice!.Particles) ||
            !first.Lattice.Report.Applications.SequenceEqual(second.Lattice.Report.Applications))
        {
            return false;
        }

        foreach (var application in first.Lattice.Report.Applications)
        {
            if (application.AppliedPrimary < 0 ||
                application.AppliedPrimary > application.RequestedPrimary ||
                application.RequestedPrimary > application.AvailablePrimary)
            {
                return false;
            }

            var expected = application.AvailablePrimary == 0
                ? 0.0
                : application.AppliedPrimary / (double)application.AvailablePrimary;
            if (Math.Abs(expected - application.EffectiveFraction) > 1e-12)
            {
                return false;
            }
        }

        return true;
    }
}
