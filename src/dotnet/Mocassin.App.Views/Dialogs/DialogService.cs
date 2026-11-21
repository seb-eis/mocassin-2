// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Views.Dialogs;

/// <summary>
/// Dialog service backed by the shell's in-window overlay.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly ProgressService progress;
    private readonly IShellLifetime lifetime;
    private readonly IShellLauncher launcher;
    private readonly IUiDispatcher dispatcher;
    private readonly ICrashRecoveryWriter? recoveryWriter;

    /// <summary>
    /// Initializes a new dialog service.
    /// </summary>
    /// <param name="progress">The status-bar progress state.</param>
    /// <param name="lifetime">The application lifetime used by the crash dialog.</param>
    /// <param name="launcher">The shell launcher used by the crash dialog.</param>
    /// <param name="dispatcher">The UI dispatcher used by the progress handle.</param>
    /// <param name="recoveryWriter">The optional crash-recovery writer.</param>
    public DialogService(
        ProgressService progress,
        IShellLifetime lifetime,
        IShellLauncher launcher,
        IUiDispatcher dispatcher,
        ICrashRecoveryWriter? recoveryWriter = null)
    {
        this.progress = progress;
        this.lifetime = lifetime;
        this.launcher = launcher;
        this.dispatcher = dispatcher;
        this.recoveryWriter = recoveryWriter;
    }

    /// <summary>
    /// Gets or sets the attached overlay host.
    /// </summary>
    public IDialogHost? Host { get; set; }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        ConfirmDialogRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await RequiredHost.ShowAsync(new ConfirmDialogViewModel(request), cancellationToken);
        return result.Kind == DialogResultKind.Confirmed;
    }

    /// <inheritdoc />
    public Task<DialogResult> ChooseAsync(
        ChoiceDialogRequest request,
        CancellationToken cancellationToken = default) =>
        RequiredHost.ShowAsync(new ChoiceDialogViewModel(request), cancellationToken);

    /// <inheritdoc />
    public async Task<string?> PromptAsync(
        TextPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await RequiredHost.ShowAsync(new TextPromptDialogViewModel(request), cancellationToken);
        return result.Kind == DialogResultKind.TextEntered ? result.Text : null;
    }

    /// <inheritdoc />
    public Task ShowErrorAsync(ErrorDialogRequest request, CancellationToken cancellationToken = default) =>
        ShowAndIgnoreAsync(new ErrorDialogViewModel(request), cancellationToken);

    /// <inheritdoc />
    public Task ShowCrashAsync(
        CrashDialogRequest request,
        IReadOnlyList<CrashRecoverySnapshot>? recoverySnapshots = null,
        CancellationToken cancellationToken = default) =>
        ShowAndIgnoreAsync(
            new CrashDialogViewModel(request, lifetime, launcher, recoveryWriter, recoverySnapshots),
            cancellationToken);

    /// <inheritdoc />
    public IProgressDialog ShowProgress(ProgressDialogRequest request)
    {
        var dialog = new ProgressDialogViewModel(request);
        progress.Begin(request.Title, request.IsIndeterminate);
        RequiredHost.Show(dialog);
        return new ProgressDialogHandle(RequiredHost, dialog, progress, dispatcher);
    }

    private async Task ShowAndIgnoreAsync(DialogViewModel dialog, CancellationToken cancellationToken) =>
        await RequiredHost.ShowAsync(dialog, cancellationToken);

    private IDialogHost RequiredHost =>
        Host ?? throw new InvalidOperationException("The dialog host is not attached to a shell window.");
}
