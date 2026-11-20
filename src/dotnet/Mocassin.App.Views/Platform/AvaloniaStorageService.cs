// SPDX-License-Identifier: MIT

using Avalonia.Platform.Storage;
using Mocassin.App.ViewModels.Dialogs;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Avalonia file-picker implementation. The provider is attached by the shell window.
/// </summary>
internal sealed class AvaloniaStorageService : IStorageService
{
    private static readonly FilePickerFileType projectFileType = new("MOCASSIN project")
    {
        Patterns = ["*.mocprj"],
        MimeTypes = ["application/json"],
    };

    /// <summary>
    /// Gets or sets the platform storage provider.
    /// </summary>
    public IStorageProvider? StorageProvider { get; set; }

    /// <inheritdoc />
    public async Task<string?> PickOpenProjectAsync(CancellationToken cancellationToken = default)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open MOCASSIN project",
                AllowMultiple = false,
                FileTypeFilter = [projectFileType],
            });

        cancellationToken.ThrowIfCancellationRequested();
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveProjectAsync(string suggestedName, CancellationToken cancellationToken = default)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save MOCASSIN project",
                SuggestedFileName = suggestedName,
                DefaultExtension = "mocprj",
                FileTypeChoices = [projectFileType],
            });

        cancellationToken.ThrowIfCancellationRequested();
        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select folder",
                AllowMultiple = false,
            });

        cancellationToken.ThrowIfCancellationRequested();
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}
