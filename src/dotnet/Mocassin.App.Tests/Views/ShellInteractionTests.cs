// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Notifications;
using Mocassin.App.Views.Settings;
using Mocassin.App.Views.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Headless tests for overlay focus, Escape and Enter routing, layer modality, and list activation.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ShellInteractionTests
{
    [Fact]
    public Task ThePaletteFocusesSearchAndEscapeRestoresThePreviousFocus() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var filter = FocusConsoleFilter(window);
                    shell.ShowPaletteCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    var search = Find<TextBox>(window, "SearchBox");
                    Assert.Same(search, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);

                    Assert.False(shell.IsPaletteOpen);
                    Assert.Same(filter, window.FocusManager?.GetFocusedElement());
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ThePaletteClosesOnEscapeFromTheResultsList() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.ShowPaletteCommand.Execute(null);
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                    HeadlessSession.Pump(window);
                    var focused = Assert.IsAssignableFrom<Control>(window.FocusManager?.GetFocusedElement());
                    Assert.True(focused is ListBox or ListBoxItem);

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);

                    Assert.False(shell.IsPaletteOpen);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task PaletteEnterKeepsAnEmptyPaletteOpenAndExecutesOneResult() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.ShowPaletteCommand.Execute(null);
                    shell.Palette.SearchText = "zzz-no-command";
                    HeadlessSession.Pump(window);
                    Assert.Empty(shell.Palette.Results);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);

                    Assert.True(shell.IsPaletteOpen);
                    Assert.True(shell.Palette.ExecuteSelectedCommand.CanExecute(null));

                    shell.Palette.SearchText = "toggle projects";
                    HeadlessSession.Pump(window);
                    Assert.NotEmpty(shell.Palette.Results);
                    var projectsVisible = shell.Regions.IsProjectsVisible;

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);

                    Assert.False(shell.IsPaletteOpen);
                    Assert.NotEqual(projectsVisible, shell.Regions.IsProjectsVisible);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheCheatSheetFocusesCloseAndClosesOnEscapeAndEnter() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var filter = FocusConsoleFilter(window);

                    shell.ShowShortcutsCommand.Execute(null);
                    HeadlessSession.Pump(window);
                    var close = Find<Button>(window, "CloseButton");
                    Assert.Same(close, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);
                    Assert.False(shell.IsCheatSheetOpen);
                    Assert.Same(filter, window.FocusManager?.GetFocusedElement());

                    shell.ShowShortcutsCommand.Execute(null);
                    HeadlessSession.Pump(window);
                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.False(shell.IsCheatSheetOpen);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task SettingsEscapeDiscardsUnappliedEditsAndRestoresFocus() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var filter = FocusConsoleFilter(window);
                    shell.Settings.Open();
                    HeadlessSession.Pump(window);

                    var theme = Find<ComboBox>(window, "ThemeBox");
                    Assert.Same(theme, window.FocusManager?.GetFocusedElement());
                    shell.Settings.Theme = ThemeSelection.Dark;

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);

                    Assert.False(shell.Settings.IsOpen);
                    Assert.Equal(ThemeSelection.System, host.Services.GetRequiredService<ISettingsService>().Current.Theme);
                    Assert.Same(filter, window.FocusManager?.GetFocusedElement());
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task SettingsEnterSavesAndCloses() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.Settings.Open();
                    HeadlessSession.Pump(window);
                    var interval = window.GetVisualDescendants()
                        .OfType<TextBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Autosave interval minutes");
                    interval.Focus();
                    shell.Settings.AutosaveIntervalMinutes = 12;
                    HeadlessSession.Pump(window);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    if (shell.Settings.SaveCommand is IAsyncRelayCommand { ExecutionTask: { } save })
                    {
                        await save;
                    }

                    HeadlessSession.Pump(window);
                    Assert.False(shell.Settings.IsOpen);
                    Assert.Equal(12, host.Services.GetRequiredService<ISettingsService>().Current.Autosave.IntervalMinutes);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ConfirmationFocusesCancelAndRequiresTabEnterToConfirm() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var dialogs = host.Services.GetRequiredService<IDialogService>();
                    var first = dialogs.ConfirmAsync(new ConfirmDialogRequest("Delete", "Delete the item?"));
                    HeadlessSession.Pump(window);
                    var cancel = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsCancel);
                    Assert.Same(cancel, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.False(await first);

                    var second = dialogs.ConfirmAsync(new ConfirmDialogRequest("Delete", "Delete the item?"));
                    HeadlessSession.Pump(window);
                    window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, null);
                    HeadlessSession.Pump(window);
                    var confirm = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
                    Assert.Same(confirm, window.FocusManager?.GetFocusedElement());

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.True(await second);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task PromptAndChoiceDialogsFocusTheirDeclaredTargets() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var dialogs = host.Services.GetRequiredService<IDialogService>();
                    var prompt = dialogs.PromptAsync(new TextPromptRequest("Name", "Project name", "Initial"));
                    HeadlessSession.Pump(window);
                    var textBox = window.GetVisualDescendants().OfType<TextBox>()
                        .Single(control => control.Text == "Initial");
                    Assert.Same(textBox, window.FocusManager?.GetFocusedElement());
                    textBox.Text = "Typed";

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("Typed", await prompt);

                    var choice = dialogs.ChooseAsync(
                        new ChoiceDialogRequest(
                            "Template",
                            "Choose",
                            [new DialogChoice("blank", "Blank", IsDefault: true), new DialogChoice("ceria", "Ceria")]));
                    HeadlessSession.Pump(window);
                    var focused = Assert.IsAssignableFrom<Control>(window.FocusManager?.GetFocusedElement());
                    var list = focused as ListBox
                        ?? focused.GetVisualAncestors().OfType<ListBox>().First();
                    Assert.Equal(2, list.ItemCount);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.Equal("blank", (await choice).ChoiceId);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task TheProgressDialogFocusesItsContainerAndIgnoresEnterAndEscape() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    using var progress = host.Services.GetRequiredService<IDialogService>()
                        .ShowProgress(new ProgressDialogRequest("Working", "Working", CanCancel: true));
                    HeadlessSession.Pump(window);

                    var content = Find<ContentControl>(window, "DialogContent");
                    Assert.Same(content, window.FocusManager?.GetFocusedElement());
                    Assert.IsType<ProgressDialogViewModel>(content.Content);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.False(progress.CancellationToken.IsCancellationRequested);

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);
                    Assert.True(shell.Progress.IsActive);
                    Assert.NotNull(content.Content);

                    ((ProgressDialogViewModel)content.Content!).RequestCancelCommand.Execute(null);
                    Assert.True(progress.CancellationToken.IsCancellationRequested);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task AnOpenSettingsLayerDisablesLowerLayersAndCatalogGestures() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var shellRoot = Find<ShellView>(window, "ShellRoot");
                    var notifications = Find<NotificationHostView>(window, "NotificationOverlay");
                    var settingsLayer = Find<Grid>(window, "SettingsLayer");

                    shell.Settings.Open();
                    HeadlessSession.Pump(window);

                    Assert.False(shellRoot.IsEnabled);
                    Assert.False(notifications.IsEnabled);
                    Assert.True(settingsLayer.IsEnabled);
                    Assert.True(window.GetVisualDescendants().OfType<SettingsDialogView>().Single().IsVisible);

                    window.KeyPress(Key.F1, RawInputModifiers.None, PhysicalKey.F1, null);
                    window.KeyPress(Key.P, RawInputModifiers.Control | RawInputModifiers.Shift, PhysicalKey.P, "P");
                    HeadlessSession.Pump(window);

                    Assert.False(shell.IsCheatSheetOpen);
                    Assert.False(shell.IsPaletteOpen);

                    window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
                    HeadlessSession.Pump(window);

                    Assert.False(shell.Settings.IsOpen);
                    Assert.True(shellRoot.IsEnabled);
                    Assert.True(notifications.IsEnabled);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ADialogOverSettingsDisablesSettingsAndItsDefaultAction() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    shell.Settings.Open();
                    shell.Settings.Theme = ThemeSelection.Dark;
                    HeadlessSession.Pump(window);
                    var settingsLayer = Find<Grid>(window, "SettingsLayer");
                    Assert.True(settingsLayer.IsEnabled);

                    var confirmation = host.Services.GetRequiredService<IDialogService>()
                        .ConfirmAsync(new ConfirmDialogRequest("Nested", "Shown over settings"));
                    HeadlessSession.Pump(window);

                    Assert.False(settingsLayer.IsEnabled);
                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);
                    Assert.False(await confirmation);
                    Assert.True(shell.Settings.IsOpen);
                    Assert.True(settingsLayer.IsEnabled);
                    Assert.Equal(ThemeSelection.System, host.Services.GetRequiredService<ISettingsService>().Current.Theme);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ProblemsEnterNavigatesTheSelectedDiagnostic() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                using var workspace = new TempWorkspace();
                using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                var window = host.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var session = CreateDiagnosticSession();
                    shell.Workspace.Add(new ProjectWorkspaceEntry(session));
                    var problems = shell.Console.Channels.OfType<ProblemsChannelViewModel>().Single();
                    shell.Console.ActiveChannel = problems;
                    HeadlessSession.Pump(window);

                    var list = window.GetVisualDescendants()
                        .OfType<ListBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Validation entries");
                    var item = Assert.Single(problems.Items);
                    Assert.True(item.CanNavigate);
                    Assert.True(item.NavigateCommand.CanExecute(null));
                    list.SelectedItem = item;
                    HeadlessSession.Pump(window);
                    Assert.Same(item, list.SelectedItem);
                    var row = list.GetVisualDescendants().OfType<ListBoxItem>().First();
                    row.Focus();
                    Assert.True(row.IsFocused);

                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    HeadlessSession.Pump(window);

                    Assert.IsType<ProjectOverviewViewModel>(shell.Documents.ActiveDocument);
                }
                finally
                {
                    window.Close();
                }
            });

    private static TextBox FocusConsoleFilter(ShellWindow window)
    {
        var filter = window.GetVisualDescendants()
            .OfType<TextBox>()
            .Single(control => control.PlaceholderText == "Filter the application console");
        filter.Focus();
        Assert.True(filter.IsFocused);
        return filter;
    }

    private static T Find<T>(ShellWindow window, string name)
        where T : Control =>
        window.GetVisualDescendants()
            .OfType<T>()
            .Single(control => control.Name == name);

    private static ProjectSession CreateDiagnosticSession()
    {
        var time = new ManualTimeProvider();
        var project = ProjectTemplateFactory.CreateProject(
            ProjectTemplateKind.Blank,
            "Diagnostics",
            time.GetUtcNow());
        var diagnostics = ImmutableArray.Create(
            new Diagnostic(
                "PROJ001",
                DiagnosticSeverity.Error,
                "Loaded project error.",
                new ModelPath("project", project.Metadata.ProjectId, "name")));
        return ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, diagnostics),
            "/tmp/shell-interaction-diagnostics.mocprj",
            null,
            isReadOnly: false,
            lockPath: null,
            new ProjectValidationPipeline(),
            time);
    }
}
