// SPDX-License-Identifier: MIT

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Documents;

/// <summary>
/// Base view model for a document tab.
/// </summary>
internal abstract partial class DocumentViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a document.
    /// </summary>
    /// <param name="key">The stable document key.</param>
    protected DocumentViewModel(DocumentKey key)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the stable document key.
    /// </summary>
    public DocumentKey Key { get; }

    /// <summary>
    /// Gets the tab title.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Gets the optional tooltip, normally the full path.
    /// </summary>
    public virtual string? ToolTip => null;

    /// <summary>
    /// Gets a value indicating whether the document's project has unsaved changes.
    /// </summary>
    public virtual bool IsDirty => false;

    /// <summary>
    /// Gets or sets the command that closes this document.
    /// </summary>
    public ICommand? CloseCommand { get; internal set; }

    /// <summary>
    /// Gets or sets the command that closes every other document.
    /// </summary>
    public ICommand? CloseOthersCommand { get; internal set; }

    /// <summary>
    /// Gets or sets the command that closes every document.
    /// </summary>
    public ICommand? CloseAllCommand { get; internal set; }

    /// <summary>
    /// Runs before the document is removed from the workspace.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the document can close.</returns>
    public virtual Task OnClosingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
