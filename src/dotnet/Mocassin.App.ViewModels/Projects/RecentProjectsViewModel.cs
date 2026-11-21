// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Settings;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Opens, removes, and renders the recent-project list, including missing-file state.
/// </summary>
internal sealed partial class RecentProjectsViewModel : ObservableObject
{
    private readonly ISettingsService settings;
    private readonly IProjectFileProbe probe;
    private readonly Func<string, Task> openProject;

    /// <summary>
    /// Initializes the recent-project list.
    /// </summary>
    /// <param name="settings">The settings service.</param>
    /// <param name="probe">The file probe used for missing-file state.</param>
    /// <param name="openProject">The shell action that opens a path and handles the result.</param>
    public RecentProjectsViewModel(
        ISettingsService settings,
        IProjectFileProbe probe,
        Func<string, Task> openProject)
    {
        this.settings = settings;
        this.probe = probe;
        this.openProject = openProject;
        Refresh();
    }

    /// <summary>
    /// Gets the recent-project rows.
    /// </summary>
    public ObservableCollection<RecentProjectViewModel> Entries { get; } = [];

    /// <summary>
    /// Rebuilds the list from settings.
    /// </summary>
    public void Refresh()
    {
        Entries.Clear();
        foreach (var file in settings.Current.RecentFiles)
        {
            var missing = probe.GetTargetState(file.Path) != ProjectTargetState.File;
            var captured = file;
            Entries.Add(
                new RecentProjectViewModel(
                    captured,
                    missing,
                    () => openProject(captured.Path),
                    () => RemoveAsync(captured)));
        }
    }

    private async Task RemoveAsync(RecentFile file)
    {
        var updated = settings.Current.RecentFiles.Where(candidate => candidate != file).ToImmutableArray();
        await settings.UpdateAsync(settings.Current with { RecentFiles = updated });
        Refresh();
    }
}
