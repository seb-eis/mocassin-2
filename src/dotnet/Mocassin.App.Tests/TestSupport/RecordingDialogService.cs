// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Recovery;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Records dialog requests and returns scripted results.
/// </summary>
internal sealed class RecordingDialogService : IDialogService
{
    /// <summary>
    /// Gets the recorded confirmation requests.
    /// </summary>
    public List<ConfirmDialogRequest> ConfirmRequests { get; } = [];

    /// <summary>
    /// Gets the recorded choice requests.
    /// </summary>
    public List<ChoiceDialogRequest> ChoiceRequests { get; } = [];

    /// <summary>
    /// Gets the recorded prompt requests.
    /// </summary>
    public List<TextPromptRequest> PromptRequests { get; } = [];

    /// <summary>
    /// Gets the recorded error requests.
    /// </summary>
    public List<ErrorDialogRequest> ErrorRequests { get; } = [];

    /// <summary>
    /// Gets the recorded crash requests.
    /// </summary>
    public List<CrashDialogRequest> CrashRequests { get; } = [];

    /// <summary>
    /// Gets the captured crash snapshots from the most recent request.
    /// </summary>
    public List<CrashRecoverySnapshot> CrashSnapshots { get; } = [];

    /// <summary>
    /// Gets or sets the scripted confirmation result.
    /// </summary>
    public bool ConfirmResult { get; set; } = true;

    /// <summary>
    /// Gets or sets the scripted prompt result.
    /// </summary>
    public string? PromptResult { get; set; }

    /// <summary>
    /// Gets or sets an optional choice handler; the first choice is selected when it is unset.
    /// </summary>
    public Func<ChoiceDialogRequest, DialogResult>? ChoiceHandler { get; set; }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default)
    {
        ConfirmRequests.Add(request);
        return Task.FromResult(ConfirmResult);
    }

    /// <inheritdoc />
    public Task<DialogResult> ChooseAsync(ChoiceDialogRequest request, CancellationToken cancellationToken = default)
    {
        ChoiceRequests.Add(request);
        if (ChoiceHandler is not null)
        {
            return Task.FromResult(ChoiceHandler(request));
        }

        var choice = request.Choices.Count == 0 ? null : request.Choices[0];
        return Task.FromResult(
            choice is null
                ? DialogResult.Canceled
                : new DialogResult(DialogResultKind.ChoiceSelected, ChoiceId: choice.Id));
    }

    /// <inheritdoc />
    public Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken = default)
    {
        PromptRequests.Add(request);
        return Task.FromResult(PromptResult);
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(ErrorDialogRequest request, CancellationToken cancellationToken = default)
    {
        ErrorRequests.Add(request);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowCrashAsync(
        CrashDialogRequest request,
        IReadOnlyList<CrashRecoverySnapshot>? recoverySnapshots = null,
        CancellationToken cancellationToken = default)
    {
        CrashRequests.Add(request);
        if (recoverySnapshots is not null)
        {
            CrashSnapshots.AddRange(recoverySnapshots);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IProgressDialog ShowProgress(ProgressDialogRequest request) => new RecordingProgressDialog();
}
