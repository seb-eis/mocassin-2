// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

public sealed partial class ProjectEditor
{
    /// <summary>
    /// Adds an occupation exchange.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="exchange">The exchange to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddOccupationExchange(ProjectModel project, OccupationExchange exchange)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(exchange);
        return Apply(project, "Add occupation exchange", current => current with
        {
            Model = current.Model with
            {
                OccupationExchanges = Append(current.Model.OccupationExchanges, exchange),
            },
        });
    }

    /// <summary>
    /// Updates an occupation exchange by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="exchange">The replacement exchange.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateOccupationExchange(ProjectModel project, OccupationExchange exchange)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(exchange);
        if (!Contains(project.Model.OccupationExchanges, candidate => candidate.Id == exchange.Id))
        {
            return Unknown(
                project,
                "occupationExchange",
                exchange.Id.Value,
                $"Occupation exchange '{exchange.Id:D}' does not exist.");
        }

        return Apply(project, "Update occupation exchange", current =>
        {
            var model = current.Model;
            return TryReplace(
                model.OccupationExchanges,
                candidate => candidate.Id == exchange.Id,
                exchange,
                out var exchanges)
                ? current with { Model = model with { OccupationExchanges = exchanges } }
                : current;
        });
    }

    /// <summary>
    /// Removes an occupation exchange, optionally removing referencing dopings.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The occupation-exchange identifier.</param>
    /// <param name="removeDependents">Whether dopings that reference the exchange are removed too.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveOccupationExchange(
        ProjectModel project,
        OccupationExchangeId id,
        bool removeDependents = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.OccupationExchanges, candidate => candidate.Id == id))
        {
            return Unknown(
                project,
                "occupationExchange",
                id.Value,
                $"Occupation exchange '{id:D}' does not exist.");
        }

        if (!removeDependents)
        {
            var dependents = GetOccupationExchangeDependents(project.Model, id);
            if (dependents.Length > 0)
            {
                return DependentsError(project, "occupationExchange", id.Value, dependents);
            }
        }

        return Apply(project, "Remove occupation exchange", current => current with
        {
            Model = current.Model with
            {
                OccupationExchanges = RemoveWhere(
                    current.Model.OccupationExchanges,
                    candidate => candidate.Id == id),
                Dopings = removeDependents
                    ? RemoveWhere(
                        current.Model.Dopings,
                        doping => doping.PrimaryExchangeId == id || doping.DependentExchangeId == id)
                    : current.Model.Dopings,
            },
        });
    }

    /// <summary>
    /// Adds a doping.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="doping">The doping to add.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult AddDoping(ProjectModel project, Doping doping)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(doping);
        return Apply(project, "Add doping", current => current with
        {
            Model = current.Model with { Dopings = Append(current.Model.Dopings, doping) },
        });
    }

    /// <summary>
    /// Updates a doping by identifier.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="doping">The replacement doping.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult UpdateDoping(ProjectModel project, Doping doping)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(doping);
        if (!Contains(project.Model.Dopings, candidate => candidate.Id == doping.Id))
        {
            return Unknown(project, "doping", doping.Id.Value, $"Doping '{doping.Id:D}' does not exist.");
        }

        return Apply(project, "Update doping", current =>
        {
            var model = current.Model;
            return TryReplace(model.Dopings, candidate => candidate.Id == doping.Id, doping, out var dopings)
                ? current with { Model = model with { Dopings = dopings } }
                : current;
        });
    }

    /// <summary>
    /// Removes a doping.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="id">The doping identifier.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult RemoveDoping(ProjectModel project, DopingId id)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Contains(project.Model.Dopings, candidate => candidate.Id == id))
        {
            return Unknown(project, "doping", id.Value, $"Doping '{id:D}' does not exist.");
        }

        return Apply(project, "Remove doping", current => current with
        {
            Model = current.Model with
            {
                Dopings = RemoveWhere(current.Model.Dopings, candidate => candidate.Id == id),
            },
        });
    }

    /// <summary>
    /// Reorders all dopings and renumbers their priorities deterministically from zero.
    /// </summary>
    /// <param name="project">The current project.</param>
    /// <param name="orderedIds">Every doping identifier exactly once, in the desired priority order.</param>
    /// <returns>The edit result.</returns>
    public ProjectEditResult ReorderDopings(ProjectModel project, ImmutableArray<DopingId> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(project);
        var dopings = project.Model.Dopings.IsDefault ? ImmutableArray<Doping>.Empty : project.Model.Dopings;
        if (orderedIds.IsDefault ||
            orderedIds.Length != dopings.Length ||
            orderedIds.Distinct().Count() != orderedIds.Length)
        {
            return InvalidEdit(
                project,
                "doping",
                Guid.Empty,
                "Reordering requires every doping exactly once.",
                "priority");
        }

        var knownIds = dopings.Select(static doping => doping.Id).ToHashSet();
        if (orderedIds.Any(id => !knownIds.Contains(id)))
        {
            return InvalidEdit(
                project,
                "doping",
                Guid.Empty,
                "Reordering references an unknown doping.",
                "priority");
        }

        return Apply(project, "Reorder dopings", current =>
        {
            var byId = new Dictionary<DopingId, Doping>();
            foreach (var doping in current.Model.Dopings.IsDefault
                         ? ImmutableArray<Doping>.Empty
                         : current.Model.Dopings)
            {
                byId.TryAdd(doping.Id, doping);
            }

            var reordered = orderedIds
                .Select((id, index) => byId[id] with { Priority = index })
                .ToImmutableArray();
            return current with { Model = current.Model with { Dopings = reordered } };
        });
    }

    private static ImmutableArray<ModelPath> GetOccupationExchangeDependents(
        ModelDefinition model,
        OccupationExchangeId id) =>
        [
            .. (model.Dopings.IsDefault ? ImmutableArray<Doping>.Empty : model.Dopings)
                .Where(doping => doping.PrimaryExchangeId == id || doping.DependentExchangeId == id)
                .Select(static doping => new ModelPath("doping", doping.Id.Value)),
        ];

    private static ProjectEditResult InvalidEdit(
        ProjectModel project,
        string domain,
        Guid objectId,
        string message,
        string? property = null) =>
        new(
            project,
            null,
            [
                new Diagnostic(
                    ProjectDiagnosticCodes.InvalidDocument,
                    DiagnosticSeverity.Error,
                    message,
                    new ModelPath(domain, objectId, property)),
            ]);
}
