// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Recovery;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Shows modal dialogs and the progress surface. The implementation is an in-shell overlay.
/// </summary>
internal interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="request">The confirmation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the user confirmed.</returns>
    public Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a choice dialog.
    /// </summary>
    /// <param name="request">The choice request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result carrying the selected choice id.</returns>
    public Task<DialogResult> ChooseAsync(ChoiceDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a single-line text prompt.
    /// </summary>
    /// <param name="request">The prompt request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entered text, or <see langword="null"/> when cancelled.</returns>
    public Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows an error report.
    /// </summary>
    /// <param name="request">The error request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the report is dismissed.</returns>
    public Task ShowErrorAsync(ErrorDialogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows the crash dialog.
    /// </summary>
    /// <param name="request">The crash request.</param>
    /// <param name="recoverySnapshots">The dirty sessions captured before the dialog.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the crash dialog closes.</returns>
    public Task ShowCrashAsync(
        CrashDialogRequest request,
        IReadOnlyList<CrashRecoverySnapshot>? recoverySnapshots = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a progress surface for a long-running operation.
    /// </summary>
    /// <param name="request">The progress request.</param>
    /// <returns>The handle that reports progress and closes the surface.</returns>
    public IProgressDialog ShowProgress(ProgressDialogRequest request);
}
