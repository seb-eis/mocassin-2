// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Settings.Dialog;

namespace Mocassin.App.Tests.Settings;

/// <summary>
/// Verifies the settings dialog state and save path.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SettingsViewModelTests
{
    [Fact]
    public async Task SavingUpdatesTheServiceAndClosesTheDialog()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var environment = new RecordingSettingsEnvironment();
        var dialog = new SettingsViewModel(service, environment) { IsOpen = true };
        dialog.Theme = ThemeSelection.Dark;
        dialog.Density = Density.Compact;
        dialog.FontScale = 1.2;
        dialog.HighContrast = true;
        dialog.LogLevel = SettingsLogLevel.Warning;

        await dialog.SaveCommand.ExecuteAsync(null);

        Assert.False(dialog.IsOpen);
        Assert.NotNull(store.Saved);
        Assert.Equal(ThemeSelection.Dark, service.Current.Theme);
        Assert.Equal(Density.Compact, service.Current.Density);
        Assert.Equal(1.2, service.Current.FontScale);
        Assert.True(service.Current.HighContrast);
        Assert.Equal(SettingsLogLevel.Warning, service.Current.Logging.Level);
    }

    [Fact]
    public async Task AutosaveSettingsRoundTripAndAnInvalidIntervalIsRejected()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment())
        {
            IsOpen = true,
            AutosaveEnabled = false,
            AutosaveIntervalMinutes = 12,
        };

        await dialog.SaveCommand.ExecuteAsync(null);

        Assert.False(dialog.IsOpen);
        Assert.False(service.Current.Autosave.Enabled);
        Assert.Equal(12, service.Current.Autosave.IntervalMinutes);

        dialog.IsOpen = true;
        dialog.AutosaveIntervalMinutes = 0;
        await dialog.SaveCommand.ExecuteAsync(null);

        Assert.True(dialog.IsOpen);
        Assert.NotNull(dialog.ErrorMessage);
    }

    [Fact]
    public async Task AnOutOfRangeFontScaleIsRejectedBeforeWriting()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment())
        {
            IsOpen = true,
            FontScale = 2.0,
        };

        await dialog.SaveCommand.ExecuteAsync(null);

        Assert.True(dialog.IsOpen);
        Assert.NotNull(dialog.ErrorMessage);
        Assert.Null(store.Saved);
    }

    [Fact]
    public async Task AFailedSaveKeepsTheDialogOpenAndShowsTheMessage()
    {
        var store = new InMemorySettingsStore { FailSave = true };
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment()) { IsOpen = true };

        await dialog.SaveCommand.ExecuteAsync(null);

        Assert.True(dialog.IsOpen);
        Assert.Contains("simulated", dialog.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheBannerStateFollowsTheMessages()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment())
        {
            IsOpen = true,
            FontScale = 2.0,
        };
        Assert.False(dialog.HasErrorMessage);

        await dialog.SaveCommand.ExecuteAsync(null);
        Assert.True(dialog.HasErrorMessage);

        dialog.CloseCommand.Execute(null);
        Assert.False(dialog.HasErrorMessage);

        dialog.ResetCommand.Execute(null);
        Assert.True(dialog.HasStatusMessage);
    }

    [Fact]
    public void ResetRestoresDefaultsWithoutWriting()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment());
        dialog.Theme = ThemeSelection.Dark;
        dialog.FontScale = 1.4;

        dialog.ResetCommand.Execute(null);

        Assert.Equal(ThemeSelection.System, dialog.Theme);
        Assert.Equal(SettingsDefaults.DefaultFontScale, dialog.FontScale);
        Assert.Null(store.Saved);
    }

    [Fact]
    public void RecentFilesCanBeCleared()
    {
        var settings = new AppSettings
        {
            RecentFiles = [new RecentFile("/tmp/a.mocprj", DateTimeOffset.UnixEpoch)],
        };
        var store = new InMemorySettingsStore
        {
            LoadResult = new AppSettingsLoadResult(settings, [], false, true, null),
        };
        var service = new SettingsService(store, store.LoadResult);
        var dialog = new SettingsViewModel(service, new RecordingSettingsEnvironment());

        Assert.Single(dialog.RecentFiles);
        dialog.ClearRecentCommand.Execute(null);
        Assert.Empty(dialog.RecentFiles);
    }

    [Fact]
    public async Task TheEnvironmentCommandsAreForwarded()
    {
        var store = new InMemorySettingsStore();
        var service = new SettingsService(store, AppSettingsLoadResult.Missing());
        var environment = new RecordingSettingsEnvironment();
        var dialog = new SettingsViewModel(service, environment);

        await dialog.OpenConfigFolderCommand.ExecuteAsync(null);
        await dialog.ClearLogsCommand.ExecuteAsync(null);

        Assert.Equal(1, environment.OpenFolderCalls);
        Assert.Equal(1, environment.ClearCalls);
        Assert.Contains("1234", dialog.StatusMessage, StringComparison.Ordinal);
    }
}
