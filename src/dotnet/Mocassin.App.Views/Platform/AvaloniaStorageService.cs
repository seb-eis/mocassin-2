// SPDX-License-Identifier: MIT

using Avalonia.Platform.Storage;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Settings;

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

    private readonly ISettingsService settings;

    /// <summary>
    /// Initializes the picker service.
    /// </summary>
    /// <param name="settings">The settings service used for the last directories.</param>
    public AvaloniaStorageService(ISettingsService settings)
    {
        this.settings = settings;
    }

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
                SuggestedStartLocation = await GetStartFolderAsync(
                    settings.Current.Paths.LastOpenDirectory,
                    cancellationToken),
            });

        cancellationToken.ThrowIfCancellationRequested();
        var path = files.Count == 0 ? null : files[0].TryGetLocalPath();
        if (path is not null)
        {
            await UpdateLastDirectoryAsync(path, PathKind.Open);
        }

        return path;
    }

    /// <inheritdoc />
    public async Task<string?> PickSaveProjectAsync(
        string suggestedName,
        CancellationToken cancellationToken = default)
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
                SuggestedStartLocation = await GetStartFolderAsync(
                    settings.Current.Paths.LastSaveDirectory,
                    cancellationToken),
            });

        cancellationToken.ThrowIfCancellationRequested();
        var path = file?.TryGetLocalPath();
        if (path is not null)
        {
            await UpdateLastDirectoryAsync(path, PathKind.Save);
        }

        return path;
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
                SuggestedStartLocation = await GetStartFolderAsync(
                    settings.Current.Paths.LastFolderDirectory,
                    cancellationToken),
            });

        cancellationToken.ThrowIfCancellationRequested();
        var path = folders.Count == 0 ? null : folders[0].TryGetLocalPath();
        if (path is not null)
        {
            await UpdateLastDirectoryAsync(path, PathKind.Folder);
        }

        return path;
    }

    private async Task<IStorageFolder?> GetStartFolderAsync(
        string? directory,
        CancellationToken cancellationToken)
    {
        if (StorageProvider is null || string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await StorageProvider.TryGetFolderFromPathAsync(new Uri(directory));
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    private async Task UpdateLastDirectoryAsync(string path, PathKind kind)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var paths = settings.Current.Paths;
        var updated = kind switch
        {
            PathKind.Open => paths with { LastOpenDirectory = directory },
            PathKind.Save => paths with { LastSaveDirectory = directory },
            _ => paths with { LastFolderDirectory = directory },
        };
        await settings.UpdateAsync(settings.Current with { Paths = updated });
    }

    private enum PathKind
    {
        Open,
        Save,
        Folder,
    }
}
