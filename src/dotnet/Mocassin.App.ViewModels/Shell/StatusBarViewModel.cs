// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Holds the status-bar items shown by the shell.
/// </summary>
internal sealed partial class StatusBarViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new status bar.
    /// </summary>
    /// <param name="version">The application version text.</param>
    /// <param name="providers">The optional item providers.</param>
    public StatusBarViewModel(string version, IEnumerable<IStatusBarItemProvider>? providers = null)
    {
        Version = version;
        ActiveDocument = new StatusBarItemViewModel("No document");
        Project = new StatusBarItemViewModel("No project");
        VersionItem = new StatusBarItemViewModel(version, "Application version");
        Appearance = new StatusBarItemViewModel("System · Comfortable", "Theme and density");
        var items = new ObservableCollection<StatusBarItemViewModel>
        {
            ActiveDocument,
            Project,
            Appearance,
            VersionItem,
        };
        if (providers is not null)
        {
            foreach (var item in providers.SelectMany(provider => provider.CreateItems()))
            {
                items.Add(item);
            }
        }

        Items = items;
    }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName { get; } = "MOCASSIN";

    /// <summary>
    /// Gets the application version text.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the active-document item.
    /// </summary>
    public StatusBarItemViewModel ActiveDocument { get; }

    /// <summary>
    /// Gets the project item.
    /// </summary>
    public StatusBarItemViewModel Project { get; }

    /// <summary>
    /// Gets the appearance item.
    /// </summary>
    public StatusBarItemViewModel Appearance { get; }

    /// <summary>
    /// Gets the version item.
    /// </summary>
    public StatusBarItemViewModel VersionItem { get; }

    /// <summary>
    /// Gets every status-bar item.
    /// </summary>
    public ObservableCollection<StatusBarItemViewModel> Items { get; }

    /// <summary>
    /// Updates the active-document item.
    /// </summary>
    /// <param name="title">The active document title.</param>
    public void SetActiveDocument(string? title) => ActiveDocument.Text = title ?? "No document";

    /// <summary>
    /// Updates the project item.
    /// </summary>
    /// <param name="text">The project text.</param>
    public void SetProject(string? text) => Project.Text = text ?? "No project";

    /// <summary>
    /// Updates the appearance item.
    /// </summary>
    /// <param name="text">The appearance text.</param>
    public void SetAppearance(string text) => Appearance.Text = text;
}
