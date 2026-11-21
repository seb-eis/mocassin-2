// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.ViewModels.Scheduling;

/// <summary>
/// Contributes the scheduler's autosave status to the status bar.
/// </summary>
internal sealed class AutosaveStatusItemProvider : IStatusBarItemProvider
{
    private readonly AutosaveScheduler scheduler;
    private readonly StatusBarItemViewModel item = new("Autosave idle", "Autosave status", glyph: "○");

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    /// <param name="scheduler">The autosave scheduler.</param>
    public AutosaveStatusItemProvider(AutosaveScheduler scheduler)
    {
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        scheduler.PropertyChanged += OnSchedulerChanged;
        Update();
    }

    /// <inheritdoc />
    public IEnumerable<StatusBarItemViewModel> CreateItems() => [item];

    private void OnSchedulerChanged(object? sender, PropertyChangedEventArgs e) => Update();

    private void Update()
    {
        item.Text = scheduler.StatusText;
        item.Glyph = scheduler.StatusGlyph;
        item.Severity = scheduler.Severity;
    }
}
