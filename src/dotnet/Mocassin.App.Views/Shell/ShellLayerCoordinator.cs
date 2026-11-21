// SPDX-License-Identifier: MIT

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Settings.Dialog;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Commands;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Notifications;
using Mocassin.App.Views.Platform;
using Mocassin.App.Views.Settings;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Owns the shell's interactive-layer state: modality, focus capture and restore, the modal command gate, and
/// the window-level Escape route.
/// </summary>
internal sealed class ShellLayerCoordinator
{
    private enum Layer
    {
        None,
        Palette,
        CheatSheet,
        Settings,
        Dialog,
    }

    private readonly ShellWindow window;
    private readonly ShellView shellRoot;
    private readonly Grid paletteLayer;
    private readonly CommandPaletteView paletteView;
    private readonly Grid cheatSheetLayer;
    private readonly ShortcutCheatSheetView cheatSheetView;
    private readonly Grid settingsLayer;
    private readonly SettingsDialogView settingsView;
    private readonly DialogHost dialogHost;
    private readonly NotificationHostView notificationHost;
    private readonly ShellViewModel shell;
    private readonly HashSet<(Key Key, KeyModifiers Modifiers)> catalogGestures = [];
    private readonly KeyBinding[] catalogBindings;
    private IInputElement? capturedFocus;
    private Layer activeLayer;
    private bool bindingsAttached = true;
    private bool updating;

    /// <summary>
    /// Initializes the coordinator and subscribes to every layer source.
    /// </summary>
    /// <param name="window">The shell window.</param>
    /// <param name="shellRoot">The shell workspace root.</param>
    /// <param name="paletteLayer">The palette overlay root.</param>
    /// <param name="paletteView">The palette view.</param>
    /// <param name="cheatSheetLayer">The cheat-sheet overlay root.</param>
    /// <param name="cheatSheetView">The cheat-sheet view.</param>
    /// <param name="settingsLayer">The settings overlay root.</param>
    /// <param name="settingsView">The settings view.</param>
    /// <param name="dialogHost">The dialog overlay.</param>
    /// <param name="notificationHost">The notification host.</param>
    /// <param name="shell">The shell view model.</param>
    /// <param name="commands">The command catalog.</param>
    /// <param name="platform">The gesture platform.</param>
    public ShellLayerCoordinator(
        ShellWindow window,
        ShellView shellRoot,
        Grid paletteLayer,
        CommandPaletteView paletteView,
        Grid cheatSheetLayer,
        ShortcutCheatSheetView cheatSheetView,
        Grid settingsLayer,
        SettingsDialogView settingsView,
        DialogHost dialogHost,
        NotificationHostView notificationHost,
        ShellViewModel shell,
        ICommandCatalog commands,
        GesturePlatform platform)
    {
        this.window = window;
        this.shellRoot = shellRoot;
        this.paletteLayer = paletteLayer;
        this.paletteView = paletteView;
        this.cheatSheetLayer = cheatSheetLayer;
        this.cheatSheetView = cheatSheetView;
        this.settingsLayer = settingsLayer;
        this.settingsView = settingsView;
        this.dialogHost = dialogHost;
        this.notificationHost = notificationHost;
        this.shell = shell;

        foreach (var command in commands.Commands)
        {
            if (command.Gesture is { } gesture)
            {
                var keyGesture = GestureTranslator.ToKeyGesture(gesture, platform);
                catalogGestures.Add((keyGesture.Key, keyGesture.KeyModifiers));
            }
        }

        catalogBindings = [.. window.KeyBindings];
        shell.PropertyChanged += OnShellPropertyChanged;
        shell.Settings.PropertyChanged += OnSettingsPropertyChanged;
        dialogHost.DialogOpened += OnDialogStateChanged;
        dialogHost.DialogClosed += OnDialogStateChanged;
        ApplyModality(Layer.None);
    }

    /// <summary>
    /// Gets a value indicating whether any layer is open.
    /// </summary>
    internal bool IsLayerOpen => activeLayer != Layer.None;

    /// <summary>
    /// Returns <see langword="true"/> when the open layer must swallow a catalog gesture so a shortcut cannot
    /// open a second layer.
    /// </summary>
    /// <param name="e">The key event.</param>
    /// <returns><see langword="true"/> when the gesture is inert.</returns>
    internal bool BlocksCatalogGesture(KeyEventArgs e) =>
        IsLayerOpen && catalogGestures.Contains((e.Key, e.KeyModifiers));

    /// <summary>
    /// Routes Escape through the contract priority: dialog, settings, palette, cheat sheet.
    /// </summary>
    /// <returns><see langword="true"/> when a layer consumed the key.</returns>
    internal bool HandleEscape()
    {
        if (dialogHost.IsDialogOpen)
        {
            return dialogHost.TryCancel();
        }

        if (shell.Settings.IsOpen)
        {
            shell.Settings.CloseCommand.Execute(null);
            return true;
        }

        if (shell.IsPaletteOpen)
        {
            shell.ClosePaletteCommand.Execute(null);
            return true;
        }

        if (shell.IsCheatSheetOpen)
        {
            shell.CloseShortcutsCommand.Execute(null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detaches from the layer sources when the window closes.
    /// </summary>
    public void Detach()
    {
        shell.PropertyChanged -= OnShellPropertyChanged;
        shell.Settings.PropertyChanged -= OnSettingsPropertyChanged;
        dialogHost.DialogOpened -= OnDialogStateChanged;
        dialogHost.DialogClosed -= OnDialogStateChanged;
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.IsPaletteOpen) or nameof(ShellViewModel.IsCheatSheetOpen))
        {
            HandleStateChange();
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.IsOpen))
        {
            HandleStateChange();
        }
    }

    private void OnDialogStateChanged(object? sender, EventArgs e) => HandleStateChange();

    private void HandleStateChange()
    {
        if (updating)
        {
            return;
        }

        var next = ComputeLayer();
        if (next == activeLayer)
        {
            return;
        }

        if (next == Layer.Settings)
        {
            updating = true;
            shell.IsPaletteOpen = false;
            shell.IsCheatSheetOpen = false;
            updating = false;
            next = ComputeLayer();
        }
        else if (next == Layer.Palette && shell.IsCheatSheetOpen)
        {
            updating = true;
            shell.IsCheatSheetOpen = false;
            updating = false;
            next = ComputeLayer();
        }

        var previous = activeLayer;
        if (previous == Layer.None && next is Layer.Palette or Layer.CheatSheet or Layer.Settings)
        {
            capturedFocus = window.FocusManager?.GetFocusedElement();
        }

        activeLayer = next;
        ApplyModality(next);

        if (next is Layer.Palette or Layer.CheatSheet or Layer.Settings)
        {
            PostInitialFocus(next);
        }
        else if (next == Layer.None && capturedFocus is not null)
        {
            PostFocusRestore();
        }
    }

    private Layer ComputeLayer()
    {
        if (dialogHost.IsDialogOpen)
        {
            return Layer.Dialog;
        }

        if (shell.Settings.IsOpen)
        {
            return Layer.Settings;
        }

        if (shell.IsPaletteOpen)
        {
            return Layer.Palette;
        }

        return shell.IsCheatSheetOpen ? Layer.CheatSheet : Layer.None;
    }

    private void ApplyModality(Layer top)
    {
        SetEnabled(shellRoot, top == Layer.None);
        SetEnabled(notificationHost, top == Layer.None);
        SetEnabled(paletteLayer, top is Layer.None or Layer.Palette);
        SetEnabled(cheatSheetLayer, top is Layer.None or Layer.CheatSheet);
        SetEnabled(settingsLayer, top is Layer.None or Layer.Settings);
        SetCatalogGesturesEnabled(top == Layer.None);
    }

    private void SetCatalogGesturesEnabled(bool enabled)
    {
        if (enabled == bindingsAttached)
        {
            return;
        }

        if (enabled)
        {
            foreach (var binding in catalogBindings)
            {
                window.KeyBindings.Add(binding);
            }

            bindingsAttached = true;
        }
        else
        {
            window.KeyBindings.Clear();
            bindingsAttached = false;
        }
    }

    private static void SetEnabled(Control control, bool enabled)
    {
        if (control.IsEnabled != enabled)
        {
            control.IsEnabled = enabled;
        }
    }

    private void PostInitialFocus(Layer layer)
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (ComputeLayer() != layer)
                {
                    return;
                }

                switch (layer)
                {
                    case Layer.Palette:
                        paletteView.FocusSearch();
                        break;
                    case Layer.CheatSheet:
                        cheatSheetView.FocusInitial();
                        break;
                    case Layer.Settings:
                        settingsView.FocusInitial();
                        break;
                }
            },
            DispatcherPriority.Loaded);
    }

    private void PostFocusRestore()
    {
        var target = capturedFocus;
        capturedFocus = null;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (ComputeLayer() == Layer.None)
                {
                    ShellFocusRestore.TryFocus(window, target);
                }
            },
            DispatcherPriority.Loaded);
    }
}
