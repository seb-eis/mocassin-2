// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Notifications;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Platform;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Main application window. It attaches the dialog overlay and the picker provider.
/// </summary>
internal sealed partial class ShellWindow : Window
{
    private readonly ShellLayerCoordinator layers;
    private bool closeApproved;

    /// <summary>
    /// Initializes the shell window.
    /// </summary>
    /// <param name="viewModel">The shell view model.</param>
    /// <param name="dialogs">The dialog service whose overlay lives in this window.</param>
    /// <param name="storage">The platform picker service.</param>
    /// <param name="clipboard">The platform clipboard service.</param>
    /// <param name="notifications">The notification service shown by the window.</param>
    /// <param name="commands">The command catalog used for key bindings.</param>
    /// <param name="platform">The platform used to translate gestures.</param>
    public ShellWindow(
        ShellViewModel viewModel,
        DialogService dialogs,
        AvaloniaStorageService storage,
        AvaloniaClipboardService clipboard,
        INotificationService notifications,
        ICommandCatalog commands,
        GesturePlatform platform)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewKeyBindingBinder.Apply(this, commands, platform);
        layers = new ShellLayerCoordinator(
            this,
            ShellRoot,
            PaletteLayer,
            PaletteView,
            CheatSheetLayer,
            CheatSheetView,
            SettingsLayer,
            SettingsView,
            DialogOverlay,
            NotificationOverlay,
            viewModel,
            commands,
            platform);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnEscapeKeyDown, RoutingStrategies.Bubble);
        dialogs.Host = DialogOverlay;
        NotificationOverlay.DataContext = notifications;

        Opened += (_, _) =>
        {
            storage.StorageProvider = StorageProvider;
            clipboard.Clipboard = Clipboard;
            if (DataContext is ShellViewModel shell)
            {
                shell.BeginStartupRestore();
            }
        };
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            storage.StorageProvider = null;
            clipboard.Clipboard = null;
            dialogs.Host = null;
            layers.Detach();
        };
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (closeApproved || DataContext is not ShellViewModel shell)
        {
            return;
        }

        e.Cancel = true;
        try
        {
            if (await shell.RequestCloseAsync())
            {
                await shell.PendingLayoutPersist;
                closeApproved = true;
                Close();
            }
        }
#pragma warning disable CA1031 // A close-handler failure must leave the window open, not crash the process.
        catch (Exception)
#pragma warning restore CA1031
        {
            closeApproved = false;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ShellViewModel shell)
        {
            return;
        }

        if (layers.IsLayerOpen)
        {
            // Catalog gestures are inert while a layer is open; typing, Tab, arrows, Enter, and Escape keep
            // working inside the layer.
            if (layers.BlocksCatalogGesture(e))
            {
                e.Handled = true;
            }

            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Key is < Key.D1 or > Key.D9)
        {
            return;
        }

        var index = e.Key - Key.D1;
        if (shell.Documents.ActivateIndex(index))
        {
            e.Handled = true;
        }
    }

    private void OnEscapeKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Escape && layers.HandleEscape())
        {
            e.Handled = true;
        }
    }
}
