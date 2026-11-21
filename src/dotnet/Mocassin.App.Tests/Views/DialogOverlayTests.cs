// SPDX-License-Identifier: MIT

using Avalonia.Automation;
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
[Trait("Tier", "PR-fast")]
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
                    HeadlessSession.Pump(window);

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
    public Task PromptAndChoiceDialogsRoundTrip() =>
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
                    var prompt = dialogs.PromptAsync(new TextPromptRequest("Name", "Project name", "Initial"));
                    HeadlessSession.Pump(window);
                    var textBox = window.GetVisualDescendants().OfType<TextBox>()
                        .First(control => control.Text == "Initial");
                    textBox.Text = "Typed";
                    var promptButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    promptButton.Command!.Execute(null);
                    Assert.Equal("Typed", await prompt);

                    var choice = dialogs.ChooseAsync(
                        new ChoiceDialogRequest(
                            "Template",
                            "Choose",
                            [new DialogChoice("blank", "Blank", IsDefault: true), new DialogChoice("ceria", "Ceria")]));
                    HeadlessSession.Pump(window);
                    var choiceButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    choiceButton.Command!.Execute(null);
                    Assert.Equal("blank", (await choice).ChoiceId);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ErrorDialogClosesWithoutThrowing() =>
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
                    var error = dialogs.ShowErrorAsync(
                        new ErrorDialogRequest(
                            "Failure",
                            "Something failed.",
                            [new ErrorDialogDetail("TST001", "Detail")]));
                    HeadlessSession.Pump(window);
                    var close = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    close.Command!.Execute(null);
                    await error;
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task FocusIsRestoredAfterADialog() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();

                try
                {
                    var focusTarget = window.GetVisualDescendants().OfType<TextBox>()
                        .First(control => control.PlaceholderText == "Filter the application console");
                    focusTarget.Focus();
                    Assert.True(focusTarget.IsFocused);

                    var dialogs = host.Services.GetRequiredService<IDialogService>();
                    var confirmation = dialogs.ConfirmAsync(new ConfirmDialogRequest("Focus", "Restore"));
                    HeadlessSession.Pump(window);
                    var confirm = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    confirm.Command!.Execute(null);
                    await confirmation;
                    HeadlessSession.Pump(window);

                    Assert.Same(focusTarget, window.FocusManager?.GetFocusedElement());
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheErrorDialogFocusesCloseAndClosesOnEnterAndEscape() =>
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
                    var first = dialogs.ShowErrorAsync(new ErrorDialogRequest("Failure", "Something failed."));
                    HeadlessSession.Pump(window);
                    var close = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    Assert.Same(close, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    await first;

                    var second = dialogs.ShowErrorAsync(new ErrorDialogRequest("Failure", "Something failed."));
                    HeadlessSession.Pump(window);
                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    await second;
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheDialogHostAnnouncesTheActiveTitleAndFocusesTheDeclaredTarget() =>
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
                    HeadlessSession.Pump(window);

                    var content = window.GetVisualDescendants()
                        .OfType<ContentControl>()
                        .Single(control => control.Name == "DialogContent");
                    Assert.Equal("Delete", AutomationProperties.GetName(content));
                    var cancel = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsCancel);
                    Assert.Same(cancel, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    Assert.False(await confirmation);
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
