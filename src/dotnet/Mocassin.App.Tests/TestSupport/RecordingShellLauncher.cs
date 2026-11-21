// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Records shell launcher calls.
/// </summary>
internal sealed class RecordingShellLauncher : IShellLauncher
{
    /// <summary>
    /// Gets the opened folders.
    /// </summary>
    public List<string> Folders { get; } = [];

    /// <summary>
    /// Gets the opened URIs.
    /// </summary>
    public List<string> Uris { get; } = [];

    /// <inheritdoc />
    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Folders.Add(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Uris.Add(uri);
        return Task.CompletedTask;
    }
}
