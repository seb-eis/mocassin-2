// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.Views.Shell;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Drives the real in-shell dialog overlay headlessly.
/// </summary>
public sealed class DialogOverlayTests
{
    [Fact]
    public Task AConfirmationRoundTripsThroughTheRealOverlay() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var dialogs = host.Services.GetRequiredService<IDialogService>();
                    var confirmation = dialogs.ConfirmAsync(new ConfirmDialogRequest("Delete", "Delete the item?"));

                    var confirmButton = window.GetVisualDescendants()
                        .OfType<Button>()
                        .Single(button => button.IsDefault);
                    Assert.NotNull(confirmButton.Command);
                    confirmButton.Command.Execute(null);

                    Assert.True(await confirmation);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task EscapeCancelsTheDialog() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var dialogs = host.Services.GetRequiredService<IDialogService>();
                    var confirmation = dialogs.ConfirmAsync(new ConfirmDialogRequest("Delete", "Delete the item?"));

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

                    Assert.False(await confirmation);
                }
                finally
                {
                    window.Close();
                }
            });
}
