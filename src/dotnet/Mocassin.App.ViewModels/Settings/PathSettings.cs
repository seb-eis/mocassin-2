// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Last-used directory paths.
/// </summary>
internal sealed record PathSettings
{
    /// <summary>
    /// Gets the last open-picker directory.
    /// </summary>
    public string? LastOpenDirectory { get; init; }

    /// <summary>
    /// Gets the last save-picker directory.
    /// </summary>
    public string? LastSaveDirectory { get; init; }

    /// <summary>
    /// Gets the last folder-picker directory.
    /// </summary>
    public string? LastFolderDirectory { get; init; }
}
