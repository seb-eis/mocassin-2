// SPDX-License-Identifier: MIT

using System.Diagnostics;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Opens folders with the platform shell.
/// </summary>
internal sealed class AvaloniaShellLauncher : IShellLauncher
{
    /// <inheritdoc />
    public Task OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        };

        using var process = Process.Start(startInfo);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        cancellationToken.ThrowIfCancellationRequested();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = uri,
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }
}
