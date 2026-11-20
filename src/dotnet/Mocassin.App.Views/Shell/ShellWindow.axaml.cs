// SPDX-License-Identifier: MIT

using Avalonia.Controls;
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
    /// <summary>
    /// Initializes the shell window.
    /// </summary>
    /// <param name="viewModel">The shell view model.</param>
    /// <param name="dialogs">The dialog service whose overlay lives in this window.</param>
    /// <param name="storage">The platform picker service.</param>
    /// <param name="notifications">The notification service shown by the window.</param>
    public ShellWindow(
        ShellViewModel viewModel,
        DialogService dialogs,
        AvaloniaStorageService storage,
        INotificationService notifications)
    {
        InitializeComponent();
        DataContext = viewModel;
        dialogs.Host = DialogOverlay;
        NotificationOverlay.DataContext = notifications;

        Opened += (_, _) => storage.StorageProvider = StorageProvider;
        Closed += (_, _) =>
        {
            storage.StorageProvider = null;
            dialogs.Host = null;
        };
    }
}
