// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Commands;
using Mocassin.App.Views.Consoles;
using Mocassin.App.Views.Dialogs;
using Mocassin.App.Views.Documents.Project;
using Mocassin.App.Views.Settings;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Measures the realized scrollbar edge against the realized content edge for every application scroll surface.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ScrollGutterTests
{
    private static readonly Configuration[] configurations =
    [
        new("1000x700", new AppSettings { Theme = ThemeSelection.Light }, 1000, 700),
        new("900x600", new AppSettings { Theme = ThemeSelection.Light }, 900, 600),
        new(
            "1.5x compact",
            new AppSettings { Theme = ThemeSelection.Light, FontScale = 1.5, Density = Density.Compact },
            1000,
            700),
    ];

    private static readonly Thickness gutterRight = new(0, 0, 20, 0);
    private static readonly Thickness gutterBottom = new(0, 0, 0, 20);
    private static readonly Thickness gutterBoth = new(0, 0, 20, 20);

    [Fact]
    public Task EveryApplicationScrollSurfaceKeepsItsContentClearOfTheScrollbar() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                foreach (var configuration in configurations)
                {
                    await AssertConfigurationAsync(configuration);
                }
            });

    private static async Task AssertConfigurationAsync(Configuration configuration)
    {
        using var fixture = new ShellFixture(configuration.Settings, configuration.Width, configuration.Height);
        var shell = fixture.Shell;
        var window = fixture.Window;

        var projects = new List<ProjectWorkspaceEntry>
        {
            await ProjectFixtures.LoadCeriaAsync(),
            await LoadProjectAsync("samples", "barium-zirconate", "barium-zirconate.mocprj"),
            await LoadProjectAsync("samples", "step-14-ceria-10x10x10", "step-14-ceria-10x10x10.mocprj"),
            await LoadProjectAsync("samples", "blank", "blank.mocprj"),
        };
        foreach (var project in projects)
        {
            shell.Workspace.Add(project);
        }

        HeadlessSession.Pump(window);

        // Project overview: no class; its 20 px content margin meets the gutter.
        await shell.Navigation.NavigateToAsync(
            new ModelPath("project", projects[0].Id, "overview"),
            NavigationIntent.Activate);
        HeadlessSession.Pump(window);
        var overview = window.GetVisualDescendants().OfType<ProjectOverviewView>().Single();
        var overviewScroll = overview.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(default, overviewScroll.Padding);
        AssertVerticalGutter(window, overviewScroll, Content(overviewScroll), "project overview", expectBar: true);

        // Project JSON: the scoped TextBox class reaches the internal scroll viewer.
        await shell.Navigation.NavigateToAsync(
            new ModelPath("project", projects[0].Id, "json"),
            NavigationIntent.Activate);
        HeadlessSession.Pump(window);
        var json = window.GetVisualDescendants().OfType<ProjectJsonView>().Single();
        var jsonBox = json.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Contains("scroll-gutter-both", jsonBox.Classes);
        var jsonScroll = jsonBox.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "PART_ScrollViewer");
        Assert.Equal(gutterBoth, jsonScroll.Padding);
        Assert.Equal(
            gutterBoth,
            jsonScroll.GetVisualDescendants().OfType<ScrollContentPresenter>().Single().Padding);
        Assert.True(jsonScroll.Extent.Width > jsonScroll.Viewport.Width, $"{configuration.Name}: JSON has no horizontal extent.");
        Assert.True(jsonScroll.Extent.Height > jsonScroll.Viewport.Height, $"{configuration.Name}: JSON has no vertical extent.");

        // Application console: a populated list, so the bar is real.
        var channel = shell.Console.Channels[0];
        for (var index = 0; index < 60; index++)
        {
            channel.Add(
                new ConsoleEntryViewModel(
                    DateTimeOffset.UnixEpoch.AddSeconds(index),
                    ConsoleSeverity.Information,
                    "gutter",
                    $"Application console entry {index} with a message long enough to fill the row."));
        }

        HeadlessSession.Pump(window);
        var consoleList = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => AutomationProperties.GetName(list) == "Application console entries");
        AssertListGutter(window, consoleList, configuration.Name, "application console", gutterRight, expectBar: true);

        // Validation: switch the channel and populate both the filter row and the entries list.
        var problems = Assert.IsType<ProblemsChannelViewModel>(
            shell.Console.Channels.Single(item => item.Id == "problems"));
        shell.Console.ActiveChannel = problems;
        var launcher = fixture.Host.Services.GetRequiredService<IShellLauncher>();
        for (var index = 0; index < 40; index++)
        {
            problems.Items.Add(
                new DiagnosticItemViewModel(
                    new Diagnostic("ENM018", DiagnosticSeverity.Error, $"Validation entry {index} with a long message."),
                    "validation",
                    shell.Navigation,
                    launcher));
        }

        HeadlessSession.Pump(window);
        var problemsView = window.GetVisualDescendants().OfType<ProblemsView>().Single();
        var filterScroll = problemsView.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(gutterBottom, filterScroll.Padding);
        AssertHorizontalGutter(window, filterScroll, Content(filterScroll), configuration.Name, expectBar: true);

        var entries = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => AutomationProperties.GetName(list) == "Validation entries");
        AssertListGutter(window, entries, configuration.Name, "validation entries", gutterRight, expectBar: true);

        // Palette: must be genuinely scrolled before measuring.
        shell.ShowPaletteCommand.Execute(null);
        HeadlessSession.Pump(window);
        var palette = Assert.Single(window.GetVisualDescendants().OfType<CommandPaletteView>());
        var paletteList = palette.GetVisualDescendants().OfType<ListBox>()
            .Single(list => AutomationProperties.GetName(list) == "Filtered commands");
        var paletteScroll = ScrollViewerOf(paletteList);
        Assert.True(
            paletteScroll.Extent.Height > paletteScroll.Viewport.Height,
            $"{configuration.Name}: the palette is not scrollable ({paletteScroll.Extent.Height} <= {paletteScroll.Viewport.Height}).");
        paletteScroll.Offset = new Vector(0, paletteScroll.Extent.Height - paletteScroll.Viewport.Height);
        HeadlessSession.Pump(window);
        AssertListGutter(window, paletteList, configuration.Name, "palette", gutterRight, expectBar: true);
        AssertGestureTextStaysLeftOfTheBar(window, paletteList, configuration.Name);
        shell.Palette.CancelCommand.Execute(null);
        HeadlessSession.Pump(window);

        // Cheat sheet: scrolled to the end before measuring.
        shell.ShowShortcutsCommand.Execute(null);
        HeadlessSession.Pump(window);
        var cheatSheet = Assert.Single(window.GetVisualDescendants().OfType<ShortcutCheatSheetView>());
        var cheatScroll = cheatSheet.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(gutterRight, cheatScroll.Padding);
        Assert.True(
            cheatScroll.Extent.Height > cheatScroll.Viewport.Height,
            $"{configuration.Name}: the cheat sheet is not scrollable ({cheatScroll.Extent.Height} <= {cheatScroll.Viewport.Height}).");
        cheatScroll.Offset = new Vector(0, cheatScroll.Extent.Height - cheatScroll.Viewport.Height);
        HeadlessSession.Pump(window);
        AssertVerticalGutter(window, cheatScroll, Content(cheatScroll), $"{configuration.Name}: cheat sheet", expectBar: true);
        shell.Shortcuts.CloseCommand.Execute(null);
        HeadlessSession.Pump(window);

        // Settings.
        shell.Settings.Open();
        HeadlessSession.Pump(window);
        var settings = Assert.Single(window.GetVisualDescendants().OfType<SettingsDialogView>());
        var settingsScroll = settings.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal(gutterRight, settingsScroll.Padding);
        AssertVerticalGutter(
            window,
            settingsScroll,
            Content(settingsScroll),
            $"{configuration.Name}: settings",
            expectBar: settingsScroll.Extent.Height > settingsScroll.Viewport.Height);

        // ComboBox drop-down: the targeted style reaches the popup scroll viewer.
        var logLevel = settings.GetVisualDescendants().OfType<ComboBox>()
            .Single(box => AutomationProperties.GetName(box) == "Log level");
        logLevel.IsDropDownOpen = true;
        HeadlessSession.Pump(window);
        var popup = logLevel.GetVisualDescendants().OfType<Popup>().Single(candidate => candidate.Name == "PART_Popup");
        Assert.NotNull(popup.Child);
        var popupScroll = popup.Child!.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(gutterRight, popupScroll.Padding);
        Assert.Equal(
            gutterRight,
            popupScroll.GetVisualDescendants().OfType<ScrollContentPresenter>().Single().Padding);
        logLevel.IsDropDownOpen = false;
        HeadlessSession.Pump(window);
        shell.Settings.CloseCommand.Execute(null);
        HeadlessSession.Pump(window);

        // Projects list and explorer tree.
        var projectsList = window.GetVisualDescendants().OfType<ListBox>()
            .Single(list => AutomationProperties.GetName(list) == "Open projects");
        AssertListGutter(window, projectsList, configuration.Name, "open projects", gutterRight, expectBar: true);

        var root = new ExplorerNode(
            "gutter.root",
            "Gutter root",
            Children: Enumerable.Range(0, 40)
                .Select(index => new ExplorerNode($"gutter.{index}", $"Child {index}"))
                .ToArray());
        shell.Explorer.Nodes.Add(root);
        HeadlessSession.Pump(window);
        var tree = window.GetVisualDescendants().OfType<TreeView>()
            .Single(view => AutomationProperties.GetName(view) == "Project explorer");
        var treeItem = tree.GetVisualDescendants().OfType<TreeViewItem>()
            .Single(item => ReferenceEquals(item.DataContext, root));
        treeItem.IsExpanded = true;
        HeadlessSession.Pump(window);
        AssertTreeGutter(window, tree, configuration.Name, expectBar: true);

        // Choice dialog: the scoped class applies to the realized list.
        var dialogs = fixture.Host.Services.GetRequiredService<IDialogService>();
        var choiceTask = dialogs.ChooseAsync(
            new ChoiceDialogRequest(
                "Choose",
                "Pick one",
                Enumerable.Range(0, 30)
                    .Select(index => new DialogChoice($"choice.{index}", $"Choice {index}"))
                    .ToList()));
        HeadlessSession.Pump(window);
        var choice = Assert.Single(window.GetVisualDescendants().OfType<ChoiceDialogView>());
        var choiceList = choice.GetVisualDescendants().OfType<ListBox>().Single();
        Assert.Contains("scroll-gutter-right", choiceList.Classes);
        Assert.Equal(gutterRight, choiceList.Padding);
        var choiceScroll = ScrollViewerOf(choiceList);
        if (choiceScroll.Extent.Height > choiceScroll.Viewport.Height)
        {
            AssertVerticalGutter(window, choiceScroll, ItemsOf(choiceScroll), $"{configuration.Name}: choice dialog", expectBar: true);
        }

        Assert.IsType<ChoiceDialogViewModel>(choice.DataContext).CancelCommand.Execute(null);
        HeadlessSession.Pump(window);
        await choiceTask;

        // Crash details: the expander must be open for the ScrollViewer to realize.
        _ = dialogs.ShowCrashAsync(
            new CrashDialogRequest(
                "The application crashed.",
                null,
                string.Join('\n', Enumerable.Range(0, 120).Select(index => $"Crash detail line {index}"))));
        HeadlessSession.Pump(window);
        var crash = Assert.Single(window.GetVisualDescendants().OfType<CrashDialogView>());
        var expander = crash.GetVisualDescendants().OfType<Expander>().Single();
        expander.IsExpanded = true;
        HeadlessSession.Pump(window);
        var crashScroll = crash.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(gutterRight, crashScroll.Padding);
        AssertVerticalGutter(window, crashScroll, Content(crashScroll), $"{configuration.Name}: crash details", expectBar: true);
    }

    private static void AssertListGutter(
        ShellWindow window,
        ListBox list,
        string configuration,
        string name,
        Thickness gutter,
        bool expectBar)
    {
        Assert.Equal(gutter, list.Padding);
        var scroll = ScrollViewerOf(list);
        AssertVerticalGutter(window, scroll, ItemsOf(scroll), $"{configuration}: {name}", expectBar);
    }

    private static void AssertTreeGutter(ShellWindow window, TreeView tree, string configuration, bool expectBar)
    {
        Assert.Equal(gutterBoth, tree.Padding);
        var scroll = tree.GetVisualDescendants().OfType<ScrollViewer>().Single();
        AssertVerticalGutter(window, scroll, ItemsOf(scroll), $"{configuration}: explorer tree", expectBar);
    }

    private static ScrollViewer ScrollViewerOf(ListBox list) =>
        list.GetVisualDescendants().OfType<ScrollViewer>()
            .Single(viewer => viewer.Name == "PART_ScrollViewer");

    private static ItemsPresenter ItemsOf(ScrollViewer scroll) =>
        scroll.GetVisualDescendants().OfType<ItemsPresenter>()
            .Single(presenter => presenter.Name == "PART_ItemsPresenter"
                && ReferenceEquals(presenter.TemplatedParent, scroll.TemplatedParent));

    private static Control Content(ScrollViewer scroll) =>
        scroll.Content as Control
        ?? throw new InvalidOperationException($"{scroll} has no realized content.");

    private static void AssertVerticalGutter(
        ShellWindow window,
        ScrollViewer scroll,
        Control content,
        string name,
        bool expectBar)
    {
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>()
            .Single(candidate => candidate.Name == "PART_VerticalScrollBar" && ReferenceEquals(candidate.TemplatedParent, scroll));
        var visible = bar.IsVisible && bar.Bounds.Width > 0.5;
        Assert.Equal(expectBar, visible);
        if (!visible)
        {
            return;
        }

        var barLeft = Translate(bar, new Point(0, 0), window).X;
        var contentRight = Translate(content, new Point(content.Bounds.Width, 0), window).X;
        Assert.True(
            barLeft - contentRight >= 3,
            $"{name}: content right {contentRight:F1} is not 3 px clear of the scrollbar left {barLeft:F1}.");
    }

    private static void AssertHorizontalGutter(
        ShellWindow window,
        ScrollViewer scroll,
        Control content,
        string configuration,
        bool expectBar)
    {
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>()
            .Single(candidate => candidate.Name == "PART_HorizontalScrollBar" && ReferenceEquals(candidate.TemplatedParent, scroll));
        var visible = bar.IsVisible && bar.Bounds.Height > 0.5;
        Assert.Equal(expectBar, visible);
        if (!visible)
        {
            return;
        }

        var barTop = Translate(bar, new Point(0, 0), window).Y;
        var contentBottom = Translate(content, new Point(0, content.Bounds.Height), window).Y;
        Assert.True(
            barTop - contentBottom >= 3,
            $"{configuration}: filter content bottom {contentBottom:F1} is not 3 px clear of the scrollbar top {barTop:F1}.");
    }

    private static void AssertGestureTextStaysLeftOfTheBar(ShellWindow window, ListBox list, string configuration)
    {
        var scroll = ScrollViewerOf(list);
        var bar = scroll.GetVisualDescendants().OfType<ScrollBar>()
            .Single(candidate => candidate.Name == "PART_VerticalScrollBar" && ReferenceEquals(candidate.TemplatedParent, scroll));
        if (!bar.IsVisible)
        {
            return;
        }

        var barLeft = Translate(bar, new Point(0, 0), window).X;
        var gestureTexts = list.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(text => text.Classes.Contains("caption"))
            .Where(text => !string.IsNullOrWhiteSpace(text.Text))
            .ToList();
        Assert.NotEmpty(gestureTexts);
        foreach (var text in gestureTexts)
        {
            var right = Translate(text, new Point(text.Bounds.Width, 0), window).X;
            Assert.True(
                right <= barLeft - 3,
                $"{configuration}: the palette gesture text ends at {right:F1}, under the bar at {barLeft:F1}.");
        }
    }

    private static Point Translate(Avalonia.Visual visual, Point point, Avalonia.Visual relativeTo) =>
        visual.TranslatePoint(point, relativeTo)
        ?? throw new InvalidOperationException("The control is not attached to the window.");

    private static async Task<ProjectWorkspaceEntry> LoadProjectAsync(params string[] relativePath)
    {
        var path = RepositoryPaths.Combine(relativePath);
        var store = new ProjectStore(new ManualTimeProvider(), new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path);
        if (result.Project is null)
        {
            throw new InvalidOperationException($"The fixture {path} did not load: {result.FailureKind}.");
        }

        return new ProjectWorkspaceEntry(
            result.Project,
            path,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            isReadOnly: false);
    }

    private sealed record Configuration(string Name, AppSettings Settings, int Width, int Height);

    private sealed class ShellFixture : IDisposable
    {
        public ShellFixture(AppSettings settings, int width, int height)
        {
            Workspace = new TempWorkspace();
            Host = AppHost.Build(new AppStartupOptions { DataDirectory = Workspace.Path });
            new AvaloniaThemeApplier().Apply(settings);
            Window = Host.Services.GetRequiredService<ShellWindow>();
            Window.Width = width;
            Window.Height = height;
            Window.Show();
            HeadlessSession.Pump(Window);
            Shell = Assert.IsType<ShellViewModel>(Window.DataContext);
        }

        public TempWorkspace Workspace { get; }

        public AppHost Host { get; }

        public ShellWindow Window { get; }

        public ShellViewModel Shell { get; }

        public void Dispose()
        {
            Window.Close();
            Host.Dispose();
            Workspace.Dispose();
        }
    }
}
