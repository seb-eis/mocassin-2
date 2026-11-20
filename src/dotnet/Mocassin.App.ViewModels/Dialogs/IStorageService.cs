// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Opens platform file pickers. The implementation lives in the view layer.
/// </summary>
internal interface IStorageService
{
    /// <summary>
    /// Picks an existing project file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected path, or <see langword="null"/> when cancelled.</returns>
    public Task<string?> PickOpenProjectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks a target project file for saving.
    /// </summary>
    /// <param name="suggestedName">The suggested file name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected path, or <see langword="null"/> when cancelled.</returns>
    public Task<string?> PickSaveProjectAsync(string suggestedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks a folder.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected folder path, or <see langword="null"/> when cancelled.</returns>
    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}
