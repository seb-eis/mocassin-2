// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Commands;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Renders the shell and its overlay surfaces and compares the checked-in PNG baselines on Linux.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ScreenshotTests
{
    private const string regenerateVariable = "MOCASSIN_REGENERATE_VISUAL_BASELINES";

    [Fact]
    public Task TheShellRendersLightDarkAndHighContrastBaselines() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                Capture("light", new AppSettings { Theme = ThemeSelection.Light, ReduceMotion = true });
                Capture("dark", new AppSettings { Theme = ThemeSelection.Dark, ReduceMotion = true });
                Capture(
                    "highcontrast",
                    new AppSettings
                    {
                        Theme = ThemeSelection.Dark,
                        HighContrast = true,
                        Density = Density.Compact,
                        ReduceMotion = true,
                    });
            });

    [Fact]
    public Task TheValidationAndSettingsOverlaysHaveLightAndDarkBaselines() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                Capture(
                    "light-validation",
                    new AppSettings { Theme = ThemeSelection.Light, ReduceMotion = true },
                    (_, shell) => shell.Console.ActiveChannel =
                        shell.Console.Channels.Single(channel => channel.Id == "problems"));
                Capture(
                    "dark-validation",
                    new AppSettings { Theme = ThemeSelection.Dark, ReduceMotion = true },
                    (_, shell) => shell.Console.ActiveChannel =
                        shell.Console.Channels.Single(channel => channel.Id == "problems"));
                Capture(
                    "light-settings",
                    new AppSettings { Theme = ThemeSelection.Light, ReduceMotion = true },
                    (_, shell) => shell.Settings.Open());
                Capture(
                    "dark-settings",
                    new AppSettings { Theme = ThemeSelection.Dark, ReduceMotion = true },
                    (_, shell) => shell.Settings.Open());
            });

    [Fact]
    public Task TheOpenProjectIconSurfacesHaveLightDarkAndHighContrastBaselines() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                foreach (var (name, settings) in new[]
                {
                    ("light-project", new AppSettings { Theme = ThemeSelection.Light, ReduceMotion = true }),
                    ("dark-project", new AppSettings { Theme = ThemeSelection.Dark, ReduceMotion = true }),
                    (
                        "highcontrast-project",
                        new AppSettings
                        {
                            Theme = ThemeSelection.Dark,
                            HighContrast = true,
                            Density = Density.Compact,
                            ReduceMotion = true,
                        }),
                })
                {
                    var entry = await VisualCaptureSupport.LoadProjectAsync();
                    Capture(
                        name,
                        settings,
                        (_, shell) =>
                        {
                            shell.Workspace.Add(entry);
                            shell.Navigation.NavigateToAsync(
                                new ModelPath("project", entry.Id, "overview"),
                                NavigationIntent.Activate).GetAwaiter().GetResult();
                            shell.Navigation.NavigateToAsync(
                                new ModelPath("project", entry.Id, "json"),
                                NavigationIntent.Activate).GetAwaiter().GetResult();
                            shell.Explorer.SelectedNode = shell.Explorer.Nodes[0];
                        });
                }
            });

    [Fact]
    public Task ThePaletteCheatSheetAndTabHoverHaveBaselines() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                foreach (var (name, theme) in new[]
                {
                    ("light-palette", ThemeSelection.Light),
                    ("dark-palette", ThemeSelection.Dark),
                })
                {
                    Capture(
                        name,
                        new AppSettings { Theme = theme, ReduceMotion = true },
                        (_, shell) => shell.ShowPaletteCommand.Execute(null),
                        (window, _) =>
                        {
                            var list = window.GetVisualDescendants().OfType<ListBox>()
                                .Single(control => AutomationProperties.GetName(control) == "Filtered commands");
                            var scroll = list.GetVisualDescendants().OfType<ScrollViewer>()
                                .Single(viewer => viewer.Name == "PART_ScrollViewer");
                            Assert.True(
                                scroll.Extent.Height > scroll.Viewport.Height,
                                $"{name}: the palette does not scroll.");
                            scroll.Offset = new Vector(0, scroll.Extent.Height - scroll.Viewport.Height);
                        });
                }

                foreach (var (name, theme) in new[]
                {
                    ("light-cheatsheet", ThemeSelection.Light),
                    ("dark-cheatsheet", ThemeSelection.Dark),
                })
                {
                    Capture(
                        name,
                        new AppSettings { Theme = theme, ReduceMotion = true },
                        (_, shell) => shell.ShowShortcutsCommand.Execute(null),
                        (window, _) =>
                        {
                            var view = window.GetVisualDescendants().OfType<ShortcutCheatSheetView>().Single();
                            var scroll = view.GetVisualDescendants().OfType<ScrollViewer>().First();
                            Assert.True(
                                scroll.Extent.Height > scroll.Viewport.Height,
                                $"{name}: the cheat sheet does not scroll.");
                            scroll.Offset = new Vector(0, scroll.Extent.Height - scroll.Viewport.Height);
                        });
                }

                Capture(
                    "dark-tab-hover",
                    new AppSettings { Theme = ThemeSelection.Dark, ReduceMotion = true },
                    (window, _) =>
                    {
                        var inactive = window.GetVisualDescendants()
                            .OfType<TabControl>()
                            .Single(control => AutomationProperties.GetName(control) == "Console tabs")
                            .GetVisualDescendants()
                            .OfType<TabItem>()
                            .Single(tab => !tab.IsSelected);
                        var point = inactive.TranslatePoint(
                            new Point(inactive.Bounds.Width / 2, inactive.Bounds.Height / 2),
                            window);
                        Assert.NotNull(point);
                        window.MouseMove(point.Value, RawInputModifiers.None);
                    });
            });

    private static void Capture(
        string name,
        AppSettings settings,
        Action<ShellWindow, ShellViewModel>? configure = null,
        Action<ShellWindow, ShellViewModel>? prepareRealized = null,
        double width = 1000,
        double height = 700)
    {
        using var workspace = new TempWorkspace("visual-baselines");
        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
        var window = host.Services.GetRequiredService<ShellWindow>();
        new AvaloniaThemeApplier().Apply(settings);
        window.Width = width;
        window.Height = height;
        window.Show();
        HeadlessSession.Pump(window);
        try
        {
            var shell = Assert.IsType<ShellViewModel>(window.DataContext);
            VisualCaptureSupport.NormalizeShell(host.Services, shell);
            HeadlessSession.Pump(window);
            if (configure is not null)
            {
                configure(window, shell);
                HeadlessSession.Pump(window);
            }

            if (prepareRealized is not null)
            {
                prepareRealized(window, shell);
                HeadlessSession.Pump(window);
            }

            // A second pass settles theme-variant and font resources after earlier tests in the assembly.
            HeadlessSession.Pump(window);
            using var frame = window.CaptureRenderedFrame()
                ?? throw new InvalidOperationException("The headless window did not produce a frame.");
            var path = BaselinePath(name);
            if (Environment.GetEnvironmentVariable(regenerateVariable) == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                frame.Save(path, PngBitmapEncoderOptions.Default);
                return;
            }

            Assert.True(File.Exists(path), $"Missing screenshot baseline {path}. Set {regenerateVariable}=1 to create it.");
            VisualComparer.AssertBaselineMatches(path, frame, VisualComparer.DefaultTolerance);
        }
        finally
        {
            window.Close();
        }
    }

    private static string BaselinePath(string name) =>
        RepositoryPaths.Combine(
            "src",
            "dotnet",
            "Mocassin.App.Tests",
            "Visual",
            "Baselines",
            name + ".png");

}
