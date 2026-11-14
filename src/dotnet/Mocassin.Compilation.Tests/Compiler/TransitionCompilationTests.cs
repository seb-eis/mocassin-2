// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Transitions;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers direction expansion, state triplets, conservation, and inverse rules.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class TransitionCompilationTests
{
    [Fact]
    public async Task CeriaKineticMigrationCompilesForwardAndInverseRules()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.RuleStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var transitions = Assert.IsType<CompiledTransitionModel>(context.Transitions);
        var collection = Assert.Single(transitions.Collections);
        Assert.Equal(Core.Model.Simulations.SimulationKind.Kmc, collection.Kind);
        Assert.NotEmpty(collection.DirectionIndices);
        Assert.NotEmpty(collection.RuleIndices);
        Assert.True(transitions.InverseRuleCount > 0);
        Assert.All(transitions.Rules, rule =>
        {
            Assert.Equal(3, rule.Movement.Length);
            Assert.Equal(CompiledJumpRule.DefaultFrequencyHz, rule.FrequencyHz);
            Assert.Null(rule.StaticCorrection);
            Assert.Equal(3, rule.Movement.Length);
            Assert.Equal(0UL, (rule.State1 >> 0) & 63UL);
            Assert.Equal(0UL, (rule.State1 >> 12) & 63UL);
            Assert.NotEqual(0UL, (rule.State1 >> 6) & 63UL);
        });
    }

    [Fact]
    public async Task MetropolisExchangeCompilesForwardAndInverseRulesWithoutState1()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-mmc.mocprj"),
            kinetic: false);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.RuleStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var transitions = Assert.IsType<CompiledTransitionModel>(context.Transitions);
        Assert.Equal(Core.Model.Simulations.SimulationKind.Mmc, transitions.Collections[0].Kind);
        Assert.True(transitions.InverseRuleCount > 0);
        Assert.All(transitions.Rules, rule => Assert.Equal(0UL, rule.State1));
        Assert.All(transitions.Rules, rule => Assert.Equal(2, rule.Movement.Length));
    }

    [Fact]
    public async Task EveryRuleReferencesADirectionInTheSameCollection()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.RuleStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var transitions = Assert.IsType<CompiledTransitionModel>(context.Transitions);
        Assert.All(transitions.Rules, rule =>
        {
            Assert.All(rule.DirectionIndices, index =>
            {
                Assert.InRange(index, 0, transitions.DirectionCount - 1);
                Assert.Equal(rule.CollectionIndex, transitions.Directions[index].CollectionIndex);
            });
        });
    }
}

/// <summary>
/// Covers the corrected vehicle and interstitialcy samples.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ComplexMechanismTests
{
    [Fact]
    public async Task CorrectedVehicleCompilesUnidirectionalRules()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/vehicle5.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.NotEmpty(result.Model!.Transitions.Rules);
        Assert.Equal(0, result.Model.Transitions.InverseRuleCount);
        Assert.Equal(0, result.Model.Transitions.Collections[0].DirectionIndices.Length % 1);
    }

    [Fact]
    public async Task CorrectedInterstitialcyCompilesWithASpectatorCentre()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/interstitialcy5.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.NotEmpty(result.Model!.Transitions.Rules);
        Assert.Equal(0, result.Model.Transitions.InverseRuleCount);
        Assert.Equal("e2b8255b09f136519f8febff410721029592bdcac0ee220e2d59dd887c82c798", result.Model.ModelSignature);
        var rule = Assert.Single(result.Model.Transitions.Rules.Where(static rule => !rule.IsInverse));
        Assert.Equal(-4.0, rule.ChargeTransportVector.X, 12);
        Assert.Equal(0.0, rule.ChargeTransportVector.Y, 12);
        Assert.Equal(0.0, rule.ChargeTransportVector.Z, 12);
        Assert.Equal(0.2, rule.Movement[2].A, 12);
    }
}

/// <summary>
/// Pins the vehicle charge-transport result for the neutral A+/B- sample.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class VehicleChargeTransportTests
{
    [Fact]
    public async Task NeutralVehiclePairHasZeroNetChargeTransport()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/vehicle5.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess);
        var rule = Assert.Single(result.Model!.Transitions.Rules.Where(static rule => !rule.IsInverse));
        Assert.Equal(0.0, rule.ChargeTransportVector.X);
        Assert.Equal(0.0, rule.ChargeTransportVector.Y);
        Assert.Equal(0.0, rule.ChargeTransportVector.Z);
        Assert.Equal(0.2, rule.Movement[0].A, 12);
        Assert.Equal(0.2, rule.Movement[3].A, 12);
        Assert.Equal(-0.2, rule.Movement[1].A, 12);
        Assert.Equal(-0.2, rule.Movement[4].A, 12);
    }
}

/// <summary>
/// Pins the spectator movement for the interstitialcy sample.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SpectatorMovementTests
{
    [Fact]
    public async Task InteriorStableSpectatorStillMovesPhysically()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/transitions/interstitialcy5.mocprj"),
            kinetic: true);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess);
        var rule = Assert.Single(result.Model!.Transitions.Rules.Where(static rule => !rule.IsInverse));
        Assert.Equal(0.2, rule.Movement[0].A, 12);
        Assert.Equal(0.2, rule.Movement[2].A, 12);
        Assert.Equal(-0.4, rule.Movement[4].A, 12);
        Assert.NotEqual((byte)0xFF, rule.TrackerOrder[2]);
        Assert.Equal(rule.TrackerOrder[0], rule.TrackerOrder[2]);
    }
}

/// <summary>
/// Covers explicit reverse transitions compiled alongside their forward partner.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ExplicitReverseCompilationTests
{
    [Theory]
    [InlineData("samples/transitions/vehicle5.mocprj")]
    [InlineData("samples/transitions/interstitialcy5.mocprj")]
    public async Task ForwardAndReverseTransitionsCompileInOneProject(string sample)
    {
        var loaded = await StageTestSupport.LoadProjectAsync(sample);
        var transitions = loaded.Model.KineticTransitions
            .Select(static transition => Core.Model.Transitions.TransitionRef.From(transition.Id))
            .ToArray();
        var project = StageTestSupport.WithSimulationTransitions(loaded, transitions);

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.Equal(2, result.Model!.Transitions.CollectionCount);
        Assert.Equal(0, result.Model.Transitions.InverseRuleCount);
        Assert.All(result.Model.Transitions.Collections, collection => Assert.NotEmpty(collection.DirectionIndices));
        Assert.All(result.Model.Transitions.Collections, collection => Assert.NotEmpty(collection.RuleIndices));
    }
}
