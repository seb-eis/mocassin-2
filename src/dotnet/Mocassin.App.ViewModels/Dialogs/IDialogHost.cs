// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Hosts the modal overlay. The implementation is the shell's dialog host control.
/// </summary>
internal interface IDialogHost
{
    /// <summary>
    /// Shows a dialog and completes when it closes.
    /// </summary>
    /// <param name="dialog">The dialog to show.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The dialog result.</returns>
    public Task<DialogResult> ShowAsync(DialogViewModel dialog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a dialog without waiting for it to close.
    /// </summary>
    /// <param name="dialog">The dialog to show.</param>
    public void Show(DialogViewModel dialog);

    /// <summary>
    /// Closes an open dialog.
    /// </summary>
    /// <param name="dialog">The dialog to close.</param>
    /// <param name="result">The result to record.</param>
    public void Close(DialogViewModel dialog, DialogResult result);
}
