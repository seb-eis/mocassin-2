// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Dialogs;

/// <summary>
/// Shows the crash dialog after an unhandled exception. Recovery actions belong to Step 23B.
/// </summary>
internal sealed partial class CrashDialogViewModel : DialogViewModel
{
    private readonly IShellLifetime? lifetime;
    private readonly IShellLauncher? launcher;

    /// <summary>
    /// Initializes a new crash dialog.
    /// </summary>
    /// <param name="request">The crash request.</param>
    /// <param name="lifetime">The lifetime used by the exit action.</param>
    /// <param name="launcher">The launcher used by the open-log-folder action.</param>
    public CrashDialogViewModel(CrashDialogRequest request, IShellLifetime lifetime, IShellLauncher launcher)
        : base(request.Title, request.Message)
    {
        this.lifetime = lifetime;
        this.launcher = launcher;
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
    [RelayCommand]
    private void Exit()
    {
        Close(DialogResult.Dismissed);
        lifetime?.Shutdown(1);
    }
}
