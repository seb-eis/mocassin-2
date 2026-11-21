// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Owns open/close/reload result handling, recovery prompts, and document navigation.
/// </summary>
internal sealed class ShellProjectOperations
{
    private readonly IProjectSessionService sessions;
    private readonly IDialogService dialogs;
    private readonly IConsoleService console;
    private readonly INavigationService navigation;
    private readonly DocumentCollectionViewModel documents;
    private readonly ShellLifecycleCoordinator lifecycle;

    /// <summary>
    /// Initializes the project operations.
    /// </summary>
    /// <param name="sessions">The project sessions.</param>
    /// <param name="dialogs">The dialog service.</param>
    /// <param name="console">The console.</param>
    /// <param name="navigation">The navigation service.</param>
    /// <param name="documents">The open documents.</param>
    /// <param name="lifecycle">The lifecycle coordinator.</param>
    public ShellProjectOperations(
        IProjectSessionService sessions,
        IDialogService dialogs,
        IConsoleService console,
        INavigationService navigation,
        DocumentCollectionViewModel documents,
        ShellLifecycleCoordinator lifecycle)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.console = console ?? throw new ArgumentNullException(nameof(console));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.documents = documents ?? throw new ArgumentNullException(nameof(documents));
        this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    /// <summary>
    /// Opens a project path and applies the result.
    /// </summary>
    /// <param name="path">The project path.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the result is applied.</returns>
    public async Task OpenAsync(string path, CancellationToken cancellationToken = default) =>
        await ApplyResultAsync(await sessions.OpenAsync(path, cancellationToken), cancellationToken)
            .ConfigureAwait(true);

    /// <summary>
    /// Applies one session result: reports failure, offers recovery, and navigates to the overview.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the result is applied.</returns>
    public async Task ApplyResultAsync(
        ProjectSessionResult result,
        CancellationToken cancellationToken = default)
    {
        if (!result.IsSuccess)
        {
            ReportFailure(result);
            return;
        }

        if (result.Outcome == ProjectSessionOutcome.Closed)
        {
            return;
        }

        if (result.Session is { } session)
        {
            if (session.AvailableRecovery is { } candidate
                && result.Outcome is ProjectSessionOutcome.Opened
                    or ProjectSessionOutcome.OpenedReadOnly
                    or ProjectSessionOutcome.Reloaded)
            {
                await PromptRecoveryAsync(session, candidate, cancellationToken).ConfigureAwait(true);
            }

            await navigation.NavigateToAsync(
                new ModelPath("project", session.Id, "overview"),
                NavigationIntent.Activate,
                cancellationToken).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Closes the active project after the unsaved-changes prompt.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the operation.</returns>
    public async Task CloseActiveAsync(CancellationToken cancellationToken = default)
    {
        if (sessions.ActiveSession is not { } session)
        {
            return;
        }

        var discard = false;
        if (session.IsDirty && !session.IsReadOnly)
        {
            var decision = await lifecycle.PromptUnsavedChangesAsync(session, cancellationToken)
                .ConfigureAwait(true);
            if (decision == ShellUnsavedDecision.Cancel)
            {
                return;
            }

            if (decision == ShellUnsavedDecision.Save)
            {
                var saved = await sessions.SaveAsync(session, cancellationToken).ConfigureAwait(true);
                if (!saved.IsSuccess)
                {
                    await ApplyResultAsync(saved, cancellationToken).ConfigureAwait(true);
                    return;
                }
            }
            else
            {
                discard = true;
            }
        }

        foreach (var document in documents.Documents
                     .Where(document => document.Key.ScopeId == session.Id)
                     .ToArray())
        {
            await documents.CloseAsync(document, cancellationToken).ConfigureAwait(true);
        }

        await ApplyResultAsync(
            await sessions.CloseAsync(session, discard, cancellationToken).ConfigureAwait(true),
            cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Reloads the active project after the discard confirmation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after the operation.</returns>
    public async Task ReloadActiveAsync(CancellationToken cancellationToken = default)
    {
        if (sessions.ActiveSession is not { } session)
        {
            return;
        }

        if (session.IsDirty)
        {
            var confirmed = await dialogs.ConfirmAsync(
                new ConfirmDialogRequest(
                    "Reload and discard changes?",
                    $"'{session.Name}' has unsaved changes. Reloading discards them.",
                    "Reload",
                    "Keep",
                    IsDestructive: true),
                cancellationToken).ConfigureAwait(true);
            if (!confirmed)
            {
                return;
            }
        }

        await ApplyResultAsync(
            await sessions.ReloadAsync(session, discardChanges: true, cancellationToken).ConfigureAwait(true),
            cancellationToken).ConfigureAwait(true);
    }

    private async Task PromptRecoveryAsync(
        ProjectSession session,
        Mocassin.App.ViewModels.Recovery.RecoveryCandidate candidate,
        CancellationToken cancellationToken)
    {
        var choice = await dialogs.ChooseAsync(
            new ChoiceDialogRequest(
                "Recovery available",
                $"A recovery snapshot for '{session.Name}' is available. Recover it, discard it, or decide later?",
                [
                    new DialogChoice("recover", "Recover", IsDefault: true),
                    new DialogChoice("discard", "Discard"),
                    new DialogChoice("later", "Decide later", IsCancel: true),
                ]),
            cancellationToken).ConfigureAwait(true);
        switch (choice.ChoiceId)
        {
            case "recover":
                await ApplyResultAsync(
                    await sessions.RecoverAsync(session, candidate, cancellationToken).ConfigureAwait(true),
                    cancellationToken).ConfigureAwait(true);
                break;
            case "discard":
                await ApplyResultAsync(
                    await sessions.DiscardRecoveryAsync(session, candidate, cancellationToken)
                        .ConfigureAwait(true),
                    cancellationToken).ConfigureAwait(true);
                break;
            default:
                session.PendingRecovery = candidate;
                break;
        }
    }

    private void ReportFailure(ProjectSessionResult result) =>
        console.Write(
            ConsoleSeverity.Warning,
            "project",
            result.Message ?? $"The project operation ended with {result.Outcome}.");
}
