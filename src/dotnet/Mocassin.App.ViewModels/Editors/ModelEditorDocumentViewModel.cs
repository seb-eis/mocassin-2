// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// A master-detail model-editor document: the table selects and the detail pane edits. The document owns the tab
/// identity, the dirty marker, the inspector content, and the navigation intent.
/// </summary>
internal class ModelEditorDocumentViewModel : DocumentViewModel, IInspectorContributor, IDisposable
{
    private bool disposed;

    /// <summary>
    /// Initializes a model-editor document around one editor.
    /// </summary>
    /// <param name="descriptor">The domain descriptor.</param>
    /// <param name="editor">The editor view model.</param>
    public ModelEditorDocumentViewModel(ModelEditorDescriptor descriptor, ModelEditorViewModel editor)
        : base(descriptor.KeyFor(editor.Session.Id))
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        Editor.NavigationRequested += OnEditorNavigationRequested;
    }

    /// <summary>
    /// Raised when the editor's dependents/usage action asks the shell to navigate.
    /// </summary>
    public event EventHandler<ModelPath>? NavigationRequested;

    /// <summary>
    /// Gets the domain descriptor.
    /// </summary>
    public ModelEditorDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the editor view model.
    /// </summary>
    public ModelEditorViewModel Editor { get; }

    /// <inheritdoc />
    public override string Title => Descriptor.Title;

    /// <inheritdoc />
    public override string? ToolTip => Editor.Session.Path;

    /// <inheritdoc />
    public override bool IsDirty => Editor.Coordinator.IsDirty;

    /// <summary>
    /// Applies a navigation intent to the editor.
    /// </summary>
    /// <param name="path">The model path.</param>
    /// <param name="intent">The navigation intent.</param>
    /// <returns><see langword="false"/> when the target object does not exist.</returns>
    public bool TryNavigate(ModelPath path, NavigationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(path);
        switch (intent)
        {
            case NavigationIntent.Activate:
                return true;
            case NavigationIntent.SelectObject:
                return path.ObjectId == Guid.Empty || Editor.RequestSelection(path.ObjectId);
            case NavigationIntent.FocusProperty:
                if (path.ObjectId != Guid.Empty && !Editor.RequestSelection(path.ObjectId))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(path.Property))
                {
                    Editor.RequestFocus(path.Property);
                }

                return true;
            default:
                return true;
        }
    }

    /// <inheritdoc />
    public object? CreateInspector(InspectorSelection selection) => Editor.CreateInspector();

    private void OnEditorNavigationRequested(object? sender, ModelPath path) =>
        NavigationRequested?.Invoke(this, path);

    /// <inheritdoc />
    public override Task OnClosingAsync(CancellationToken cancellationToken = default)
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Editor.NavigationRequested -= OnEditorNavigationRequested;
        Editor.Dispose();
        GC.SuppressFinalize(this);
    }
}
