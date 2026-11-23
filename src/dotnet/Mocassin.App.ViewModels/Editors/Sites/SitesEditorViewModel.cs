// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Sites;

/// <summary>
/// The cell-site master-detail editor. The table carries the orbit/Cartesian projection; the detail edits the site,
/// validates the candidate with the Symmetry preview/validator, and owns the P1 preview and dependency actions.
/// </summary>
internal sealed class SitesEditorViewModel : ModelEditorViewModel<CellSite>
{
    /// <summary>
    /// The site table's column policy.
    /// </summary>
    public static readonly IReadOnlyList<ModelColumn> Columns =
    [
        new("Name", ModelColumnWidth.Star(2d)),
        new("A", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("B", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("C", ModelColumnWidth.Fixed(70d), SortKind: ModelSortKind.Number),
        new("Stability", ModelColumnWidth.Fixed(84d)),
        new("Particle set", ModelColumnWidth.Star(1d)),
        new("Orbit", ModelColumnWidth.Fixed(64d), SortKind: ModelSortKind.Number),
        new("Multiplicity", ModelColumnWidth.Fixed(88d), SortKind: ModelSortKind.Number, IsSortable: false),
        new("Cartesian", ModelColumnWidth.Star(2d), IsSortable: false),
    ];

    private readonly IStructurePreviewService? previewService;
    private readonly IStructureValidator validator;
    private StructurePreview? preview;
    private ProjectModel? previewSnapshot;
    private Dictionary<Guid, int> siteIndices = [];

    /// <summary>
    /// Initializes the sites editor.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public SitesEditorViewModel(
        ProjectSession session,
        IEditorPaneLayout layout,
        IStructurePreviewService previewService,
        IStructureValidator validator)
        : base(session, ModelEditorDomains.Sites, layout)
    {
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        Table.SetColumns(Columns);
        RebuildRows();
    }

    /// <inheritdoc />
    protected override ImmutableArray<CellSite> GetModels(ProjectModel project) =>
        project.Structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : project.Structure.Sites;

    /// <inheritdoc />
    protected override Guid GetId(CellSite model) => model.Id.Value;

    /// <inheritdoc />
    protected override ModelTableRow ToRow(CellSite model)
    {
        var structure = previewService is null ? null : EnsurePreview();
        var index = SiteIndex(model.Id.Value);
        var orbit = structure is not null && index >= 0 && index < structure.Orbits.Length
            ? structure.Orbits[index]
            : null;
        var cartesian = orbit is not null && orbit.PositionIndices.Length > 0
            ? structure!.CartesianPositions[orbit.PositionIndices[0]]
            : (Cartesian3?)null;
        var severity = SeverityFor(model.Id.Value);
        var set = Project.Find(model.ParticleSetId);
        return new ModelTableRow(
            model.Id.Value,
            [
                ModelTableCell.OfText(model.Name),
                ModelTableCell.OfNumber(model.Fractional.A),
                ModelTableCell.OfNumber(model.Fractional.B),
                ModelTableCell.OfNumber(model.Fractional.C),
                ModelTableCell.OfText(model.Stability.ToString()),
                ModelTableCell.OfText(set?.Name ?? string.Empty),
                ModelTableCell.OfNumber(orbit?.Positions.Length ?? 0),
                ModelTableCell.OfNumber(orbit?.Multiplicity ?? 0),
                ModelTableCell.OfText(cartesian is null ? "—" : FormatCartesian(cartesian.Value)),
            ],
            severity,
            severity == ModelRowSeverity.None ? string.Empty : severity.ToString());
    }

    /// <inheritdoc />
    protected override ModelDetailViewModel? CreateDetailFor(CellSite model) =>
        new CellSiteDetailViewModel(model, false, Project, previewService!, validator);

    /// <inheritdoc />
    protected override void AddCore()
    {
        var set = Project.Model.ParticleSets.FirstOrDefault();
        var site = new CellSite(
            CellSiteId.New(),
            NextName(),
            new Fractional3(0d, 0d, 0d),
            set?.Id ?? ParticleSetId.Empty,
            SiteStability.Stable);
        var detail = new CellSiteDetailViewModel(site, true, Project, previewService!, validator);
        detail.Added += (_, id) => RequestSelection(id);
        ShowDetail(detail);
    }

    /// <inheritdoc />
    protected override bool RemoveCore(Guid objectId)
    {
        var result = Coordinator.Apply(
            "Remove cell site",
            (editor, project) => editor.RemoveCellSite(project, CellSiteId.Create(objectId)));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    /// <inheritdoc />
    protected override bool RemoveManyCore(IReadOnlyList<Guid> objectIds)
    {
        var steps = objectIds
            .Select(id => (Func<ProjectEditor, ProjectModel, ProjectEditResult>)((editor, project) =>
                editor.RemoveCellSite(project, CellSiteId.Create(id))))
            .ToArray();
        if (steps.Length == 0)
        {
            return false;
        }

        var result = Coordinator.Apply("Remove cell site(s)", BulkEditComposer.Compose(steps));
        return result.Outcome == EditApplyOutcome.Applied && result.HasChange;
    }

    private StructurePreview? EnsurePreview()
    {
        if (ReferenceEquals(previewSnapshot, Project))
        {
            return preview;
        }

        previewSnapshot = Project;
        preview = previewService!.TryCreatePreview(Project.Structure, 1, 1, 1, out _);
        siteIndices = [];
        var sites = Project.Structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : Project.Structure.Sites;
        for (var index = 0; index < sites.Length; index++)
        {
            siteIndices[sites[index].Id.Value] = index;
        }

        return preview;
    }

    private int SiteIndex(Guid objectId) =>
        siteIndices.TryGetValue(objectId, out var index) ? index : -1;

    private ModelRowSeverity SeverityFor(Guid objectId)
    {
        var diagnostics = Session.ValidationCoordinator.Diagnostics
            .Concat(DiagnosticSummary.Diagnostics)
            .Where(diagnostic => diagnostic.Path is { } path
                && path.ObjectId == objectId
                && string.Equals(path.Domain, ModelEditorDomains.CellSite, StringComparison.Ordinal))
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
        var names = Project.Structure.Sites
            .Select(static site => site.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; ; index++)
        {
            var candidate = string.Create(CultureInfo.InvariantCulture, $"Site {index}");
            if (!names.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private static string FormatCartesian(Cartesian3 value) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"({value.X:0.###}, {value.Y:0.###}, {value.Z:0.###}) Å");
}
