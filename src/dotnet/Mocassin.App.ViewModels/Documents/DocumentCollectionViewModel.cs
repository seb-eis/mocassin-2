// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Documents;

/// <summary>
/// Owns the open document tabs and the active document.
/// </summary>
internal sealed partial class DocumentCollectionViewModel : ObservableObject
{
    [ObservableProperty]
    private DocumentViewModel? activeDocument;

    /// <summary>
    /// Occurs when the active document changes.
    /// </summary>
    public event EventHandler<DocumentViewModel?>? ActiveDocumentChanged;

    /// <summary>
    /// Gets the open documents.
    /// </summary>
    public ObservableCollection<DocumentViewModel> Documents { get; } = [];

    /// <summary>
    /// Finds a document by key.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <returns>The document, or <see langword="null"/>.</returns>
    public DocumentViewModel? Find(DocumentKey key) =>
        Documents.FirstOrDefault(document => document.Key == key);

    /// <summary>
    /// Opens a document, or activates the existing document with the same key.
    /// </summary>
    /// <param name="document">The document to open.</param>
    /// <returns>The opened or activated document.</returns>
    public DocumentViewModel OpenOrActivate(DocumentViewModel document)
    {
        var existing = Find(document.Key);
        if (existing is not null)
        {
            ActiveDocument = existing;
            return existing;
        }

        if (!string.Equals(document.Key.Kind, DocumentKinds.Welcome, StringComparison.Ordinal))
        {
            var welcome = Documents.FirstOrDefault(
                candidate => string.Equals(candidate.Key.Kind, DocumentKinds.Welcome, StringComparison.Ordinal));
            if (welcome is not null)
            {
                Documents.Remove(welcome);
            }
        }

        AttachCommands(document);
        Documents.Add(document);
        ActiveDocument = document;
        return document;
    }

    /// <summary>
    /// Closes one document after its close hook has run.
    /// </summary>
    /// <param name="document">The document to close.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the document was closed.</returns>
    public async Task<bool> CloseAsync(
        DocumentViewModel document,
        CancellationToken cancellationToken = default)
    {
        if (!Documents.Contains(document))
        {
            return false;
        }

        await document.OnClosingAsync(cancellationToken);
        var index = Documents.IndexOf(document);
        Documents.Remove(document);
        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = Documents.Count == 0 ? null : Documents[Math.Min(index, Documents.Count - 1)];
        }

        return true;
    }

    /// <summary>
    /// Closes every document except one.
    /// </summary>
    /// <param name="keep">The document to keep.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the other documents are closed.</returns>
    public async Task CloseOthersAsync(
        DocumentViewModel keep,
        CancellationToken cancellationToken = default)
    {
        foreach (var document in Documents.Where(document => !ReferenceEquals(document, keep)).ToArray())
        {
            await CloseAsync(document, cancellationToken);
        }

        ActiveDocument = keep;
    }

    /// <summary>
    /// Closes every document.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every document is closed.</returns>
    public async Task CloseAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var document in Documents.ToArray())
        {
            await CloseAsync(document, cancellationToken);
        }

        ActiveDocument = null;
    }

    /// <summary>
    /// Activates the document at a zero-based index, used by Ctrl+1..9.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns><see langword="true"/> when a document was activated.</returns>
    public bool ActivateIndex(int index)
    {
        if (index < 0 || index >= Documents.Count)
        {
            return false;
        }

        ActiveDocument = Documents[index];
        return true;
    }

    /// <summary>
    /// Moves a document to a new index for drag reordering.
    /// </summary>
    /// <param name="fromIndex">The current index.</param>
    /// <param name="toIndex">The target index.</param>
    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Documents.Count || toIndex < 0 || toIndex >= Documents.Count)
        {
            return;
        }

        Documents.Move(fromIndex, toIndex);
    }

    partial void OnActiveDocumentChanged(DocumentViewModel? value) =>
        ActiveDocumentChanged?.Invoke(this, value);

    private void AttachCommands(DocumentViewModel document)
    {
        document.CloseCommand = new AsyncRelayCommand(() => CloseAsync(document));
        document.CloseOthersCommand = new AsyncRelayCommand(() => CloseOthersAsync(document));
        document.CloseAllCommand = new AsyncRelayCommand(CloseAllAsync);
    }
}
