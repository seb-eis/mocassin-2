// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Transitions;

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers jump mappings, tracker definitions, and derived solver intent.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SimulationEncodingTests
{
    [Fact]
    public async Task KmcSimulationKeepsDualDofAndEmitsTrackers()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.EncodingStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var transitions = Assert.IsType<CompiledTransitionModel>(context.Transitions);
        var simulation = Assert.Single(context.CompiledSimulations);
        Assert.True(simulation.Intent.UseDualDofCorrection);
        Assert.True(simulation.Intent.UsePreRun);
        Assert.NotEmpty(transitions.GlobalTrackers);
        Assert.NotEmpty(transitions.StaticTrackers);
        Assert.NotEmpty(simulation.Trackers.GlobalTrackerIndices);
        Assert.NotEmpty(simulation.Trackers.StaticTrackerIndices);
        Assert.Equal(
            context.Structure!.PositionCount * context.Particles!.Count,
            transitions.JumpCounts.Length);
        Assert.Equal(transitions.JumpCounts.Length, transitions.JumpOffsets.Length);
        Assert.All(transitions.Rules, rule =>
        {
            Assert.Equal(rule.Movement.Length, rule.TrackerOrder.Length);
            Assert.Contains(rule.TrackerOrder, static value => value == 0xFF);
        });
        for (var index = 0; index < transitions.JumpCounts.Length; index++)
        {
            Assert.InRange(transitions.JumpOffsets[index], 0, transitions.JumpMappings.Length);
            for (var slot = 0; slot < transitions.JumpCounts[index]; slot++)
            {
                var direction = transitions.JumpMappings[transitions.JumpOffsets[index] + slot];
                Assert.InRange(direction, 0, transitions.DirectionCount - 1);
            }
        }
    }

    [Fact]
    public async Task MmcSimulationHasNoDualDofPreRunOrTrackers()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-mmc.mocprj"),
            kinetic: false);

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.EncodingStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        var simulation = Assert.Single(context.CompiledSimulations);
        Assert.False(simulation.Intent.UseDualDofCorrection);
        Assert.False(simulation.Intent.UsePreRun);
        Assert.Empty(simulation.Trackers.GlobalTrackerIndices);
        Assert.Empty(simulation.Trackers.StaticTrackerIndices);
    }

    [Fact]
    public async Task ZeroPreRunDisablesThePreRunIntent()
    {
        var project = StageTestSupport.WithFirstSimulationTransition(
            await StageTestSupport.LoadProjectAsync("samples/simulations/ceria-kmc.mocprj"),
            kinetic: true);
        var model = project.Model;
        var simulation = (Core.Model.Simulations.KineticSimulation)model.Simulations[0];
        project = project with
        {
            Model = model with { Simulations = [simulation with { PreRunMcsp = 0 }] },
        };

        var (context, diagnostics) = StageTestSupport.Run(project, StageTestSupport.EncodingStages());

        Assert.Empty(diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.False(Assert.Single(context.CompiledSimulations).Intent.UsePreRun);
    }
}
