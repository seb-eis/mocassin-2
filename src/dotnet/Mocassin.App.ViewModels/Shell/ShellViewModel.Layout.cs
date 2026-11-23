// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Layout capture, application, and persistence for <see cref="ShellViewModel"/>.
/// </summary>
internal sealed partial class ShellViewModel
{
    private bool applyingLayout;
    private Task pendingLayoutPersist = Task.CompletedTask;
    private bool suppressSettingsApply;

    private void OnRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (applyingLayout)
        {
            return;
        }

        if (e.PropertyName is nameof(ShellRegionsViewModel.IsProjectsVisible)
            or nameof(ShellRegionsViewModel.IsInspectorVisible)
            or nameof(ShellRegionsViewModel.IsConsoleVisible)
            or nameof(ShellRegionsViewModel.LeftFraction)
            or nameof(ShellRegionsViewModel.RightFraction)
            or nameof(ShellRegionsViewModel.ConsoleFraction)
            or nameof(ShellRegionsViewModel.EditorFraction))
        {
            ScheduleLayoutPersist();
        }
    }

    /// <summary>
    /// Gets the pending layout-persistence task; tests await it before disposing the data directory.
    /// </summary>
    internal Task PendingLayoutPersist => pendingLayoutPersist;

    private void OnConsolePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!applyingLayout && e.PropertyName is nameof(ConsoleViewModel.ActiveChannel))
        {
            ScheduleLayoutPersist();
        }
    }

    /// <summary>
    /// Restores the refreshed default fractions and every region's visibility through the normal persist path.
    /// Theme, density, recent files, and the active console tab are not reset.
    /// </summary>
    [RelayCommand]
    internal void ResetLayout()
    {
        applyingLayout = true;
        try
        {
            Regions.IsProjectsVisible = true;
            Regions.IsInspectorVisible = true;
            Regions.IsConsoleVisible = true;
            Regions.LeftFraction = SettingsDefaults.DefaultLeftFraction;
            Regions.RightFraction = SettingsDefaults.DefaultRightFraction;
            Regions.ConsoleFraction = SettingsDefaults.DefaultConsoleFraction;
            Regions.EditorFraction = SettingsDefaults.DefaultEditorFraction;
        }
        finally
        {
            applyingLayout = false;
        }

        ScheduleLayoutPersist();
    }

    private void ScheduleLayoutPersist()
    {
        var previous = pendingLayoutPersist;
        pendingLayoutPersist = PersistLayoutAfterAsync(previous, CaptureLayout());
    }

    private LayoutSettings CaptureLayout() => new()
    {
        LeftVisible = Regions.IsProjectsVisible,
        RightVisible = Regions.IsInspectorVisible,
        ConsoleVisible = Regions.IsConsoleVisible,
        LeftFraction = Regions.LeftFraction,
        RightFraction = Regions.RightFraction,
        ConsoleFraction = Regions.ConsoleFraction,
        EditorFraction = Regions.EditorFraction,
        ActiveConsoleChannel = Console.ActiveChannel?.Id ?? SettingsDefaults.DefaultConsoleChannel,
    };

    private async Task PersistLayoutAfterAsync(Task previous, LayoutSettings layout)
    {
        await previous.ConfigureAwait(true);
        try
        {
            await PersistLayoutAsync(layout).ConfigureAwait(true);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                $"The layout could not be saved: {exception.Message}");
        }
    }

    private void ApplySettings(AppSettings value)
    {
        StatusBar.SetAppearance(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{value.Theme} · {value.Density} · {value.FontScale:0.00}×"));
        applyingLayout = true;
        Regions.IsProjectsVisible = value.Layout.LeftVisible;
        Regions.IsInspectorVisible = value.Layout.RightVisible;
        Regions.IsConsoleVisible = value.Layout.ConsoleVisible;
        Regions.LeftFraction = value.Layout.LeftFraction;
        Regions.RightFraction = value.Layout.RightFraction;
        Regions.ConsoleFraction = value.Layout.ConsoleFraction;
        Regions.EditorFraction = value.Layout.EditorFraction;
        Console.ActiveChannel =
            Console.Channels.FirstOrDefault(channel => channel.Id == value.Layout.ActiveConsoleChannel)
            ?? Console.Channels.FirstOrDefault();
        applyingLayout = false;
    }

    private async Task PersistLayoutAsync(LayoutSettings layout)
    {
        AppSettingsSaveResult result;
        suppressSettingsApply = true;
        try
        {
            result = await settings.UpdateAsync(settings.Current with { Layout = layout });
        }
        finally
        {
            suppressSettingsApply = false;
        }

        if (!result.IsSuccess)
        {
            consoleService.Write(
                ConsoleSeverity.Warning,
                "settings",
                result.Message ?? "The layout could not be saved.");
        }
    }
}
