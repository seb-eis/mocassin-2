// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Packages.Projects;

namespace Mocassin.App.ViewModels.Scheduling;

/// <summary>
/// Performs one autosave tick over every writable, dirty, due project session.
/// </summary>
internal sealed partial class AutosaveScheduler : ObservableObject
{
    private readonly IProjectSessionService sessions;
    private readonly ISettingsService settings;
    private readonly ProjectStore store;
    private readonly IRecoveryStore recovery;

    [ObservableProperty]
    private string statusText = "Autosave idle";

    [ObservableProperty]
    private string statusGlyph = "○";

    [ObservableProperty]
    private StatusBarSeverity severity = StatusBarSeverity.Normal;

    /// <summary>
    /// Initializes the scheduler.
    /// </summary>
    /// <param name="sessions">The project sessions.</param>
    /// <param name="settings">The application settings.</param>
    /// <param name="store">The project store used for sidecar writes.</param>
    /// <param name="recovery">The recovery store used for failed-sidecar fallback.</param>
    /// <param name="timeProvider">The clock.</param>
    public AutosaveScheduler(
        IProjectSessionService sessions,
        ISettingsService settings,
        ProjectStore store,
        IRecoveryStore recovery,
        TimeProvider timeProvider)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        ArgumentNullException.ThrowIfNull(timeProvider);
    }

    /// <summary>
    /// Gets the number of sidecar writes attempted by this scheduler.
    /// </summary>
    internal int WriteAttempts { get; private set; }

    /// <summary>
    /// Gets the number of successfully written sidecars.
    /// </summary>
    internal int SuccessfulWrites { get; private set; }

    /// <summary>
    /// Runs one autosave tick.
    /// </summary>
    /// <param name="now">The current UTC time.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes after every due session has been considered.</returns>
    public async Task RunOnceAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var autosave = settings.Current.Autosave;
        if (!autosave.Enabled)
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(autosave.IntervalMinutes);
        foreach (var session in sessions.Sessions.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.IsReadOnly
                || session.IsClosing
                || !session.IsDirty
                || !session.Document.IsAutosaveDue(now, interval))
            {
                continue;
            }

            if (!await session.Gate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            try
            {
                if (session.IsClosing
                    || !session.IsDirty
                    || !session.Document.IsAutosaveDue(now, interval))
                {
                    continue;
                }

                StatusText = "Auto-saving…";
                StatusGlyph = "…";
                Severity = StatusBarSeverity.Information;
                WriteAttempts++;
                var snapshot = session.Project;
                var result = await store.WriteAutosaveAsync(snapshot, session.Path, cancellationToken)
                    .ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    var fallback = await recovery.WriteAsync(
                            snapshot,
                            session.Path,
                            RecoverySource.AutosaveFallback,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!fallback.IsSuccess)
                    {
                        StatusText = "Autosave failed";
                        StatusGlyph = "⚠";
                        Severity = StatusBarSeverity.Warning;
                        continue;
                    }
                }

                session.Document.MarkAutosaved(now);
                SuccessfulWrites++;
                StatusText = string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"Auto-saved {now.ToLocalTime():HH:mm}");
                StatusGlyph = "✔";
                Severity = StatusBarSeverity.Success;
            }
            finally
            {
                session.Gate.Release();
            }
        }
    }
}
