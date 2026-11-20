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
/// Covers translation-equivalent starts in a centered space-group setting.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class EquivalentStartCompilationTests
{
    [Fact]
    public async Task CenteredGroupMergesTranslationEquivalentStartsAndMapsEveryBucket()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/simulations/centered-exchange.mocprj");

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        var transitions = result.Model!.Transitions;

        // Four C-centering images for each of the two reference sites.
        Assert.Equal(8, transitions.PositionCount);

        // Two rotational variants per direction sense. The two centering images of each variant are merged,
        // so the compiler emits two forward and two inverse directions instead of one per image.
        Assert.Equal(4, transitions.DirectionCount);
        Assert.Equal(2, transitions.Directions.Count(static direction => !direction.IsInverse));
        Assert.Equal(8, transitions.RuleCount);

        foreach (var direction in transitions.Directions)
        {
            Assert.Equal(2, direction.EquivalentStartPositions.Length);
            Assert.Equal(
                direction.EquivalentStartPositions.Length,
                direction.EquivalentStartPositions.Distinct().Count());
            Assert.Contains(direction.PositionId, direction.EquivalentStartPositions);
        }

        // The two forward variants are genuinely different paths and must not be merged by the translation
        // reduction.
        var forward = transitions.Directions.Where(static direction => !direction.IsInverse).ToArray();
        Assert.Equal(2, forward.Select(static direction => direction.ObjectId).Distinct().Count());
        Assert.NotEqual(forward[0].Movement[0].C, forward[1].Movement[0].C);

        // The encoded jump_mapping must place at least one direction in a bucket whose position is not the
        // direction's representative position_id. This is the property Step 13/16 consume.
        var crossBucket = CrossBucketMappings(transitions);
        Assert.NotEmpty(crossBucket);

        // Every equivalent start bucket carries the direction for each mobile particle with a rule.
        foreach (var direction in transitions.Directions)
        {
            foreach (var start in direction.EquivalentStartPositions)
            {
                for (var particle = 1; particle < transitions.ParticleCount; particle++)
                {
                    var bucket = (start * transitions.ParticleCount) + particle;
                    Assert.True(
                        transitions.JumpCounts[bucket] > 0,
                        $"Direction {direction.Index} is missing from bucket (position {start}, particle {particle}).");
                    var mapped = Enumerable.Range(0, transitions.JumpCounts[bucket])
                        .Select(slot => transitions.JumpMappings[transitions.JumpOffsets[bucket] + slot])
                        .ToArray();
                    Assert.Contains(direction.Index, mapped);
                }
            }
        }
    }

    [Fact]
    public async Task SingleStartDirectionKeepsTheEmptyEquivalentStartRepresentation()
    {
        var project = await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj");

        var result = new ModelCompiler().Compile(project);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Diagnostics.Select(static item => item.ToString())));
        Assert.All(
            result.Model!.Transitions.Directions,
            static direction => Assert.Empty(direction.EquivalentStartPositions));
    }

    private static (int Position, int Direction)[] CrossBucketMappings(CompiledTransitionModel transitions)
    {
        var mappings = new List<(int Position, int Direction)>();
        for (var bucket = 0; bucket < transitions.JumpCounts.Length; bucket++)
        {
            var position = bucket / transitions.ParticleCount;
            for (var slot = 0; slot < transitions.JumpCounts[bucket]; slot++)
            {
                var direction = transitions.JumpMappings[transitions.JumpOffsets[bucket] + slot];
                if (transitions.Directions[direction].PositionId != position)
                {
                    mappings.Add((position, direction));
                }
            }
        }

        return [.. mappings];
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
