// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors.Structure;

/// <summary>
/// The structure form document: the space-group and cell-parameter forms plus the shared validation summary.
/// It has no table and no row selection.
/// </summary>
internal sealed class StructureEditorViewModel : ModelEditorViewModel
{
    /// <summary>
    /// Initializes the structure form.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    /// <param name="previewService">The structure preview service.</param>
    /// <param name="validator">The structure validator.</param>
    public StructureEditorViewModel(
        ProjectSession session,
        IEditorPaneLayout layout,
        IStructurePreviewService previewService,
        IStructureValidator validator)
        : base(session, ModelEditorDomains.StructureEditor, layout)
    {
        SpaceGroupEditor = new SpaceGroupEditorViewModel(session, previewService, validator);
        CellParameters = new CellParameterEditorViewModel(session, previewService);
    }

    /// <summary>
    /// Gets the space-group form.
    /// </summary>
    public SpaceGroupEditorViewModel SpaceGroupEditor { get; }

    /// <summary>
    /// Gets the cell-parameter form.
    /// </summary>
    public CellParameterEditorViewModel CellParameters { get; }

    /// <inheritdoc />
    protected override ImmutableArray<ModelTableRow> BuildRows(ProjectModel project) => [];

    /// <inheritdoc />
    protected override ModelDetailViewModel? CreateDetail(ProjectModel project, Guid objectId) => null;

    /// <inheritdoc />
    protected override bool IsValueChanged(ProjectModel previous, ProjectModel current, Guid objectId) => false;

    /// <inheritdoc />
    protected override void OnDisposed()
    {
        SpaceGroupEditor.Dispose();
        CellParameters.Dispose();
        base.OnDisposed();
    }
}
