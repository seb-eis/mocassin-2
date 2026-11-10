// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Model.Simulations;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds a simulation.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="simulation">The simulation to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddSimulation(ProjectModel project, Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(simulation);
        return Apply(project, "Add simulation", current => current with
        {
            Model = current.Model with
            {
                Simulations = Append(current.Model.Simulations, simulation),
            },
        });
    }

    /// <summary>
    /// Updates a simulation by identifier without changing its kind.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="simulation">The replacement simulation.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateSimulation(ProjectModel project, Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(simulation);
        var existing = FirstOrDefault(
            project.Model.Simulations,
            candidate => candidate.Id == simulation.Id);
        if (existing is null)
        {
            return Unknown(
                project,
                "simulation",
                simulation.Id.Value,
                $"Simulation '{simulation.Id:D}' does not exist.");
        }

        if (existing.Kind != simulation.Kind)
        {
            return InvalidEdit(
                project,
                "simulation",
                simulation.Id.Value,
                "Changing the simulation kind requires SwitchSimulationKind.",
                "kind");
        }

        return Apply(project, "Update simulation", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.Simulations,
                candidate => candidate.Id == simulation.Id,
                simulation,
                out var simulations)
                ? current with { Model = model with { Simulations = simulations } }
                : current;
        });
    }

    /// <summary>
    /// Removes a simulation.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The simulation identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveSimulation(ProjectModel project, SimulationId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Simulations, candidate => candidate.Id == id))
        {
            return Unknown(project, "simulation", id.Value, $"Simulation '{id:D}' does not exist.");
        }

        return Apply(project, "Remove simulation", current => current with
        {
            Model = current.Model with
            {
                Simulations = RemoveWhere(current.Model.Simulations, candidate => candidate.Id == id),
            },
        });
    }

    /// <summary>
    /// Duplicates a simulation with a new identifier, the same settings, and a copied authored-order
    /// transition list.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The simulation identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult DuplicateSimulation(ProjectModel project, SimulationId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        var simulation = FirstOrDefault(project.Model.Simulations, candidate => candidate.Id == id);
        if (simulation is null)
        {
            return Unknown(project, "simulation", id.Value, $"Simulation '{id:D}' does not exist.");
        }

        var duplicate = simulation with
        {
            Id = SimulationId.New(),
            Name = MakeUniqueName(
                (project.Model.Simulations.IsDefault
                    ? []
                    : project.Model.Simulations)
                .Select(static candidate => candidate.Name),
                simulation.Name),
        };
        return AddSimulation(project, duplicate);
    }

    /// <summary>
    /// Switches a simulation between KMC and MMC by deleting and creating the concrete record.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The simulation identifier.</param>
    /// <param name="kind">The target simulation kind.</param>
    /// <returns>The edit result.</returns>
    /// <remarks>
    /// Transition references are kind-specific and must be removed before switching. Kind-specific settings are
    /// reset to the target kind's defaults; common settings and the identifier are preserved.
    /// </remarks>
    public ProjectEditResult SwitchSimulationKind(ProjectModel project, SimulationId id, SimulationKind kind)
    {
        ArgumentNullException.ThrowIfNull(project);
        var simulation = FirstOrDefault(project.Model.Simulations, candidate => candidate.Id == id);
        if (simulation is null)
        {
            return Unknown(project, "simulation", id.Value, $"Simulation '{id:D}' does not exist.");
        }

        if (kind is not SimulationKind.Kmc and not SimulationKind.Mmc)
        {
            return InvalidEdit(
                project,
                "simulation",
                id.Value,
                "Unknown target simulation kind.",
                "kind");
        }

        if (simulation.Kind == kind)
        {
            return new ProjectEditResult(project, null, []);
        }

        if (!simulation.TransitionIds.IsDefaultOrEmpty)
        {
            return InvalidEdit(
                project,
                "simulation",
                id.Value,
                "Remove the transition references before switching the simulation kind.",
                "transitionIds");
        }

        var switched = CreateSimulationOfKind(simulation, kind);
        return Apply(project, "Switch simulation kind", current =>
            TryReplace(
                current.Model.Simulations,
                candidate => candidate.Id == id,
                switched,
                out var simulations)
                ? current with { Model = current.Model with { Simulations = simulations } }
                : current);
    }

    private static Simulation CreateSimulationOfKind(Simulation source, SimulationKind kind) => kind switch
    {
        SimulationKind.Kmc => new KineticSimulation
        {
            Id = source.Id,
            Name = source.Name,
            TemperatureK = source.TemperatureK,
            TargetMcsp = source.TargetMcsp,
            WriteOutCount = source.WriteOutCount,
            TimeLimitSeconds = source.TimeLimitSeconds,
            MinSuccessRate = source.MinSuccessRate,
            JobCount = source.JobCount,
            RngSeed = source.RngSeed,
            Flags = source.Flags,
            Instruction = source.Instruction,
            TransitionIds = [],
        },
        SimulationKind.Mmc => new MetropolisSimulation
        {
            Id = source.Id,
            Name = source.Name,
            TemperatureK = source.TemperatureK,
            TargetMcsp = source.TargetMcsp,
            WriteOutCount = source.WriteOutCount,
            TimeLimitSeconds = source.TimeLimitSeconds,
            MinSuccessRate = source.MinSuccessRate,
            JobCount = source.JobCount,
            RngSeed = source.RngSeed,
            Flags = source.Flags,
            Instruction = source.Instruction,
            TransitionIds = [],
        },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown simulation kind."),
    };
}
