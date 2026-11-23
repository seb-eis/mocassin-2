// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.ViewModels.Editors.ModelTable;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// A typed model editor. The domain supplies the immutable model sequence, the id selector, the row projection, and
/// the detail factory; the base owns the framework plumbing.
/// </summary>
/// <typeparam name="TModel">The immutable domain model type.</typeparam>
internal abstract class ModelEditorViewModel<TModel> : ModelEditorViewModel
    where TModel : class
{
    /// <summary>
    /// Initializes a typed editor.
    /// </summary>
    /// <param name="session">The project session.</param>
    /// <param name="descriptor">The domain descriptor.</param>
    /// <param name="layout">The shell-owned editor pane layout.</param>
    /// <param name="openProblems">The optional Problems-channel activation.</param>
    protected ModelEditorViewModel(
        ProjectSession session,
        ModelEditorDescriptor descriptor,
        IEditorPaneLayout layout,
        Action? openProblems = null)
        : base(session, descriptor, layout, openProblems)
    {
    }

    /// <summary>
    /// Gets the domain models from a snapshot.
    /// </summary>
    /// <param name="project">The snapshot.</param>
    /// <returns>The models in domain order.</returns>
    protected abstract ImmutableArray<TModel> GetModels(ProjectModel project);

    /// <summary>
    /// Gets the stable id of a model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The id.</returns>
    protected abstract Guid GetId(TModel model);

    /// <summary>
    /// Projects one model into a table row.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The row.</returns>
    protected abstract ModelTableRow ToRow(TModel model);

    /// <summary>
    /// Creates the detail editor for one model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The detail.</returns>
    protected abstract ModelDetailViewModel? CreateDetailFor(TModel model);

    /// <inheritdoc />
    protected sealed override ImmutableArray<ModelTableRow> BuildRows(ProjectModel project) =>
        GetModels(project).Select(ToRow).ToImmutableArray();

    /// <inheritdoc />
    protected sealed override ModelDetailViewModel? CreateDetail(ProjectModel project, Guid objectId)
    {
        var model = GetModels(project).FirstOrDefault(candidate => GetId(candidate) == objectId);
        return model is null ? null : CreateDetailFor(model);
    }

    /// <inheritdoc />
    protected sealed override bool IsValueChanged(ProjectModel previous, ProjectModel current, Guid objectId)
    {
        var before = GetModels(previous).FirstOrDefault(candidate => GetId(candidate) == objectId);
        var after = GetModels(current).FirstOrDefault(candidate => GetId(candidate) == objectId);
        return before is not null
            && after is not null
            && !EqualityComparer<TModel>.Default.Equals(before, after);
    }
}
