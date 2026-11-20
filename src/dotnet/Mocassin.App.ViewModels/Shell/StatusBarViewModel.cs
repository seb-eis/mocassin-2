// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Holds the status-bar values shown by the shell.
/// </summary>
internal sealed partial class StatusBarViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new status bar.
    /// </summary>
    /// <param name="version">The application version text.</param>
    public StatusBarViewModel(string version)
    {
        Version = version;
    }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName { get; } = "MOCASSIN";

    /// <summary>
    /// Gets the application version text.
    /// </summary>
    public string Version { get; }

    [ObservableProperty]
    private string activeDocumentText = "No document";

    [ObservableProperty]
    private string projectText = "No project";
}
