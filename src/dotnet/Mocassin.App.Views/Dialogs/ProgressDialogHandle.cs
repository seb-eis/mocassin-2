// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// Closes a progress dialog and mirrors its state into the status bar.
/// </summary>
internal sealed class ProgressDialogHandle : IProgressDialog
{
    private readonly IDialogHost host;
    private readonly ProgressDialogViewModel dialog;
    private readonly ProgressService progress;
    private readonly IUiDispatcher dispatcher;
    private bool isClosed;

    /// <summary>
    /// Initializes a new progress handle.
    /// </summary>
    /// <param name="host">The overlay host.</param>
    /// <param name="dialog">The progress dialog view model.</param>
    /// <param name="progress">The status-bar progress state.</param>
    /// <param name="dispatcher">The UI dispatcher.</param>
    public ProgressDialogHandle(
        IDialogHost host,
        ProgressDialogViewModel dialog,
        ProgressService progress,
        IUiDispatcher dispatcher)
    {
        this.host = host;
        this.dialog = dialog;
        this.progress = progress;
        this.dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public CancellationToken CancellationToken => dialog.CancellationToken;

    /// <inheritdoc />
    public void Report(double? fraction, string? statusText = null)
    {
        if (dispatcher.IsOnUiThread)
        {
            ReportCore(fraction, statusText);
            return;
        }

        dispatcher.Post(() => ReportCore(fraction, statusText));
    }

    /// <inheritdoc />
    public void Complete() => Close();

    /// <inheritdoc />
    public void Dispose() => Close();

    private void ReportCore(double? fraction, string? statusText)
    {
        dialog.Report(fraction, statusText);
        progress.Report(fraction, statusText);
    }

    private void Close()
    {
        if (isClosed)
        {
            return;
        }

        isClosed = true;
        if (dispatcher.IsOnUiThread)
        {
            CloseCore();
            return;
        }

        dispatcher.Post(CloseCore);
    }

    private void CloseCore()
    {
        dialog.MarkCompleted();
        host.Close(dialog, DialogResult.Dismissed);
        progress.End();
        dialog.Dispose();
    }
}
