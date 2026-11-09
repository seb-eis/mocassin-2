// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Model.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds a state change.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="stateChange">The state change to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddStateChange(ProjectModel project, StateChange stateChange)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(stateChange);
        return Apply(project, "Add state change", current => current with
        {
            Model = current.Model with { StateChanges = Append(current.Model.StateChanges, stateChange) },
        });
    }

    /// <summary>
    /// Updates a state change by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="stateChange">The replacement state change.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateStateChange(ProjectModel project, StateChange stateChange)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(stateChange);
        if (!Contains(project.Model.StateChanges, candidate => candidate.Id == stateChange.Id))
        {
            return Unknown(project, "stateChange", stateChange.Id.Value, $"State change '{stateChange.Id:D}' does not exist.");
        }

        return Apply(project, "Update state change", current =>
        {
            var model = current.Model;
            return TryReplace(model.StateChanges, candidate => candidate.Id == stateChange.Id, stateChange, out var stateChanges)
                ? current with { Model = model with { StateChanges = stateChanges } }
                : current;
        });
    }

    /// <summary>
    /// Removes a state change, optionally removing its dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The state-change identifier.</param>
    /// <param name="removeDependents">Whether referencing groups, chains, and transitions are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveStateChange(ProjectModel project, Ids.StateChangeId id, bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.StateChanges, candidate => candidate.Id == id))
        {
            return Unknown(project, "stateChange", id.Value, $"State change '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetStateChangeDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "stateChange", id.Value, dependents);
            }
        }

        return Apply(project, "Remove state change", current => current with
        {
            Model = removeDependents
                ? RemoveTransitionCascade(current.Model, removeStateChanges: [id])
                : current.Model with
                {
                    StateChanges = RemoveWhere(current.Model.StateChanges, candidate => candidate.Id == id),
                },
        });
    }

    /// <summary>
    /// Adds a state-change group.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="group">The group to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddStateChangeGroup(ProjectModel project, StateChangeGroup group)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(group);
        return Apply(project, "Add state-change group", current => current with
        {
            Model = current.Model with { StateChangeGroups = Append(current.Model.StateChangeGroups, group) },
        });
    }

    /// <summary>
    /// Updates a state-change group by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="group">The replacement group.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateStateChangeGroup(ProjectModel project, StateChangeGroup group)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(group);
        if (!Contains(project.Model.StateChangeGroups, candidate => candidate.Id == group.Id))
        {
            return Unknown(
                project,
                "stateChangeGroup",
                group.Id.Value,
                $"State-change group '{group.Id:D}' does not exist.");
        }

        return Apply(project, "Update state-change group", current =>
        {
            var model = current.Model;
            return TryReplace(model.StateChangeGroups, candidate => candidate.Id == group.Id, group, out var groups)
                ? current with { Model = model with { StateChangeGroups = groups } }
                : current;
        });
    }

    /// <summary>
    /// Removes a state-change group, optionally removing its dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The group identifier.</param>
    /// <param name="removeDependents">Whether referencing chains and transitions are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveStateChangeGroup(
        ProjectModel project,
        Ids.StateChangeGroupId id,
        bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.StateChangeGroups, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "stateChangeGroup",
                id.Value,
                $"State-change group '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetStateChangeGroupDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "stateChangeGroup", id.Value, dependents);
            }
        }

        return Apply(project, "Remove state-change group", current => current with
        {
            Model = removeDependents
                ? RemoveTransitionCascade(current.Model, removeGroups: [id])
                : current.Model with
                {
                    StateChangeGroups = RemoveWhere(current.Model.StateChangeGroups, candidate => candidate.Id == id),
                },
        });
    }

    /// <summary>
    /// Creates and adds a group from selected state changes.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="name">The new group name.</param>
    /// <param name="stateChangeIds">The ordered state-change identifiers.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult MakeStateChangeGroup(
        ProjectModel project,
        string name,
        ImmutableArray<Ids.StateChangeId> stateChangeIds)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(name);
        var group = new StateChangeGroup(Ids.StateChangeGroupId.New(), name, stateChangeIds);
        return AddStateChangeGroup(project, group);
    }
}
