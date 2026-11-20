// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Consoles;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Root view model of the application shell.
/// </summary>
internal sealed partial class ShellViewModel : ObservableObject
{
    private readonly IShellLifetime lifetime;

    /// <summary>
    /// Initializes a new shell view model.
    /// </summary>
    /// <param name="consoleService">The console service that owns the channels.</param>
    /// <param name="regions">The region visibility state.</param>
    /// <param name="statusBar">The status-bar state.</param>
    /// <param name="progress">The status-bar progress state.</param>
    /// <param name="lifetime">The application lifetime used by the exit command.</param>
    public ShellViewModel(
        IConsoleService consoleService,
        ShellRegionsViewModel regions,
        StatusBarViewModel statusBar,
        ProgressService progress,
        IShellLifetime lifetime)
    {
        Regions = regions;
        StatusBar = statusBar;
        Progress = progress;
        this.lifetime = lifetime;
        Console = new ConsoleViewModel(consoleService);

        consoleService.Write(ConsoleSeverity.Information, "shell", "MOCASSIN shell started.");
    }

    /// <summary>
    /// Gets the region state.
    /// </summary>
    public ShellRegionsViewModel Regions { get; }

    /// <summary>
    /// Gets the status-bar state.
    /// </summary>
    public StatusBarViewModel StatusBar { get; }

    /// <summary>
    /// Gets the console state.
    /// </summary>
    public ConsoleViewModel Console { get; }

    /// <summary>
    /// Gets the progress state shown by the status bar.
    /// </summary>
    public ProgressService Progress { get; }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    public string WindowTitle { get; } = "MOCASSIN";

    /// <summary>
    /// Gets the text shown when the documents region is empty.
    /// </summary>
    public string EmptyDocumentText { get; } = "No document is open.";

    /// <summary>
    /// Exits the application.
    /// </summary>
    [RelayCommand]
    private void Exit() => lifetime.Shutdown();
}
