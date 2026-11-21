// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Settings.Dialog;

/// <summary>
/// Editable settings dialog state.
/// </summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService settings;
    private readonly ISettingsEnvironment environment;

    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private ThemeSelection theme;

    [ObservableProperty]
    private string selectedAccent = "default";

    [ObservableProperty]
    private Density density;

    [ObservableProperty]
    private double fontScale = SettingsDefaults.DefaultFontScale;

    [ObservableProperty]
    private bool highContrast;

    [ObservableProperty]
    private bool reduceMotion;

    [ObservableProperty]
    private bool confirmOnExternalChange = true;

    [ObservableProperty]
    private bool showWelcomeOnStart = true;

    [ObservableProperty]
    private SettingsLogLevel logLevel = SettingsLogLevel.Information;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string? statusMessage;

    /// <summary>
    /// Initializes a settings dialog.
    /// </summary>
    /// <param name="settings">The settings service.</param>
    /// <param name="environment">The app-data operations.</param>
    public SettingsViewModel(ISettingsService settings, ISettingsEnvironment environment)
    {
        this.settings = settings;
        this.environment = environment;
        LoadFrom(settings.Current);
    }

    /// <summary>
    /// Gets the available themes.
    /// </summary>
    public IReadOnlyList<ThemeSelection> Themes { get; } = [ThemeSelection.System, ThemeSelection.Light, ThemeSelection.Dark];

    /// <summary>
    /// Gets the available densities.
    /// </summary>
    public IReadOnlyList<Density> Densities { get; } = [Density.Comfortable, Density.Compact];

    /// <summary>
    /// Gets the available log levels.
    /// </summary>
    public IReadOnlyList<SettingsLogLevel> LogLevels { get; } =
    [
        SettingsLogLevel.Trace,
        SettingsLogLevel.Debug,
        SettingsLogLevel.Information,
        SettingsLogLevel.Warning,
        SettingsLogLevel.Error,
        SettingsLogLevel.Fatal,
    ];

    /// <summary>
    /// Gets the accent choices, with <c>default</c> meaning the Fluent accent.
    /// </summary>
    public IReadOnlyList<string> Accents { get; } = ["default", .. AccentPalette.Keys];

    /// <summary>
    /// Gets the editable recent-file list.
    /// </summary>
    public ObservableCollection<RecentFile> RecentFiles { get; } = [];

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public string SettingsPath => environment.SettingsPath;

    /// <summary>
    /// Gets the application-data directory.
    /// </summary>
    public string DataDirectory => environment.DataDirectory;

    /// <summary>
    /// Opens the dialog with the current settings.
    /// </summary>
    public void Open()
    {
        LoadFrom(settings.Current);
        ErrorMessage = null;
        StatusMessage = settings.LoadResult.IsFatal
            ? "The existing settings file was rejected and is preserved; saving will report the same warning."
            : null;
        IsOpen = true;
    }

    /// <summary>
    /// Loads a settings value into the editable state.
    /// </summary>
    /// <param name="value">The settings to load.</param>
    public void LoadFrom(AppSettings value)
    {
        Theme = value.Theme;
        SelectedAccent = value.Accent ?? "default";
        Density = value.Density;
        FontScale = value.FontScale;
        HighContrast = value.HighContrast;
        ReduceMotion = value.ReduceMotion;
        ConfirmOnExternalChange = value.Ui.ConfirmOnExternalChange;
        ShowWelcomeOnStart = value.Ui.ShowWelcomeOnStart;
        LogLevel = value.Logging.Level;
        RecentFiles.Clear();
        foreach (var recent in value.RecentFiles)
        {
            RecentFiles.Add(recent);
        }
    }

    /// <summary>
    /// Saves the edited settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;
        if (FontScale < SettingsDefaults.MinFontScale || FontScale > SettingsDefaults.MaxFontScale)
        {
            ErrorMessage = "The font scale must be between 0.85 and 1.5.";
            return;
        }

        var updated = settings.Current with
        {
            Theme = Theme,
            Accent = string.Equals(SelectedAccent, "default", StringComparison.Ordinal) ? null : SelectedAccent,
            Density = Density,
            FontScale = FontScale,
            HighContrast = HighContrast,
            ReduceMotion = ReduceMotion,
            Logging = new LoggingSettings { Level = LogLevel },
            Ui = new UiSettings
            {
                ConfirmOnExternalChange = ConfirmOnExternalChange,
                ShowWelcomeOnStart = ShowWelcomeOnStart,
            },
            RecentFiles = [.. RecentFiles],
        };
        var result = await settings.UpdateAsync(updated, cancellationToken);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message ?? "The settings could not be saved.";
            return;
        }

        StatusMessage = "Settings saved.";
        IsOpen = false;
    }

    /// <summary>
    /// Restores the dialog defaults.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        LoadFrom(AppSettings.Default);
        StatusMessage = "Defaults restored; save to apply.";
        ErrorMessage = null;
    }

    /// <summary>
    /// Closes the dialog without saving.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        ErrorMessage = null;
        StatusMessage = null;
    }

    /// <summary>
    /// Opens the configuration folder.
    /// </summary>
    [RelayCommand]
    private Task OpenConfigFolderAsync(CancellationToken cancellationToken) =>
        environment.OpenConfigFolderAsync(cancellationToken);

    /// <summary>
    /// Deletes closed logs and cache files.
    /// </summary>
    [RelayCommand]
    private async Task ClearLogsAsync(CancellationToken cancellationToken)
    {
        var freed = await environment.ClearLogsAndCacheAsync(cancellationToken);
        StatusMessage = $"Freed {freed} bytes.";
    }

    /// <summary>
    /// Removes one recent file.
    /// </summary>
    /// <param name="file">The file to remove.</param>
    [RelayCommand]
    private void RemoveRecent(RecentFile? file)
    {
        if (file is not null)
        {
            RecentFiles.Remove(file);
        }
    }

    /// <summary>
    /// Clears the recent files.
    /// </summary>
    [RelayCommand]
    private void ClearRecent() => RecentFiles.Clear();
}
