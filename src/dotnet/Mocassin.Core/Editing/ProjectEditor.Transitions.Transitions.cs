// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds a kinetic transition.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="transition">The transition to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddKineticTransition(ProjectModel project, KineticTransition transition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(transition);
        return Apply(project, "Add kinetic transition", current => current with
        {
            Model = current.Model with
            {
                KineticTransitions = Append(current.Model.KineticTransitions, transition),
            },
        });
    }

    /// <summary>
    /// Updates a kinetic transition by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="transition">The replacement transition.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateKineticTransition(ProjectModel project, KineticTransition transition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(transition);
        if (!Contains(project.Model.KineticTransitions, candidate => candidate.Id == transition.Id))
        {
            return Unknown(
                project,
                "kineticTransition",
                transition.Id.Value,
                $"Kinetic transition '{transition.Id:D}' does not exist.");
        }

        return Apply(project, "Update kinetic transition", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.KineticTransitions,
                candidate => candidate.Id == transition.Id,
                transition,
                out var transitions)
                ? current with { Model = model with { KineticTransitions = transitions } }
                : current;
        });
    }

    /// <summary>
    /// Removes a kinetic transition, optionally removing its reverse-link dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The kinetic-transition identifier.</param>
    /// <param name="removeDependents">Whether transitions that link to it as a reverse are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveKineticTransition(
        ProjectModel project,
        KineticTransitionId id,
        bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.KineticTransitions, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "kineticTransition",
                id.Value,
                $"Kinetic transition '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetKineticTransitionDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "kineticTransition", id.Value, dependents);
            }
        }

        return Apply(project, "Remove kinetic transition", current => current with
        {
            Model = removeDependents
                ? RemoveTransitionCascade(current.Model, removeKineticTransitions: [id])
                : current.Model with
                {
                    KineticTransitions = RemoveWhere(
                        current.Model.KineticTransitions,
                        candidate => candidate.Id == id),
                },
        });
    }

    /// <summary>
    /// Duplicates a kinetic transition with a new identifier and no reverse link.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The kinetic-transition identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult DuplicateKineticTransition(ProjectModel project, KineticTransitionId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        var transition = FirstOrDefault(project.Model.KineticTransitions, candidate => candidate.Id == id);
        if (transition is null)
        {
            return Unknown(
                project,
                "kineticTransition",
                id.Value,
                $"Kinetic transition '{id:D}' does not exist.");
        }

        var duplicate = transition with
        {
            Id = KineticTransitionId.New(),
            Name = MakeUniqueName(project.Model.KineticTransitions.Select(static item => item.Name), transition.Name),
            ReverseTransitionId = null,
        };
        return AddKineticTransition(project, duplicate);
    }

    /// <summary>
    /// Adds a Metropolis transition.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="transition">The transition to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddMetropolisTransition(ProjectModel project, MetropolisTransition transition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(transition);
        return Apply(project, "Add Metropolis transition", current => current with
        {
            Model = current.Model with
            {
                MetropolisTransitions = Append(current.Model.MetropolisTransitions, transition),
            },
        });
    }

    /// <summary>
    /// Updates a Metropolis transition by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="transition">The replacement transition.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateMetropolisTransition(ProjectModel project, MetropolisTransition transition)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(transition);
        if (!Contains(project.Model.MetropolisTransitions, candidate => candidate.Id == transition.Id))
        {
            return Unknown(
                project,
                "metropolisTransition",
                transition.Id.Value,
                $"Metropolis transition '{transition.Id:D}' does not exist.");
        }

        return Apply(project, "Update Metropolis transition", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.MetropolisTransitions,
                candidate => candidate.Id == transition.Id,
                transition,
                out var transitions)
                ? current with { Model = model with { MetropolisTransitions = transitions } }
                : current;
        });
    }

    /// <summary>
    /// Removes a Metropolis transition, optionally removing its reverse-link dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The Metropolis-transition identifier.</param>
    /// <param name="removeDependents">Whether transitions that link to it as a reverse are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveMetropolisTransition(
        ProjectModel project,
        MetropolisTransitionId id,
        bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.MetropolisTransitions, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "metropolisTransition",
                id.Value,
                $"Metropolis transition '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetMetropolisTransitionDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "metropolisTransition", id.Value, dependents);
            }
        }

        return Apply(project, "Remove Metropolis transition", current => current with
        {
            Model = removeDependents
                ? RemoveTransitionCascade(current.Model, removeMetropolisTransitions: [id])
                : current.Model with
                {
                    MetropolisTransitions = RemoveWhere(
                        current.Model.MetropolisTransitions,
                        candidate => candidate.Id == id),
                },
        });
    }

    /// <summary>
    /// Duplicates a Metropolis transition with a new identifier and no reverse link.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The Metropolis-transition identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult DuplicateMetropolisTransition(ProjectModel project, MetropolisTransitionId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        var transition = FirstOrDefault(project.Model.MetropolisTransitions, candidate => candidate.Id == id);
        if (transition is null)
        {
            return Unknown(
                project,
                "metropolisTransition",
                id.Value,
                $"Metropolis transition '{id:D}' does not exist.");
        }

        var duplicate = transition with
        {
            Id = MetropolisTransitionId.New(),
            Name = MakeUniqueName(project.Model.MetropolisTransitions.Select(static item => item.Name), transition.Name),
            ReverseTransitionId = null,
        };
        return AddMetropolisTransition(project, duplicate);
    }
}
