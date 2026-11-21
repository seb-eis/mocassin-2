// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Shows the crash dialog after an unhandled exception. Recovery actions belong to Step 23B.
/// </summary>
internal sealed partial class CrashDialogViewModel : DialogViewModel
{
    private readonly IShellLifetime? lifetime;
    private readonly IShellLauncher? launcher;
    private readonly ICrashRecoveryWriter? recoveryWriter;
    private readonly IReadOnlyList<CrashRecoverySnapshot> snapshots;

    [ObservableProperty]
    private bool isWritingRecovery;

    [ObservableProperty]
    private string? recoveryStatus;

    /// <summary>
    /// Initializes a new crash dialog.
    /// </summary>
    /// <param name="request">The crash request.</param>
    /// <param name="lifetime">The lifetime used by the exit action.</param>
    /// <param name="launcher">The launcher used by the open-log-folder action.</param>
    /// <param name="recoveryWriter">The optional bounded crash-recovery writer.</param>
    /// <param name="snapshots">The dirty sessions captured before the dialog was shown.</param>
    public CrashDialogViewModel(
        CrashDialogRequest request,
        IShellLifetime lifetime,
        IShellLauncher launcher,
        ICrashRecoveryWriter? recoveryWriter = null,
        IReadOnlyList<CrashRecoverySnapshot>? snapshots = null)
        : base(request.Title, request.Message)
    {
        this.lifetime = lifetime;
        this.launcher = launcher;
        this.recoveryWriter = recoveryWriter;
        this.snapshots = snapshots ?? [];
        LogFilePath = request.LogFilePath;
        Details = request.Details ?? string.Empty;
    }

    /// <inheritdoc />
    public override bool CanCancel => false;

    /// <summary>
    /// Gets the log folder or file path.
    /// </summary>
    public string? LogFilePath { get; }

    /// <summary>
    /// Gets the optional stack trace and context.
    /// </summary>
    public string Details { get; }

    /// <summary>
    /// Gets a value indicating whether the crash details have content.
    /// </summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    /// <summary>
    /// Gets a value indicating whether the log folder can be opened.
    /// </summary>
    public bool CanOpenLogFolder => !string.IsNullOrWhiteSpace(LogFilePath);

    /// <summary>
    /// Gets a value indicating whether recovery snapshots are available.
    /// </summary>
    public bool HasRecoverySnapshots => snapshots.Count > 0;

    /// <summary>
    /// Gets a value indicating whether a recovery status message is visible.
    /// </summary>
    public bool HasRecoveryStatus => !string.IsNullOrWhiteSpace(RecoveryStatus);

    /// <summary>
    /// Gets a value indicating whether the exit action is enabled.
    /// </summary>
    public bool CanExit => !IsWritingRecovery;

    /// <summary>
    /// Gets a value indicating whether the recovery action is available.
    /// </summary>
    public bool CanWriteRecoveryFiles =>
        !IsWritingRecovery && HasRecoverySnapshots && recoveryWriter is not null;

    partial void OnRecoveryStatusChanged(string? value) =>
        OnPropertyChanged(nameof(HasRecoveryStatus));

    partial void OnIsWritingRecoveryChanged(bool value)
    {
        OnPropertyChanged(nameof(CanExit));
        OnPropertyChanged(nameof(CanWriteRecoveryFiles));
        WriteRecoveryFilesCommand.NotifyCanExecuteChanged();
        ExitCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Writes the captured dirty sessions to recovery files within the writer's budget.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanWriteRecoveryFiles))]
    private async Task WriteRecoveryFilesAsync(CancellationToken cancellationToken)
    {
        if (recoveryWriter is null || snapshots.Count == 0)
        {
            return;
        }

        IsWritingRecovery = true;
        RecoveryStatus = "Writing recovery files…";
        try
        {
            var results = await recoveryWriter.WriteAsync(snapshots, cancellationToken);
            var saved = results.Count(result => result.Status == CrashRecoveryStatus.Saved);
            var failed = results.Count(result => result.Status == CrashRecoveryStatus.Failed);
            var timedOut = results.Count(result => result.Status == CrashRecoveryStatus.TimedOut);
            RecoveryStatus =
                $"Recovery files: {saved} saved, {failed} failed, {timedOut} timed out.";
        }
        catch (OperationCanceledException)
        {
            RecoveryStatus = "Recovery writing was cancelled.";
        }
#pragma warning disable CA1031 // The crash dialog must not throw while the process is already failing.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            RecoveryStatus = $"Recovery writing failed: {exception.Message}";
        }
        finally
        {
            IsWritingRecovery = false;
        }
    }

    /// <summary>
    /// Opens the log folder.
    /// </summary>
    [RelayCommand]
    private async Task OpenLogFolderAsync(CancellationToken cancellationToken)
    {
        if (LogFilePath is null || launcher is null)
        {
            return;
        }

        await launcher.OpenFolderAsync(LogFilePath, cancellationToken);
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExit))]
    private void Exit()
    {
        Close(DialogResult.Dismissed);
        lifetime?.Shutdown(1);
    }
}
