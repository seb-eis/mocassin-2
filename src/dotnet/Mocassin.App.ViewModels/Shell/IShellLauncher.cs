// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Opens a folder in the platform file browser. The implementation lives in the view layer.
/// </summary>
internal interface IShellLauncher
{
    /// <summary>
    /// Opens the supplied directory with the platform file browser.
    /// </summary>
    /// <param name="path">The directory to open.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the request has been handed to the platform.</returns>
    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the supplied URI with the platform default handler.
    /// </summary>
    /// <param name="uri">The absolute URI to open.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the request has been handed to the platform.</returns>
    public Task OpenUriAsync(string uri, CancellationToken cancellationToken = default);
}
