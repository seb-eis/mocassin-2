// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Opens, activates, and navigates documents, with bounded back/forward history.
/// </summary>
internal interface INavigationService
{
    /// <summary>
    /// Gets a value indicating whether back navigation is possible.
    /// </summary>
    public bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether forward navigation is possible.
    /// </summary>
    public bool CanGoForward { get; }

    /// <summary>
    /// Occurs when the history state changes.
    /// </summary>
    public event EventHandler? HistoryChanged;

    /// <summary>
    /// Opens or activates a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the document is active.</returns>
    public Task OpenDocumentAsync(DocumentViewModel document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a document by key.
    /// </summary>
    /// <param name="key">The document key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the document was found and activated.</returns>
    public Task<bool> ActivateAsync(DocumentKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates to a model path.
    /// </summary>
    /// <param name="path">The model path.</param>
    /// <param name="intent">The navigation intent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when a target was opened.</returns>
    public Task<bool> NavigateToAsync(
        ModelPath path,
        NavigationIntent intent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates back.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when navigation finishes.</returns>
    public Task GoBackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Navigates forward.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when navigation finishes.</returns>
    public Task GoForwardAsync(CancellationToken cancellationToken = default);
}
