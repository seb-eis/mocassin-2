// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-medium")]
public sealed class DopingApplicationTests
{
    private static readonly ParticleId particleA = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a01");

    private static readonly ParticleId particleB = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a02");

    private static readonly ParticleId particleC = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a03");

    private static readonly ParticleId particleA2 = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a04");

    private static readonly ParticleId particleX = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a05");

    private static readonly ParticleId particleY = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a06");

    private static readonly ParticleId particleZ = CreateParticle("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9a07");

    private static readonly ParticleSetId setA = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b01");

    private static readonly ParticleSetId setX = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b02");

    private static readonly ParticleSetId setU = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b03");

    private static readonly ParticleSetId setZ = CreateSet("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9b04");

    private static readonly CellSiteId siteAId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c01");

    private static readonly CellSiteId siteXId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c02");

    private static readonly CellSiteId siteUId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c03");

    private static readonly CellSiteId siteZId = CreateSite("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9c04");

    private static readonly OccupationExchangeId exchangeAb =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d01", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeAc =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d02", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeAa =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d03", CultureInfo.InvariantCulture));

    private static readonly OccupationExchangeId exchangeXy =
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9d04", CultureInfo.InvariantCulture));

    private static readonly DopingId consumeXDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e01", CultureInfo.InvariantCulture));

    private static readonly DopingId compensatedIntegerDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e02", CultureInfo.InvariantCulture));

    private static readonly DopingId compensatedFractionalDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e03", CultureInfo.InvariantCulture));

    private static readonly DopingId plainDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e04", CultureInfo.InvariantCulture));

    private static readonly DopingId sameSiteDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e05", CultureInfo.InvariantCulture));

    private static readonly DopingId isovalentDoping =
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9e06", CultureInfo.InvariantCulture));

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

    private static SupercellGenerationResult Generate(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        params (DopingId DopingId, double Fraction)[] dopings)
    {
        var values = dopings.Select(static doping => new DopingValue(doping.DopingId, doping.Fraction));
        return SupercellGenerator.Generate(project, model, CreateJob(request, [.. values]));
    }

    private static int CountParticle(GeneratedLattice lattice, CompiledModel model, ParticleId particleId)
    {
        Assert.True(model.Particles.TryGetIndex(particleId, out var index));
        return lattice.Particles.Count(value => value == index);
    }

    private static JobDescriptor CreateJob(
        LatticeRequest request,
        ImmutableArray<DopingValue>? dopings = null) => new(
        1,
        0,
        0,
        0,
        "collection",
        "config",
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f01", CultureInfo.InvariantCulture)),
        0,
        SimulationKind.Kmc,
        UseDualDofCorrection: false,
        new ResolvedJobSettings(
            900.0,
            500,
            7,
            null,
            null,
            ExecutionFlags.Deterministic,
            null,
            0,
            0.0,
            Fractional3.Zero,
            null,
            null,
            0.01,
            1000,
            100,
            200,
            []),
        request,
        dopings ?? [],
        [],
        [.. Enumerable.Repeat((byte)0x5a, LatticeSeedDeriver.SeedByteLength)]);

    private static (Project Project, CompiledModel Model) CreateScenario(bool chargedBase = false)
    {
        var particles = new Particle[]
        {
            new(particleA, "A", "A", new ChargeElementary(1.0), IsVacancy: false),
            new(particleB, "B", "B", new ChargeElementary(2.0), IsVacancy: false),
            new(particleC, "C", "C", new ChargeElementary(2.5), IsVacancy: false),
            new(particleA2, "A2", "A2", new ChargeElementary(1.0), IsVacancy: false),
            new(particleX, "X", "X", new ChargeElementary(-1.0), IsVacancy: false),
            new(particleY, "Y", "Y", new ChargeElementary(-2.0), IsVacancy: false),
            new(particleZ, "Z", "Z", new ChargeElementary(1.0), IsVacancy: false),
        };

        var sites = new List<CellSite>
        {
            new(siteAId, "A", new Fractional3(0.0, 0.0, 0.0), setA, SiteStability.Stable),
            new(siteXId, "X", new Fractional3(0.5, 0.0, 0.0), setX, SiteStability.Stable),
            new(siteUId, "U", new Fractional3(0.5, 0.5, 0.5), setU, SiteStability.Unstable),
        };

        var compiledSites = new List<CompiledSite>
        {
            new(0, siteAId, "A", new Fractional3(0.0, 0.0, 0.0), SiteStability.Stable, setA, 0, [0]),
            new(1, siteXId, "X", new Fractional3(0.5, 0.0, 0.0), SiteStability.Stable, setX, 1, [1]),
            new(2, siteUId, "U", new Fractional3(0.5, 0.5, 0.5), SiteStability.Unstable, setU, 2, [2]),
        };

        var positions = new List<CompiledPosition>
        {
            CompiledPositionAt(0, siteAId, SiteStability.Stable, 0, 0b11110UL),
            CompiledPositionAt(1, siteXId, SiteStability.Stable, 1, 0b1100000UL),
            CompiledPositionAt(2, siteUId, SiteStability.Unstable, 2, 0b10UL),
        };

        if (chargedBase)
        {
            sites.Add(new CellSite(siteZId, "Z", new Fractional3(0.25, 0.25, 0.25), setZ, SiteStability.Stable));
            compiledSites.Add(
                new CompiledSite(3, siteZId, "Z", new Fractional3(0.25, 0.25, 0.25), SiteStability.Stable, setZ, 3, [3]));
            positions.Add(CompiledPositionAt(3, siteZId, SiteStability.Stable, 3, 1UL << 7));
        }

        var project = new Project(
            null,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d9f00", CultureInfo.InvariantCulture),
                new string('a', 64),
                "Doping test",
                null,
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch),
            new ModelDefinition
            {
                Particles = [.. particles],
                ParticleSets =
                [
                    new ParticleSet(setA, "A site", [particleA, particleB, particleC, particleA2]),
                    new ParticleSet(setX, "X site", [particleX, particleY]),
                    new ParticleSet(setU, "U site", [particleA]),
                    new ParticleSet(setZ, "Z site", [particleZ]),
                ],
                Structure = StructureModel.Default with
                {
                    Name = "Doping test",
                    Sites = [.. sites],
                },
                OccupationExchanges =
                [
                    new OccupationExchange(exchangeAb, "A to B", siteAId, particleA, particleB),
                    new OccupationExchange(exchangeAc, "A to C", siteAId, particleA, particleC),
                    new OccupationExchange(exchangeAa, "A to A2", siteAId, particleA, particleA2),
                    new OccupationExchange(exchangeXy, "X to Y", siteXId, particleX, particleY),
                ],
                Dopings =
                [
                    new Doping(consumeXDoping, "Consume X", 5, exchangeXy, null, ChargeCompensation: false),
                    new Doping(
                        compensatedIntegerDoping,
                        "Compensated integer",
                        10,
                        exchangeAb,
                        exchangeXy,
                        ChargeCompensation: true),
                    new Doping(
                        compensatedFractionalDoping,
                        "Compensated fractional",
                        20,
                        exchangeAc,
                        exchangeXy,
                        ChargeCompensation: true),
                    new Doping(plainDoping, "Plain", 30, exchangeAb, null, ChargeCompensation: false),
                    new Doping(sameSiteDoping, "Same site", 40, exchangeAc, null, ChargeCompensation: false),
                    new Doping(isovalentDoping, "Isovalent", 50, exchangeAa, null, ChargeCompensation: false),
                ],
            },
            [],
            [],
            [],
            ViewSettings.Default);

        var compiledModel = new CompiledModel(
            CompiledModel.CurrentFormatVersion,
            "test-model",
            "symmetry-1",
            "2026.1",
            new string('b', 64))
        {
            Particles = new CompiledParticleTable(
            [
                new CompiledParticle(0, ParticleId.Void, "Void", "V", 0.0, IsVacancy: false),
                new CompiledParticle(1, particleA, "A", "A", 1.0, IsVacancy: false),
                new CompiledParticle(2, particleB, "B", "B", 2.0, IsVacancy: false),
                new CompiledParticle(3, particleC, "C", "C", 2.5, IsVacancy: false),
                new CompiledParticle(4, particleA2, "A2", "A2", 1.0, IsVacancy: false),
                new CompiledParticle(5, particleX, "X", "X", -1.0, IsVacancy: false),
                new CompiledParticle(6, particleY, "Y", "Y", -2.0, IsVacancy: false),
                new CompiledParticle(7, particleZ, "Z", "Z", 1.0, IsVacancy: false),
            ]),
            Structure = new CompiledStructureModel(
                [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
                125.0,
                [.. compiledSites],
                [],
                [.. positions],
                [0, 0, 0, 0]),
        };

        return (project, compiledModel);
    }

    private static CompiledPosition CompiledPositionAt(
        int position,
        CellSiteId siteId,
        SiteStability stability,
        int siteIndex,
        ulong allowedParticleMask) => new(
        position,
        new Vector4I(0, 0, 0, position),
        Fractional3.Zero,
        Cartesian3.Zero,
        siteIndex,
        siteId,
        stability,
        allowedParticleMask,
        siteIndex,
        siteIndex,
        [],
        []);

    private static ParticleId CreateParticle(string value) =>
        ParticleId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));

    private static ParticleSetId CreateSet(string value) =>
        ParticleSetId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));

    private static CellSiteId CreateSite(string value) =>
        CellSiteId.Create(Guid.Parse(value, CultureInfo.InvariantCulture));
}
