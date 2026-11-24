// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Icons;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Settings;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Templates;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Realized coverage for the vendored icon subset, its mapping, fill roles, missing marker, and provenance.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class IconAssetTests
{
    private static readonly string[] geometryNames = ["Welcome", "Overview", "Json", "Project", "Missing"];

    private static readonly IReadOnlyDictionary<string, string> expectedChecksums =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["hand-waving"] = "07c4c9d18c9a4c86508fde52cddaf95aa1499552c99bfeebc404d5b991fdf41f",
            ["info"] = "438a1f319d53d86e840af73fb7b1d224c9224a437b9fa449a4e2cb45a82a3552",
            ["brackets-curly"] = "170562ad2b247d86bf2b5fd727cf3f96681b0cbef92aab49346ec6e235802ca9",
            ["cube"] = "7c0f13cb492d0dd212e48ab259dabec4627df32d71aa00ce31b3228c0f35112b",
            ["warning-circle"] = "e48a90760a68659fd3f06dd9187e471aead4877c72e915339071b2c7f7bd714c",
        };

    [Fact]
    public Task TheFiveGeometryResourcesResolveAsStreamGeometry() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var name in geometryNames)
                {
                    Assert.IsType<StreamGeometry>(GeometryResource(name));
                }
            });

    [Fact]
    public Task EveryShippedSurfaceRealizesItsMappedGlyph() =>
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
                    var documents = FindTabControl(window, "Document tabs");

                    var welcome = Assert.Single(documents.GetVisualDescendants().OfType<TabItem>());
                    AssertGlyph(welcome, "TabIconSlot", "Welcome", IconKeys.Welcome);

                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    HeadlessSession.Pump(window);

                    var projects = window.GetVisualDescendants()
                        .OfType<ListBox>()
                        .Single(control => AutomationProperties.GetName(control) == "Open projects");
                    var row = Assert.Single(projects.GetVisualDescendants().OfType<ListBoxItem>());
                    AssertGlyph(row, "ProjectRowIconSlot", "Project", IconKeys.Project);

                    var tree = FindTree(window);
                    var nodes = tree.GetVisualDescendants().OfType<TreeViewItem>().ToList();
                    Assert.Equal(3, nodes.Count);
                    AssertGlyph(nodes[0], "NodeIconSlot", "Overview", IconKeys.Overview);
                    AssertGlyph(nodes[1], "NodeIconSlot", "Json", IconKeys.Json);

                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "overview"),
                        NavigationIntent.Activate);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "json"),
                        NavigationIntent.Activate);
                    HeadlessSession.Pump(window);

                    var tabs = documents.GetVisualDescendants().OfType<TabItem>().ToList();
                    Assert.Equal(2, tabs.Count);
                    AssertGlyph(FindDocumentTab(tabs, DocumentKinds.ProjectOverview), "TabIconSlot",
                        "Overview", IconKeys.Overview);
                    AssertGlyph(FindDocumentTab(tabs, DocumentKinds.ProjectJson), "TabIconSlot",
                        "Json", IconKeys.Json);
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task FillRolesFollowStateAndTheme() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                foreach (var settings in new[]
                {
                    new AppSettings { Theme = ThemeSelection.Light },
                    new AppSettings { Theme = ThemeSelection.Dark },
                    new AppSettings
                    {
                        Theme = ThemeSelection.Dark,
                        HighContrast = true,
                        Density = Density.Compact,
                    },
                })
                {
                    using var workspace = new TempWorkspace();
                    using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                    new AvaloniaThemeApplier().Apply(settings);
                    var window = host.Services.GetRequiredService<ShellWindow>();
                    window.Show();
                    HeadlessSession.Pump(window);

                    try
                    {
                        var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                        var entry = await ProjectFixtures.LoadCeriaAsync();
                        shell.Workspace.Add(entry);
                        var second = CreateBlankEntry("Second");
                        shell.Workspace.Add(second);
                        await shell.Navigation.NavigateToAsync(
                            new ModelPath("project", entry.Id, "overview"),
                            NavigationIntent.Activate);
                        await shell.Navigation.NavigateToAsync(
                            new ModelPath("project", entry.Id, "json"),
                            NavigationIntent.Activate);
                        shell.Explorer.SelectedNode = shell.Explorer.Nodes[0];
                        HeadlessSession.Pump(window);

                        var documents = FindTabControl(window, "Document tabs");
                        var tabs = documents.GetVisualDescendants().OfType<TabItem>().ToList();
                        var projects = window.GetVisualDescendants()
                            .OfType<ListBox>()
                            .Single(control => AutomationProperties.GetName(control) == "Open projects");

                        foreach (var tab in tabs)
                        {
                            AssertGlyphFill(
                                tab,
                                "TabIconSlot",
                                tab.IsSelected ? "Mocassin.TabActiveForeground" : "Mocassin.TabInactiveForeground");
                        }

                        var nodes = FindTree(window).GetVisualDescendants().OfType<TreeViewItem>().ToList();
                        Assert.Equal(3, nodes.Count);
                        AssertGlyphFill(nodes[0], "NodeIconSlot", "Mocassin.SelectedForeground");
                        AssertGlyphFill(nodes[1], "NodeIconSlot", "Mocassin.ForegroundMutedBrush");

                        var rows = projects.GetVisualDescendants().OfType<ListBoxItem>().ToList();
                        Assert.Equal(2, rows.Count);
                        AssertGlyphFill(rows[0], "ProjectRowIconSlot", "Mocassin.SelectedForeground");
                        AssertGlyphFill(rows[1], "ProjectRowIconSlot", "Mocassin.ForegroundMutedBrush");
                        Assert.True(rows[0].IsSelected);
                        Assert.False(rows[1].IsSelected);

                        shell.Workspace.Activate(second);
                        HeadlessSession.Pump(window);
                        rows = projects.GetVisualDescendants().OfType<ListBoxItem>().ToList();
                        AssertGlyphFill(rows[0], "ProjectRowIconSlot", "Mocassin.ForegroundMutedBrush");
                        AssertGlyphFill(rows[1], "ProjectRowIconSlot", "Mocassin.SelectedForeground");
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            });

    [Fact]
    public Task AnUnresolvedKeyRendersTheMissingMarkerAndNoShippedSurfaceDoes() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                var probe = new Window { Width = 80, Height = 80 };
                var probeSlot = new ContentControl
                {
                    Name = "TabIconSlot",
                    Width = 16,
                    Height = 16,
                    Content = new AppIcon("future-surface"),
                };
                probe.Content = probeSlot;
                probe.Show();
                HeadlessSession.Pump(probe);
                try
                {
                    var glyph = VisibleGlyph(probeSlot);
                    Assert.Same(GeometryResource("Missing"), glyph.Data);
                    Assert.Equal(
                        ResourceBrush("Mocassin.ErrorForeground").Color,
                        Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Fill).Color);
                }
                finally
                {
                    probe.Close();
                }

                using var shellWorkspace = new TempWorkspace();
                using var shellHost = AppHost.Build(new AppStartupOptions { DataDirectory = shellWorkspace.Path });
                var window = shellHost.Services.GetRequiredService<ShellWindow>();
                window.Show();
                HeadlessSession.Pump(window);

                try
                {
                    var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                    var entry = await ProjectFixtures.LoadCeriaAsync();
                    shell.Workspace.Add(entry);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "overview"),
                        NavigationIntent.Activate);
                    await shell.Navigation.NavigateToAsync(
                        new ModelPath("project", entry.Id, "json"),
                        NavigationIntent.Activate);
                    HeadlessSession.Pump(window);

                    var slots = window.GetVisualDescendants()
                        .OfType<ContentControl>()
                        .Where(control => control.Name is "TabIconSlot" or "NodeIconSlot" or "ProjectRowIconSlot")
                        .Where(control => control.IsEffectivelyVisible)
                        .ToList();
                    Assert.NotEmpty(slots);
                    var missing = GeometryResource("Missing");
                    var error = ResourceBrush("Mocassin.ErrorForeground").Color;
                    foreach (var slot in slots)
                    {
                        var glyph = VisibleGlyph(slot);
                        Assert.NotSame(missing, glyph.Data);
                        Assert.NotEqual(error, Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Fill).Color);
                    }
                }
                finally
                {
                    window.Close();
                }
            });

    [Fact]
    public Task ExcludedSurfacesStayTextOnly() =>
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
                    AssertNoIconSlot(window.GetVisualDescendants().OfType<WelcomeDocumentView>().Single());

                    shell.Settings.Open();
                    HeadlessSession.Pump(window);
                    AssertNoIconSlot(window.GetVisualDescendants().OfType<SettingsDialogView>().Single());
                }
                finally
                {
                    window.Close();
                }

                var views = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.Views");
                var excludedFiles = Directory.EnumerateFiles(Path.Combine(views, "Dialogs"), "*.axaml")
                    .Append(Path.Combine(views, "Notifications", "NotificationHostView.axaml"))
                    .Append(Path.Combine(views, "Settings", "SettingsDialogView.axaml"))
                    .Append(Path.Combine(views, "Documents", "WelcomeDocumentView.axaml"));
                foreach (var file in excludedFiles)
                {
                    var text = File.ReadAllText(file);
                    Assert.DoesNotContain("TabIconSlot", text, StringComparison.Ordinal);
                    Assert.DoesNotContain("NodeIconSlot", text, StringComparison.Ordinal);
                    Assert.DoesNotContain("ProjectRowIconSlot", text, StringComparison.Ordinal);
                }
            });

    [Fact]
    public void ProvenanceAndLicenceArePinnedAndMachineChecked()
    {
        var icons = RepositoryPaths.Combine("src", "dotnet", "Mocassin.App.Views", "Icons");
        var axaml = File.ReadAllText(Path.Combine(icons, "Icons.axaml"));
        var provenance = File.ReadAllText(Path.Combine(icons, "IconsProvenance.md"));
        var licencePath = Path.Combine(icons, "PhosphorLicense.txt");
        var licence = File.ReadAllText(licencePath);
        var notice = File.ReadAllText(RepositoryPaths.Combine("NOTICE.md"));
        var converter = File.ReadAllText(Path.Combine(icons, "IconGeometryConverter.cs"));

        Assert.Contains("SPDX-License-Identifier: MIT", axaml, StringComparison.Ordinal);
        Assert.Contains("2b75f3ad12b420c9504ef05df8d2564a28f8500e", axaml, StringComparison.Ordinal);
        Assert.Contains("2b75f3ad12b420c9504ef05df8d2564a28f8500e", provenance, StringComparison.Ordinal);
        Assert.Contains("2b75f3ad12b420c9504ef05df8d2564a28f8500e", notice, StringComparison.Ordinal);

        var resources = Regex.Matches(axaml, "x:Key=\"Mocassin\\.Icon\\.(?<name>[A-Za-z]+)\"")
            .Select(match => match.Groups["name"].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(
            geometryNames.OrderBy(name => name, StringComparer.Ordinal),
            resources);

        var keys = new[] { IconKeys.Welcome, IconKeys.Overview, IconKeys.Json, IconKeys.Project };
        foreach (var key in keys)
        {
            Assert.Contains($"`{key}`", provenance, StringComparison.Ordinal);
        }

        foreach (var (name, checksum) in expectedChecksums)
        {
            Assert.Matches(new Regex($"`{Regex.Escape(name)}`[^\\n]*`{checksum}`"), provenance);
        }

        var listed = Regex.Matches(provenance, "\\b(?<hash>[0-9a-f]{64})\\b")
            .Select(match => match.Groups["hash"].Value)
            .ToList();
        Assert.All(expectedChecksums.Values, checksum => Assert.Contains(checksum, listed));

        Assert.Contains("MIT License", licence, StringComparison.Ordinal);
        Assert.Contains("Copyright (c) 2023 Phosphor Icons", licence, StringComparison.Ordinal);
        Assert.Equal(
            "b5b1f1da112d18ea2147decfd48ddc1bf2b5aeb6c265381579340e95b15a2bb2",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(licencePath))).ToLowerInvariant());

        Assert.Contains("Phosphor Icons (vendored icon subset)", notice, StringComparison.Ordinal);
        Assert.Contains("IconsProvenance.md", notice, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", converter, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", converter, StringComparison.Ordinal);
        Assert.DoesNotContain("new Uri(", converter, StringComparison.Ordinal);
    }

    private static ProjectWorkspaceEntry CreateBlankEntry(string name)
    {
        var project = ProjectTemplateFactory.CreateProject(
            ProjectTemplateKind.Blank,
            name,
            DateTimeOffset.UnixEpoch);
        return new ProjectWorkspaceEntry(project, $"/tmp/icon-pass-{name}.mocprj");
    }

    private static TabItem FindDocumentTab(IEnumerable<TabItem> tabs, string kind) =>
        tabs.Single(tab => tab.Header is DocumentViewModel document
            && string.Equals(document.Key.Kind, kind, StringComparison.Ordinal));

    private static TabControl FindTabControl(Window window, string name) =>
        window.GetVisualDescendants()
            .OfType<TabControl>()
            .Single(control => AutomationProperties.GetName(control) == name);

    private static TreeView FindTree(Window window) =>
        window.GetVisualDescendants()
            .OfType<TreeView>()
            .Single(control => AutomationProperties.GetName(control) == "Project explorer");

    private static ContentControl FindSlot(Control ancestor, string name) =>
        ancestor.GetVisualDescendants()
            .OfType<ContentControl>()
            .Single(control => control.Name == name);

    private static void AssertGlyph(Control ancestor, string slotName, string geometryName, string key)
    {
        var slot = FindSlot(ancestor, slotName);
        Assert.True(slot.IsEffectivelyVisible, $"{slotName} is collapsed.");
        var icon = Assert.IsType<AppIcon>(slot.Content);
        Assert.Equal(key, icon.Key);
        var glyph = VisibleGlyph(slot);
        Assert.Same(GeometryResource(geometryName), glyph.Data);
    }

    private static void AssertGlyphFill(Control ancestor, string slotName, string role)
    {
        var slot = FindSlot(ancestor, slotName);
        Assert.Equal(
            ResourceBrush(role).Color,
            Assert.IsAssignableFrom<ISolidColorBrush>(slot.Foreground).Color);
        var glyph = VisibleGlyph(slot);
        Assert.Equal(
            ResourceBrush(role).Color,
            Assert.IsAssignableFrom<ISolidColorBrush>(glyph.Fill).Color);
    }

    private static AvaloniaPath VisibleGlyph(ContentControl slot) =>
        Assert.Single(slot.GetVisualDescendants().OfType<AvaloniaPath>(), path => path.IsVisible);

    private static void AssertNoIconSlot(Avalonia.Visual ancestor)
    {
        Assert.DoesNotContain(
            ancestor.GetVisualDescendants().OfType<ContentControl>(),
            control => control.Name is "TabIconSlot" or "NodeIconSlot" or "ProjectRowIconSlot");
    }

    private static StreamGeometry GeometryResource(string name) =>
        Assert.IsType<StreamGeometry>(
            Application.Current is { } application
            && application.TryFindResource("Mocassin.Icon." + name, out var value)
                ? value
                : null);

    private static SolidColorBrush ResourceBrush(string key) =>
        Assert.IsType<SolidColorBrush>(
            Application.Current is { } application
            && application.TryFindResource(key, application.ActualThemeVariant, out var value)
                ? value
                : null);
}
