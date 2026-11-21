// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Copies text to the platform clipboard. The implementation lives in the view layer.
/// </summary>
internal interface IClipboardService
{
    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the text has been placed on the clipboard.</returns>
    public Task SetTextAsync(string text, CancellationToken cancellationToken = default);
}
