// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Creates documents for one or more model-path domains. Feature steps register providers explicitly.
/// </summary>
internal interface INavigationTargetProvider
{
    /// <summary>
    /// Gets the project scope, or <see langword="null"/> for a provider that follows the active project.
    /// </summary>
    public Guid? ProjectScopeId { get; }

    /// <summary>
    /// Gets the navigation domains this provider handles.
    /// </summary>
    public IReadOnlyCollection<string> Domains { get; }

    /// <summary>
    /// Creates a document for a model path.
    /// </summary>
    /// <param name="path">The model path.</param>
    /// <param name="intent">The navigation intent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document, or <see langword="null"/> when the target does not exist.</returns>
    public Task<DocumentViewModel?> CreateAsync(
        ModelPath path,
        NavigationIntent intent,
        CancellationToken cancellationToken);
}
