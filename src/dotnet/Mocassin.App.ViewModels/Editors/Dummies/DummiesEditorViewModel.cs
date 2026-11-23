// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Dummies;

/// <summary>
/// The dummy-positions master-detail editor. Dummies are visual-only markers: they use the distinct diamond glyph,
/// carry no particle set, stability, energy, or transition semantics, and round-trip through the project file.
/// </summary>
internal sealed class DummiesEditorViewModel : ModelEditorViewModel<CellDummyPosition>
{
    /// <summary>
    /// The distinct visual-only marker glyph.
    /// </summary>
    public const string MarkerGlyph = "◇";

    /// <summary>
    /// The distinct visual-only status text.
    /// </summary>
    public const string MarkerStatus = "visual-only";

    /// <summary>
    /// The dummy table's column policy.
    /// </summary>
    public static readonly IReadOnlyList<ModelColumn> Columns =
    [
        new("Marker", ModelColumnWidth.Fixed(52d), IsSortable: false),
        new("Name", ModelColumnWidth.Star(2d)),
        new("A", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("B", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("C", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("Status", ModelColumnWidth.Fixed(100d), IsSortable: false),
    ];

    /// <summary>
    /// Initializes the dummy-positions editor.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    public DummiesEditorViewModel(ProjectSession session, IEditorPaneLayout layout)
        : base(session, ModelEditorDomains.DummyPositions, layout)
    {
        Table.SetColumns(Columns);
        RebuildRows();
    }

    /// <inheritdoc />
    protected override ImmutableArray<CellDummyPosition> GetModels(ProjectModel project) =>
        project.Structure.DummyPositions.IsDefault
            ? ImmutableArray<CellDummyPosition>.Empty
            : project.Structure.DummyPositions;

    /// <inheritdoc />
    protected override Guid GetId(CellDummyPosition model) => model.Id.Value;

    /// <inheritdoc />
    protected override ModelTableRow ToRow(CellDummyPosition model)
    {
        var severity = SeverityFor(model.Id.Value);
        return new ModelTableRow(
            model.Id.Value,
            [
                ModelTableCell.OfText(MarkerGlyph),
                ModelTableCell.OfText(model.Name),
                ModelTableCell.OfNumber(model.Fractional.A),
                ModelTableCell.OfNumber(model.Fractional.B),
                ModelTableCell.OfNumber(model.Fractional.C),
                ModelTableCell.OfText(MarkerStatus),
            ],
            severity,
            severity == ModelRowSeverity.None ? string.Empty : severity.ToString());
    }

    /// <inheritdoc />
    protected override ModelDetailViewModel? CreateDetailFor(CellDummyPosition model) =>
        new DummyPositionDetailViewModel(model);

    /// <inheritdoc />
    protected override void AddCore()
    {
        var dummy = new CellDummyPosition(
            CellDummyPositionId.New(),
            NextName(),
            new Fractional3(0d, 0d, 0d));
        var result = Coordinator.Apply(
            "Add dummy position",
            (editor, project) => editor.AddDummyPosition(project, dummy));
        if (result.Outcome == EditApplyOutcome.Applied && result.HasChange)
        {
            RequestSelection(dummy.Id.Value);
        }
    }

    /// <inheritdoc />
    protected override bool RemoveCore(Guid objectId)
    {
        var result = Coordinator.Apply(
            "Remove dummy position",
            (editor, project) => editor.RemoveDummyPosition(project, CellDummyPositionId.Create(objectId)));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    /// <inheritdoc />
    protected override bool RemoveManyCore(IReadOnlyList<Guid> objectIds)
    {
        if (objectIds.Count == 1)
        {
            return RemoveCore(objectIds[0]);
        }

        var steps = objectIds
            .Select(id => (Func<ProjectEditor, ProjectModel, ProjectEditResult>)((editor, project) =>
                editor.RemoveDummyPosition(project, CellDummyPositionId.Create(id))))
            .ToArray();
        var result = Coordinator.Apply("Remove dummy position(s)", BulkEditComposer.Compose(steps));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    private ModelRowSeverity SeverityFor(Guid objectId)
    {
        var diagnostics = Session.ValidationCoordinator.Diagnostics
            .Concat(DiagnosticSummary.Diagnostics)
            .Where(diagnostic => diagnostic.Path is { } path
                && path.ObjectId == objectId
                && string.Equals(path.Domain, ModelEditorDomains.CellDummyPosition, StringComparison.Ordinal))
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

    private string NextName()
    {
        var names = Project.Structure.DummyPositions.IsDefault
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : Project.Structure.DummyPositions
                .Select(static dummy => dummy.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; ; index++)
        {
            var candidate = string.Create(CultureInfo.InvariantCulture, $"Dummy {index}");
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
