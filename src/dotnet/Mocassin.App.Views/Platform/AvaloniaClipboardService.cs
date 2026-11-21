// SPDX-License-Identifier: MIT

using Avalonia.Input.Platform;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Avalonia clipboard implementation. The clipboard is attached by the shell window.
/// </summary>
internal sealed class AvaloniaClipboardService : IClipboardService
{
    /// <summary>
    /// Gets or sets the platform clipboard.
    /// </summary>
    public IClipboard? Clipboard { get; set; }

    /// <inheritdoc />
    public async Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Clipboard is null)
        {
            return;
        }

        await Clipboard.SetTextAsync(text);
    }
}
