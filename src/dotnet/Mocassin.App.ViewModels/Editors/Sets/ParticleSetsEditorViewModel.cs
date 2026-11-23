// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Sets;

/// <summary>
/// The particle-sets master-detail editor. Each set's detail carries its own "Project particles" drag source, so
/// membership works inside one document and has a keyboard equivalent.
/// </summary>
internal sealed class ParticleSetsEditorViewModel : ModelEditorViewModel<ParticleSet>
{
    /// <summary>
    /// The set table's column policy.
    /// </summary>
    public static readonly IReadOnlyList<ModelColumn> Columns =
    [
        new("Name", ModelColumnWidth.Star(2d)),
        new("Members", ModelColumnWidth.Fixed(90d), SortKind: ModelSortKind.Number),
        new("Usage", ModelColumnWidth.Fixed(80d), SortKind: ModelSortKind.Number),
        new("Status", ModelColumnWidth.Fixed(90d), IsSortable: false),
    ];

    /// <summary>
    /// Initializes the particle-sets editor.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    public ParticleSetsEditorViewModel(ProjectSession session, IEditorPaneLayout layout)
        : base(session, ModelEditorDomains.ParticleSets, layout)
    {
        Table.SetColumns(Columns);
        Table.IsMultiSelectEnabled = true;
        RebuildRows();
    }

    /// <inheritdoc />
    protected override ImmutableArray<ParticleSet> GetModels(ProjectModel project) => project.Model.ParticleSets;

    /// <inheritdoc />
    protected override Guid GetId(ParticleSet model) => model.Id.Value;

    /// <inheritdoc />
    protected override ModelTableRow ToRow(ParticleSet model)
    {
        var usage = Usages.Count(ModelObjectRef.ParticleSet(model.Id));
        var severity = SeverityFor(model.Id.Value);
        return new ModelTableRow(
            model.Id.Value,
            [
                ModelTableCell.OfText(model.Name),
                ModelTableCell.OfNumber(model.ParticleIds.Length),
                ModelTableCell.OfNumber(usage),
                ModelTableCell.OfText(severity == ModelRowSeverity.None ? string.Empty : severity.ToString()),
            ],
            severity,
            severity == ModelRowSeverity.None ? string.Empty : severity.ToString());
    }

    /// <inheritdoc />
    protected override ModelDetailViewModel? CreateDetailFor(ParticleSet model) =>
        new ParticleSetDetailViewModel(model, Project);

    /// <inheritdoc />
    protected override void AddCore()
    {
        if (Project.Particles.Length == 0)
        {
            return;
        }

        AddSet(UniqueName("Particle set"), [Project.Particles[0].Id]);
    }

    /// <inheritdoc />
    protected override void DuplicateCore(Guid objectId)
    {
        var source = Project.Find(ParticleSetId.Create(objectId));
        if (source is null)
        {
            return;
        }

        AddSet(UniqueName(source.Name + " copy"), source.ParticleIds);
    }

    /// <inheritdoc />
    protected override bool RemoveManyCore(IReadOnlyList<Guid> objectIds)
    {
        var steps = objectIds
            .Select(id => (Func<ProjectEditor, ProjectModel, ProjectEditResult>)((editor, project) =>
                editor.RemoveParticleSet(project, ParticleSetId.Create(id))))
            .ToArray();
        if (steps.Length == 0)
        {
            return false;
        }

        var result = Coordinator.Apply("Remove particle set(s)", BulkEditComposer.Compose(steps));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    /// <summary>
    /// Adds one set through the coordinator.
    /// </summary>
    /// <param name="name">The unique set name.</param>
    /// <param name="members">The ordered member ids; must not be empty.</param>
    /// <returns>The new set id, or <see langword="null"/> when the edit was refused.</returns>
    public ParticleSetId? AddSet(string name, ImmutableArray<ParticleId> members)
    {
        var id = ParticleSetId.New();
        var result = Coordinator.Apply(
            "Add particle set",
            (editor, project) => editor.AddParticleSet(project, new ParticleSet(id, name, members)));
        if (result.Outcome != EditApplyOutcome.Applied || !result.HasChange)
        {
            return null;
        }

        RequestSelection(id.Value);
        return id;
    }

    private ModelRowSeverity SeverityFor(Guid objectId)
    {
        var diagnostics = Session.ValidationCoordinator.Diagnostics
            .Concat(DiagnosticSummary.Diagnostics)
            .Where(diagnostic => diagnostic.Path is { } path
                && path.ObjectId == objectId
                && string.Equals(path.Domain, ModelEditorDomains.ParticleSet, StringComparison.Ordinal))
            .ToArray();
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return ModelRowSeverity.Error;
        }

        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning))
        {
            return ModelRowSeverity.Warning;
        }

        return diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Info)
            ? ModelRowSeverity.Info
            : ModelRowSeverity.None;
    }

    private string UniqueName(string baseName)
    {
        var names = Project.Model.ParticleSets
            .Select(static set => set.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(baseName))
        {
            return baseName;
        }

        for (var suffix = 2; ; suffix++)
        {
            var candidate = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{baseName} {suffix}");
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
