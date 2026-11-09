// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds a transition chain.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="chain">The chain to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddTransitionChain(ProjectModel project, TransitionChain chain)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(chain);
        return Apply(project, "Add transition chain", current => current with
        {
            Model = current.Model with { Chains = Append(current.Model.Chains, chain) },
        });
    }

    /// <summary>
    /// Updates a transition chain by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="chain">The replacement chain.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateTransitionChain(ProjectModel project, TransitionChain chain)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(chain);
        if (!Contains(project.Model.Chains, candidate => candidate.Id == chain.Id))
        {
            return Unknown(project, "chain", chain.Id.Value, $"Transition chain '{chain.Id:D}' does not exist.");
        }

        return Apply(project, "Update transition chain", current =>
        {
            var model = current.Model;
            return TryReplace(model.Chains, candidate => candidate.Id == chain.Id, chain, out var chains)
                ? current with { Model = model with { Chains = chains } }
                : current;
        });
    }

    /// <summary>
    /// Removes a transition chain, optionally removing its dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The chain identifier.</param>
    /// <param name="removeDependents">Whether referencing transitions are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveTransitionChain(ProjectModel project, TransitionChainId id, bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Chains, candidate => candidate.Id == id))
        {
            return Unknown(project, "chain", id.Value, $"Transition chain '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetTransitionChainDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "chain", id.Value, dependents);
            }
        }

        return Apply(project, "Remove transition chain", current => current with
        {
            Model = removeDependents
                ? RemoveTransitionCascade(current.Model, removeChains: [id])
                : current.Model with
                {
                    Chains = RemoveWhere(current.Model.Chains, candidate => candidate.Id == id),
                },
        });
    }

    /// <summary>
    /// Duplicates a transition chain with a new identifier and no copied dependents.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The chain identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult DuplicateTransitionChain(ProjectModel project, TransitionChainId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        var chain = FirstOrDefault(project.Model.Chains, candidate => candidate.Id == id);
        if (chain is null)
        {
            return Unknown(project, "chain", id.Value, $"Transition chain '{id:D}' does not exist.");
        }

        var duplicate = chain with
        {
            Id = TransitionChainId.New(),
            Name = MakeUniqueName(project.Model.Chains.Select(static item => item.Name), chain.Name),
        };
        return AddTransitionChain(project, duplicate);
    }

    private static T? FirstOrDefault<T>(ImmutableArray<T> values, Func<T, bool> predicate)
        where T : class
    {
        if (values.IsDefault)
        {
            return null;
        }

        foreach (var value in values)
        {
            if (predicate(value))
            {
                return value;
            }
        }

        return null;
    }

    private static ImmutableArray<ModelPath> GetStateChangeDependents(ModelDefinition model, StateChangeId id) =>
        [
            .. model.StateChangeGroups
                .Where(group => group.StateChangeIds.Contains(id))
                .Select(static group => new ModelPath("stateChangeGroup", group.Id.Value)),
        ];

    private static ImmutableArray<ModelPath> GetStateChangeGroupDependents(
        ModelDefinition model,
        StateChangeGroupId id) =>
        [
            .. model.Chains
                .Where(chain => chain.GroupIds.Contains(id))
                .Select(static chain => new ModelPath("chain", chain.Id.Value)),
        ];

    private static ImmutableArray<ModelPath> GetTransitionChainDependents(
        ModelDefinition model,
        TransitionChainId id) =>
        [
            .. model.KineticTransitions
                .Where(transition => transition.ChainId == id)
                .Select(static transition => new ModelPath("kineticTransition", transition.Id.Value)),
            .. model.MetropolisTransitions
                .Where(transition => transition.ChainId == id)
                .Select(static transition => new ModelPath("metropolisTransition", transition.Id.Value)),
        ];

    private static ImmutableArray<ModelPath> GetKineticTransitionDependents(
        ModelDefinition model,
        KineticTransitionId id) =>
        [
            .. model.KineticTransitions
                .Where(transition => transition.ReverseTransitionId == id)
                .Select(static transition => new ModelPath("kineticTransition", transition.Id.Value)),
        ];

    private static ImmutableArray<ModelPath> GetMetropolisTransitionDependents(
        ModelDefinition model,
        MetropolisTransitionId id) =>
        [
            .. model.MetropolisTransitions
                .Where(transition => transition.ReverseTransitionId == id)
                .Select(static transition => new ModelPath("metropolisTransition", transition.Id.Value)),
        ];

    private static ProjectEditResult DependentsError(
        ProjectModel project,
        string domain,
        Guid objectId,
        ImmutableArray<ModelPath> dependents) =>
        new(
            project,
            null,
            [
                new Diagnostic(
                    ProjectDiagnosticCodes.InvalidDocument,
                    DiagnosticSeverity.Error,
                    "The object is referenced by other model objects. Remove the dependents explicitly.",
                    new ModelPath(domain, objectId),
                    related: dependents),
            ]);

    private static ModelDefinition RemoveTransitionCascade(
        ModelDefinition model,
        ImmutableArray<StateChangeId> removeStateChanges = default,
        ImmutableArray<StateChangeGroupId> removeGroups = default,
        ImmutableArray<TransitionChainId> removeChains = default,
        ImmutableArray<KineticTransitionId> removeKineticTransitions = default,
        ImmutableArray<MetropolisTransitionId> removeMetropolisTransitions = default)
    {
        var stateChangeIds = removeStateChanges.IsDefault ? [] : removeStateChanges.ToHashSet();
        var groupIds = removeGroups.IsDefault ? [] : removeGroups.ToHashSet();
        var chainIds = removeChains.IsDefault ? [] : removeChains.ToHashSet();
        var kineticIds = removeKineticTransitions.IsDefault ? [] : removeKineticTransitions.ToHashSet();
        var metropolisIds = removeMetropolisTransitions.IsDefault ? [] : removeMetropolisTransitions.ToHashSet();

        bool changed;
        do
        {
            changed = false;

            foreach (var group in model.StateChangeGroups)
            {
                if (!groupIds.Contains(group.Id) && group.StateChangeIds.Any(stateChangeIds.Contains))
                {
                    groupIds.Add(group.Id);
                    changed = true;
                }
            }

            foreach (var chain in model.Chains)
            {
                if (!chainIds.Contains(chain.Id) && chain.GroupIds.Any(groupIds.Contains))
                {
                    chainIds.Add(chain.Id);
                    changed = true;
                }
            }

            foreach (var transition in model.KineticTransitions)
            {
                if (!kineticIds.Contains(transition.Id) &&
                    (chainIds.Contains(transition.ChainId) ||
                     (transition.ReverseTransitionId is { } reverse && kineticIds.Contains(reverse))))
                {
                    kineticIds.Add(transition.Id);
                    changed = true;
                }
            }

            foreach (var transition in model.MetropolisTransitions)
            {
                if (!metropolisIds.Contains(transition.Id) &&
                    (chainIds.Contains(transition.ChainId) ||
                     (transition.ReverseTransitionId is { } reverse && metropolisIds.Contains(reverse))))
                {
                    metropolisIds.Add(transition.Id);
                    changed = true;
                }
            }
        }
        while (changed);

        return model with
        {
            StateChanges = model.StateChanges.IsDefault
                ? []
                : [.. model.StateChanges.Where(stateChange => !stateChangeIds.Contains(stateChange.Id))],
            StateChangeGroups = model.StateChangeGroups.IsDefault
                ? []
                : [.. model.StateChangeGroups.Where(group => !groupIds.Contains(group.Id))],
            Chains = model.Chains.IsDefault
                ? []
                : [.. model.Chains.Where(chain => !chainIds.Contains(chain.Id))],
            KineticTransitions = model.KineticTransitions.IsDefault
                ? []
                : [.. model.KineticTransitions.Where(transition => !kineticIds.Contains(transition.Id))],
            MetropolisTransitions = model.MetropolisTransitions.IsDefault
                ? []
                : [.. model.MetropolisTransitions.Where(transition => !metropolisIds.Contains(transition.Id))],
        };
    }

    private static string MakeUniqueName(IEnumerable<string> existingNames, string baseName)
    {
        var names = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidate = baseName + " copy";
        var index = 2;
        while (!names.Add(candidate))
        {
            candidate = baseName + " copy " + index;
            index++;
        }

        return candidate;
    }
}
