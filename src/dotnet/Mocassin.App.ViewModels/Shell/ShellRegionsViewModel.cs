// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Holds the visibility of the four shell regions.
/// </summary>
internal sealed partial class ShellRegionsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isProjectsVisible = true;

    [ObservableProperty]
    private bool isInspectorVisible = true;

    [ObservableProperty]
    private bool isConsoleVisible = true;

    /// <summary>
    /// Toggles the projects region.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleProjects() => IsProjectsVisible = !IsProjectsVisible;

    /// <summary>
    /// Toggles the inspector region.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleInspector() => IsInspectorVisible = !IsInspectorVisible;

    /// <summary>
    /// Toggles the console region.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleConsole() => IsConsoleVisible = !IsConsoleVisible;
}
