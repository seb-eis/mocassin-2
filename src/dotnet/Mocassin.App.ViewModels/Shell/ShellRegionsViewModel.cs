// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Holds the visibility of the four shell regions and their proportional layout.
/// </summary>
internal sealed partial class ShellRegionsViewModel : ObservableObject, IEditorPaneLayout
{
    private double leftFraction = SettingsDefaults.DefaultLeftFraction;
    private double rightFraction = SettingsDefaults.DefaultRightFraction;
    private double consoleFraction = SettingsDefaults.DefaultConsoleFraction;
    private double editorFraction = SettingsDefaults.DefaultEditorFraction;

    [ObservableProperty]
    private bool isProjectsVisible = true;

    [ObservableProperty]
    private bool isInspectorVisible = true;

    [ObservableProperty]
    private bool isConsoleVisible = true;

    /// <summary>
    /// Gets or sets the projects panel share of the workspace width.
    /// </summary>
    public double LeftFraction
    {
        get => leftFraction;
        set
        {
            if (SetProperty(
                    ref leftFraction,
                    ShellLayoutFractions.ClampSide(value, SettingsDefaults.DefaultLeftFraction)))
            {
                OnPropertyChanged(nameof(CentreFraction));
            }
        }
    }

    /// <summary>
    /// Gets or sets the inspector share of the workspace width.
    /// </summary>
    public double RightFraction
    {
        get => rightFraction;
        set
        {
            if (SetProperty(
                    ref rightFraction,
                    ShellLayoutFractions.ClampSide(value, SettingsDefaults.DefaultRightFraction)))
            {
                OnPropertyChanged(nameof(CentreFraction));
            }
        }
    }

    /// <summary>
    /// Gets or sets the console share of the documents plus console height.
    /// </summary>
    public double ConsoleFraction
    {
        get => consoleFraction;
        set
        {
            if (SetProperty(
                    ref consoleFraction,
                    ShellLayoutFractions.ClampConsole(value)))
            {
                OnPropertyChanged(nameof(DocumentsFraction));
            }
        }
    }

    /// <summary>
    /// Gets or sets the editor selection-pane share of the editor width.
    /// </summary>
    public double EditorFraction
    {
        get => editorFraction;
        set => SetProperty(
            ref editorFraction,
            ShellLayoutFractions.ClampEditor(value));
    }

    /// <summary>
    /// Gets the centre column share.
    /// </summary>
    public double CentreFraction => ShellLayoutFractions.Centre(LeftFraction, RightFraction);

    /// <summary>
    /// Gets the documents row share.
    /// </summary>
    public double DocumentsFraction => ShellLayoutFractions.Documents(ConsoleFraction);

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
