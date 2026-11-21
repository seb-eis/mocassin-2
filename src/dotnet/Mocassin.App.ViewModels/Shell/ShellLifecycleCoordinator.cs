// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Owns the shell unsaved-changes decisions, clean-exit state, and unclean-exit restore flow.
/// </summary>
internal sealed class ShellLifecycleCoordinator
{
    private readonly IProjectSessionService sessions;
    private readonly ISettingsService settings;
    private readonly IDialogService dialogs;
    private readonly IConsoleService console;
    private readonly IProjectFileProbe probe;
    private readonly IShellLifetime lifetime;

    /// <summary>
    /// Initializes the lifecycle coordinator.
    /// </summary>
    /// <param name="sessions">The project sessions.</param>
    /// <param name="settings">The application settings.</param>
    /// <param name="dialogs">The dialog service.</param>
    /// <param name="console">The console used for non-blocking reports.</param>
    /// <param name="probe">The project file probe.</param>
    /// <param name="lifetime">The application lifetime.</param>
    public ShellLifecycleCoordinator(
        IProjectSessionService sessions,
        ISettingsService settings,
        IDialogService dialogs,
        IConsoleService console,
        IProjectFileProbe probe,
        IShellLifetime lifetime)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.console = console ?? throw new ArgumentNullException(nameof(console));
        this.probe = probe ?? throw new ArgumentNullException(nameof(probe));
        this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    /// Runs the clean close and shuts the application down.
    /// </summary>
    /// <param name="startupProjectTask">The startup project-open task.</param>
    /// <returns>A task that completes after the shutdown request.</returns>
    public async Task ExitAsync(Task startupProjectTask)
    {
        if (await RequestCloseAsync(startupProjectTask).ConfigureAwait(true))
        {
            lifetime.Shutdown();
        }
    }

    /// <summary>
    /// Prompts for dirty sessions and closes them before the window closes.
    /// </summary>
    /// <param name="startupProjectTask">The startup project-open task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the window may close.</returns>
    public async Task<bool> RequestCloseAsync(
        Task startupProjectTask,
        CancellationToken cancellationToken = default)
    {
        await startupProjectTask.ConfigureAwait(true);
        foreach (var session in sessions.Sessions.ToArray())
        {
            var discard = false;
            if (session.IsDirty && !session.IsReadOnly)
            {
                var decision = await PromptUnsavedChangesAsync(session, cancellationToken).ConfigureAwait(true);
                if (decision == ShellUnsavedDecision.Cancel)
                {
                    return false;
                }

                if (decision == ShellUnsavedDecision.Save)
                {
                    var saved = await sessions.SaveAsync(session, cancellationToken).ConfigureAwait(true);
                    if (!saved.IsSuccess)
                    {
                        ReportFailure(saved);
                        return false;
                    }
                }
                else
                {
                    discard = true;
                }
            }

            var closed = await sessions.CloseAsync(session, discard, cancellationToken).ConfigureAwait(true);
            if (!closed.IsSuccess && closed.Outcome != ProjectSessionOutcome.Closed)
            {
                ReportFailure(closed);
                return false;
            }
        }

        await sessions.MarkCleanExitAsync(cancellationToken).ConfigureAwait(true);
        return true;
    }

    /// <summary>
    /// Offers the unclean-exit restore after the window is shown.
    /// </summary>
    /// <param name="startupProjectTask">The startup project-open task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the restore offer.</returns>
    public async Task RestoreSessionAsync(
        Task startupProjectTask,
        CancellationToken cancellationToken = default)
    {
        await startupProjectTask.ConfigureAwait(true);
        await sessions.SweepRecoveryAsync(cancellationToken).ConfigureAwait(true);
        var state = settings.Current.Session;
        if (state.CleanExit || state.OpenProjects.IsDefaultOrEmpty)
        {
            return;
        }

        var choice = await dialogs.ChooseAsync(
            new ChoiceDialogRequest(
                "Restore session",
                "The previous MOCASSIN session ended unexpectedly. Restore its projects and recover changes?",
                [
                    new DialogChoice("restore", "Restore session", IsDefault: true),
                    new DialogChoice("later", "Not now", IsCancel: true),
                ]),
            cancellationToken).ConfigureAwait(true);
        if (!string.Equals(choice.ChoiceId, "restore", StringComparison.Ordinal))
        {
            return;
        }

        var missing = new List<string>();
        foreach (var path in state.OpenProjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!probe.GetStamp(path).Exists)
            {
                missing.Add(path);
                continue;
            }

            var opened = await sessions.OpenAsync(path, cancellationToken).ConfigureAwait(true);
            if (!opened.IsSuccess || opened.Session is null)
            {
                missing.Add(path);
                continue;
            }

            if (opened.Session.AvailableRecovery is { } candidate)
            {
                var recovered = await sessions.RecoverAsync(opened.Session, candidate, cancellationToken)
                    .ConfigureAwait(true);
                if (!recovered.IsSuccess)
                {
                    ReportFailure(recovered);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(state.ActiveProject))
        {
            await sessions.ActivateAsync(state.ActiveProject, cancellationToken).ConfigureAwait(true);
        }

        if (missing.Count > 0)
        {
            await dialogs.ShowErrorAsync(
                new ErrorDialogRequest(
                    "Session restore",
                    "Some projects from the previous session could not be restored.",
                    [.. missing.Select(path => new ErrorDialogDetail("session", path))]),
                cancellationToken).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Prompts for one dirty session.
    /// </summary>
    /// <param name="session">The dirty session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's decision.</returns>
    public async Task<ShellUnsavedDecision> PromptUnsavedChangesAsync(
        ProjectSession session,
        CancellationToken cancellationToken = default)
    {
        var result = await dialogs.ChooseAsync(
            new ChoiceDialogRequest(
                "Unsaved changes",
                $"'{session.Name}' has unsaved changes. Save them before closing?",
                [
                    new DialogChoice("save", "Save", IsDefault: true),
                    new DialogChoice("discard", "Discard"),
                    new DialogChoice("cancel", "Cancel", IsCancel: true),
                ]),
            cancellationToken).ConfigureAwait(true);
        return result.ChoiceId switch
        {
            "save" => ShellUnsavedDecision.Save,
            "discard" => ShellUnsavedDecision.Discard,
            _ => ShellUnsavedDecision.Cancel,
        };
    }

    private void ReportFailure(ProjectSessionResult result) =>
        console.Write(
            ConsoleSeverity.Warning,
            "project",
            result.Message ?? $"The project operation ended with {result.Outcome}.");
}

/// <summary>
/// The user's decision for a dirty project.
/// </summary>
internal enum ShellUnsavedDecision
{
    /// <summary>
    /// Save and close.
    /// </summary>
    Save,

    /// <summary>
    /// Discard and close.
    /// </summary>
    Discard,

    /// <summary>
    /// Abort the close.
    /// </summary>
    Cancel,
}
