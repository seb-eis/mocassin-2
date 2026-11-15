// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Packages.Jobs.Optimization;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class SelectionOptimizerResolverTests
{
    private static readonly ParticleId particleA =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df001"));

    private static readonly ParticleId particleB =
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df002"));

    private static readonly ParticleSetId setA =
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df003"));

    private static readonly CellSiteId site0 =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df004"));

    private static readonly CellSiteId site1 =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df005"));

    private static readonly CellSiteId site2 =
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df006"));

    private static readonly SimulationId simulationId =
        SimulationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df007"));

    private static readonly JobCollectionId collectionId =
        JobCollectionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4df008"));

    [Fact]
    public void MapsEveryP1PositionOfTheSiteAndSortsTheCompiledTargets()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);
        ImmutableArray<SelectionOptimizer> optimizers =
        [
            new SelectionOptimizer(site0, particleA),
            new SelectionOptimizer(site1, particleB),
        ];

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            optimizers,
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(2, plan.AuthoredOptimizerCount);
        Assert.Equal(
            [
                new CompiledSelectionOptimizer(0, 1),
                new CompiledSelectionOptimizer(1, 1),
                new CompiledSelectionOptimizer(2, 2),
            ],
            plan.Targets.ToArray());
        Assert.Equal(
            [new CompiledSelectionOptimizer(0, 1), new CompiledSelectionOptimizer(1, 1)],
            plan.Effects[0].Targets.ToArray());
        Assert.Equal([new CompiledSelectionOptimizer(2, 2)], plan.Effects[1].Targets.ToArray());
        Assert.False(plan.StartOptionCheckEvaluated);
        Assert.Equal(SelectionOptimizerResolver.MissingJumpTableLimitation, plan.StartOptionCheckLimitation);
    }

    [Fact]
    public void ResolutionIsDeterministicForTheSameInputs()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);
        ImmutableArray<SelectionOptimizer> optimizers = [new SelectionOptimizer(site0, particleA)];

        var first = SelectionOptimizerResolver.Resolve(model, simulation, collectionId, optimizers, out _);
        var second = SelectionOptimizerResolver.Resolve(model, simulation, collectionId, optimizers, out _);

        Assert.Equal(first.Targets.ToArray(), second.Targets.ToArray());
        Assert.Equal(first.UseDualDofCorrection, second.UseDualDofCorrection);
        Assert.Equal(first.Effects[0].Targets.ToArray(), second.Effects[0].Targets.ToArray());
    }

    [Fact]
    public void NoOptimizerKeepsTheCompiledDualDofIntent()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [],
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(plan.Targets);
        Assert.Equal(0, plan.AuthoredOptimizerCount);
        Assert.True(plan.UseDualDofCorrection);
    }

    [Fact]
    public void OneReducedEnvironmentKeepsDualDof()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out _);

        Assert.True(plan.UseDualDofCorrection);
    }

    [Fact]
    public void SomeReducedEnvironmentsKeepDualDof()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA), new SelectionOptimizer(site1, particleA)],
            out _);

        Assert.True(plan.UseDualDofCorrection);
    }

    [Fact]
    public void EveryReducedEnvironmentClearsDualDof()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [
                new SelectionOptimizer(site0, particleA),
                new SelectionOptimizer(site1, particleA),
                new SelectionOptimizer(site2, particleA),
            ],
            out _);

        Assert.False(plan.UseDualDofCorrection);
    }

    [Fact]
    public void AlreadyClearDualDofStaysClear()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: false);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out _);

        Assert.False(plan.UseDualDofCorrection);
    }

    [Fact]
    public void MmcIgnoresOptimizersAndKeepsDualDofClear()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Mmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Empty(plan.Targets);
        Assert.Empty(plan.Effects);
        Assert.False(plan.UseDualDofCorrection);
    }

    [Fact]
    public void InvalidSiteOrParticleIsJob009AndNeverThrows()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(CellSiteId.New(), particleA), new SelectionOptimizer(site0, ParticleId.New())],
            out var diagnostics);

        Assert.Empty(plan.Targets);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, static diagnostic =>
        {
            Assert.Equal(JobDiagnosticCodes.InvalidOptimizer, diagnostic.Code);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        });
    }

    [Fact]
    public void RemovingTheOnlyStartOptionIsJob009()
    {
        var model = CreateStartOptionModel(positionCount: 1, startPositions: [0]);
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.True(plan.StartOptionCheckEvaluated);
        Assert.Null(plan.StartOptionCheckLimitation);
        Assert.Equal(1, plan.Effects[0].RemovedStartOptions);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(JobDiagnosticCodes.InvalidOptimizer, diagnostic.Code);
        Assert.Contains("every selectable start option", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingOneOfTwoStartOptionsIsAllowed()
    {
        var model = CreateStartOptionModel(positionCount: 2, startPositions: [0, 1]);
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.True(plan.StartOptionCheckEvaluated);
        Assert.Equal(1, plan.Effects[0].RemovedStartOptions);
    }

    [Fact]
    public void RemovingAllStartOptionsOfOneCollectionIsJob009()
    {
        var model = CreateStartOptionModel(positionCount: 1, startPositions: [0], collectionCount: 2);
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true, activeCollections: [0, 1]);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.True(plan.StartOptionCheckEvaluated);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, static diagnostic =>
            Assert.Equal(JobDiagnosticCodes.InvalidOptimizer, diagnostic.Code));
    }

    [Fact]
    public void MissingJumpTableRecordsThePreciseLimitationInsteadOfJob009()
    {
        var model = CreateEnvironmentModel();
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.False(plan.StartOptionCheckEvaluated);
        Assert.Equal(SelectionOptimizerResolver.MissingJumpTableLimitation, plan.StartOptionCheckLimitation);
    }

    [Fact]
    public void MissingActiveCollectionRecordsThePreciseLimitation()
    {
        var model = CreateStartOptionModel(positionCount: 1, startPositions: [0]);
        var simulation = CreateSimulation(SimulationKind.Kmc, useDualDof: true, activeCollections: []);

        var plan = SelectionOptimizerResolver.Resolve(
            model,
            simulation,
            collectionId,
            [new SelectionOptimizer(site0, particleA)],
            out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.False(plan.StartOptionCheckEvaluated);
        Assert.Equal(SelectionOptimizerResolver.NoActiveCollectionLimitation, plan.StartOptionCheckLimitation);
    }

    private static CompiledModel CreateEnvironmentModel() => new(
        CompiledModel.CurrentFormatVersion,
        "test-model",
        "symmetry-1",
        "catalog-1",
        new string('0', 64))
    {
        Particles = new CompiledParticleTable(
        [
            new CompiledParticle(0, ParticleId.Void, "Void", "V", 0.0, IsVacancy: false),
            new CompiledParticle(1, particleA, "A", "A", 1.0, IsVacancy: false),
            new CompiledParticle(2, particleB, "B", "B", 2.0, IsVacancy: false),
        ]),
        Structure = new CompiledStructureModel(
            [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
            125.0,
            [
                new CompiledSite(0, site0, "S0", Fractional3.Zero, SiteStability.Stable, setA, 0, [1, 0]),
                new CompiledSite(1, site1, "S1", Fractional3.Zero, SiteStability.Stable, setA, 1, [2]),
                new CompiledSite(2, site2, "S2", Fractional3.Zero, SiteStability.Stable, setA, 2, [3]),
            ],
            [
                new CompiledEnvironment(0, 0, SiteStability.Stable, 0b10UL, [1]),
                new CompiledEnvironment(1, 1, SiteStability.Stable, 0b110UL, [1, 2]),
                new CompiledEnvironment(2, 2, SiteStability.Stable, 0b110UL, [1, 2]),
            ],
            [
                Position(0, site0, 0, 0b10UL),
                Position(1, site0, 0, 0b10UL),
                Position(2, site1, 1, 0b110UL),
                Position(3, site2, 2, 0b110UL),
            ],
            [0, 0, 0]),
        Transitions = CompiledTransitionModel.Empty,
    };

    private static CompiledModel CreateStartOptionModel(
        int positionCount,
        int[] startPositions,
        int collectionCount = 1)
    {
        var siteIds = new[] { site0, site1, site2 };
        var positions = Enumerable.Range(0, positionCount)
            .Select(index => Position(index, siteIds[index], 0, 0b10UL))
            .ToImmutableArray();
        var sites = Enumerable.Range(0, positionCount)
            .Select(index => new CompiledSite(
                index,
                siteIds[index],
                $"S{index}",
                Fractional3.Zero,
                SiteStability.Stable,
                setA,
                0,
                [index]))
            .ToImmutableArray();
        var jumpCounts = new int[positionCount * 2];
        foreach (var position in startPositions)
        {
            jumpCounts[(position * 2) + 1] = 1;
        }

        var collections = Enumerable.Range(0, collectionCount)
            .Select(index => new CompiledJumpCollection(
                index,
                TransitionRef.From(KineticTransitionId.New()),
                SimulationKind.Kmc,
                0b10UL,
                [],
                []))
            .ToImmutableArray();
        return new CompiledModel(
            CompiledModel.CurrentFormatVersion,
            "test-model",
            "symmetry-1",
            "catalog-1",
            new string('0', 64))
        {
            Particles = new CompiledParticleTable(
            [
                new CompiledParticle(0, ParticleId.Void, "Void", "V", 0.0, IsVacancy: false),
                new CompiledParticle(1, particleA, "A", "A", 1.0, IsVacancy: false),
            ]),
            Structure = new CompiledStructureModel(
                [new Cartesian3(5.0, 0.0, 0.0), new Cartesian3(0.0, 5.0, 0.0), new Cartesian3(0.0, 0.0, 5.0)],
                125.0,
                sites,
                [new CompiledEnvironment(0, 0, SiteStability.Stable, 0b10UL, [1])],
                positions,
                [0, 0, 0]),
            Transitions = new CompiledTransitionModel(
                collections,
                [],
                [],
                [.. jumpCounts],
                ImmutableArray.CreateRange(new int[positionCount * 2]),
                [],
                2,
                positionCount),
        };
    }

    private static CompiledPosition Position(
        int index,
        CellSiteId siteId,
        int environmentIndex,
        ulong mask) => new(
        index,
        new Vector4I(0, 0, 0, index),
        Fractional3.Zero,
        Cartesian3.Zero,
        0,
        siteId,
        SiteStability.Stable,
        mask,
        environmentIndex,
        0,
        [],
        []);

    private static CompiledSimulation CreateSimulation(
        SimulationKind kind,
        bool useDualDof,
        int[]? activeCollections = null)
    {
        var settings = new CompiledRuntimeSettings(
            TemperatureK: 900.0,
            TargetMcsp: 500,
            WriteOutCount: 7,
            TimeLimitSeconds: null,
            MinSuccessRate: null,
            JobCount: 1,
            RngSeed: null,
            Flags: ExecutionFlags.Deterministic,
            Instruction: null,
            PreRunMcsp: kind == SimulationKind.Kmc ? 50 : 0,
            ElectricFieldModulusVm: 0.0,
            ElectricFieldDirection: Fractional3.Zero,
            NormalizationEnergyEv: null,
            MaxAttemptFrequencyHz: null,
            BreakTolerance: 0.01,
            BreakSampleLengthMcsp: 1000,
            BreakSampleIntervalMcsp: 100,
            ResultSampleMcsp: 200);

        return new CompiledSimulation(
            simulationId,
            "simulation",
            kind,
            activeCollections is null ? [0] : [.. activeCollections],
            settings,
            new CompiledSolverIntent(kind, useDualDof, false, 0),
            CompiledTrackerModel.Empty);
    }
}
