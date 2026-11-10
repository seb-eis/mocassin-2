// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditorLatticeSimulationTests
{
    private static readonly DopingId[] reorderedIds =
    [
        LatticeTestModels.IsovalentDoping.Id,
        LatticeTestModels.CeriaReductionDoping.Id,
    ];

    private static readonly int[] reorderedPriorities = [0, 1];

    [Fact]
    public void AddUpdateRemoveOccupationExchangeAndDoping()
    {
        var editor = new ProjectEditor();
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var exchange = new OccupationExchange(
            OccupationExchangeId.New(),
            "Second Ce/Zr exchange",
            LatticeTestModels.CeSite.Id,
            LatticeTestModels.Ce4Plus.Id,
            LatticeTestModels.Zr4Plus.Id);

        var added = editor.AddOccupationExchange(project, exchange);
        Assert.True(added.IsSuccess);
        var updated = editor.UpdateOccupationExchange(
            added.Project,
            exchange with { Name = "Ce/Zr exchange" });
        Assert.True(updated.IsSuccess);

        var doping = new Doping(
            DopingId.New(),
            "Second Zr doping",
            2,
            exchange.Id,
            DependentExchangeId: null,
            ChargeCompensation: false);
        var dopingAdded = editor.AddDoping(updated.Project, doping);
        Assert.True(dopingAdded.IsSuccess);
        var dopingUpdated = editor.UpdateDoping(
            dopingAdded.Project,
            doping with { Name = "Zr substitution" });
        Assert.True(dopingUpdated.IsSuccess);

        var dopingRemoved = editor.RemoveDoping(dopingUpdated.Project, doping.Id);
        Assert.True(dopingRemoved.IsSuccess);
        Assert.True(editor.RemoveOccupationExchange(dopingRemoved.Project, exchange.Id).IsSuccess);
    }

    [Fact]
    public void RemovingReferencedOccupationExchangeReportsDependents()
    {
        var editor = new ProjectEditor();
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        var rejected = editor.RemoveOccupationExchange(project, LatticeTestModels.CeReduction.Id);

        Assert.False(rejected.IsSuccess);
        var diagnostic = Assert.Single(rejected.Diagnostics);
        Assert.Equal(ProjectDiagnosticCodes.InvalidDocument, diagnostic.Code);
        Assert.NotEmpty(diagnostic.Related);
        Assert.Same(project, rejected.Project);

        var cascaded = editor.RemoveOccupationExchange(
            project,
            LatticeTestModels.CeReduction.Id,
            removeDependents: true);

        Assert.True(cascaded.IsSuccess);
        Assert.DoesNotContain(
            cascaded.Project.Model.Dopings,
            doping => doping.Id == LatticeTestModels.CeriaReductionDoping.Id);
    }

    [Fact]
    public void ReorderDopingsRenumbersPrioritiesDeterministically()
    {
        var editor = new ProjectEditor();
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        ImmutableArray<DopingId> order =
        [
            LatticeTestModels.IsovalentDoping.Id,
            LatticeTestModels.CeriaReductionDoping.Id,
        ];

        var result = editor.ReorderDopings(project, order);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            reorderedIds,
            result.Project.Model.Dopings.Select(static doping => doping.Id).ToArray());
        Assert.Equal(
            reorderedPriorities,
            result.Project.Model.Dopings.Select(static doping => doping.Priority).ToArray());
    }

    [Fact]
    public void ReorderDopingsRejectsIncompleteOrDuplicateOrders()
    {
        var editor = new ProjectEditor();
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        Assert.False(editor.ReorderDopings(
            project,
            [LatticeTestModels.CeriaReductionDoping.Id]).IsSuccess);
        Assert.False(editor.ReorderDopings(
            project,
            [
                LatticeTestModels.CeriaReductionDoping.Id,
                LatticeTestModels.CeriaReductionDoping.Id,
            ]).IsSuccess);
        Assert.False(editor.ReorderDopings(
            project,
            [DopingId.New(), LatticeTestModels.CeriaReductionDoping.Id]).IsSuccess);
    }

    [Fact]
    public void AddUpdateRemoveAndDuplicateSimulation()
    {
        var editor = new ProjectEditor();
        var project = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());
        var metropolis = SimulationTestModels.CreateMetropolis() with
        {
            Id = SimulationId.New(),
            Name = "Second MMC simulation",
        };

        var added = editor.AddSimulation(project, metropolis);
        Assert.True(added.IsSuccess);
        Assert.Contains(added.Project.Model.Simulations, simulation => simulation.Id == metropolis.Id);

        var updated = editor.UpdateSimulation(
            added.Project,
            metropolis with { TemperatureK = 900.0 });
        Assert.True(updated.IsSuccess);
        Assert.Equal(
            900.0,
            updated.Project.Model.Simulations.Single(
                simulation => simulation.Id == metropolis.Id).TemperatureK);

        var duplicated = editor.DuplicateSimulation(updated.Project, SimulationTestModels.KineticId);
        Assert.True(duplicated.IsSuccess);
        var duplicate = duplicated.Project.Model.Simulations.Single(
            simulation => simulation.Id != SimulationTestModels.KineticId &&
                simulation.Id != metropolis.Id);
        Assert.NotEqual(SimulationTestModels.KineticId, duplicate.Id);
        Assert.Contains("copy", duplicate.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            SimulationTestModels.CreateKinetic().TransitionIds.ToArray(),
            duplicate.TransitionIds.ToArray());

        Assert.True(editor.RemoveSimulation(duplicated.Project, duplicate.Id).IsSuccess);
        Assert.True(editor.RemoveSimulation(duplicated.Project, metropolis.Id).IsSuccess);
    }

    [Fact]
    public void SwitchKindRequiresEmptyTransitionsAndChangesTheConcreteType()
    {
        var editor = new ProjectEditor();
        var withTransitions = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());

        var rejected = editor.SwitchSimulationKind(
            withTransitions,
            SimulationTestModels.KineticId,
            SimulationKind.Mmc);

        Assert.False(rejected.IsSuccess);
        Assert.Contains(
            rejected.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);

        var emptyKinetic = SimulationTestModels.CreateKinetic([]) with { TemperatureK = 900.0 };
        var emptyProject = SimulationTestModels.CreateProject(emptyKinetic);
        var switched = editor.SwitchSimulationKind(
            emptyProject,
            SimulationTestModels.KineticId,
            SimulationKind.Mmc);

        Assert.True(switched.IsSuccess);
        var simulation = Assert.IsType<MetropolisSimulation>(Assert.Single(switched.Project.Model.Simulations));
        Assert.Equal(SimulationTestModels.KineticId, simulation.Id);
        Assert.Equal(900.0, simulation.TemperatureK);
        Assert.Empty(simulation.TransitionIds);
    }

    [Fact]
    public void SwitchKindToTheSameKindIsANoOp()
    {
        var editor = new ProjectEditor();
        var project = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());

        var result = editor.SwitchSimulationKind(
            project,
            SimulationTestModels.KineticId,
            SimulationKind.Kmc);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Same(project, result.Project);
    }

    [Fact]
    public void UpdateSimulationRejectsAKindChange()
    {
        var editor = new ProjectEditor();
        var project = SimulationTestModels.CreateProject(SimulationTestModels.CreateKinetic());
        var replacement = SimulationTestModels.CreateMetropolis() with
        {
            Id = SimulationTestModels.KineticId,
            Name = "Changed kind",
        };

        var result = editor.UpdateSimulation(project, replacement);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidDocument);
    }
}
