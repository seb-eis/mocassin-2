// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.Views.Documents;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;

namespace Mocassin.App.Tests.Views;

/// <summary>
/// Measures the rendered welcome-button label centre against the button centre in both themes and densities.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class WelcomeCentringTests
{
    [Fact]
    public Task EveryWelcomeButtonCentresItsRenderedLabelInBothThemesAndDensities() =>
        HeadlessSession.DispatchAsync(
            () =>
            {
                foreach (var theme in new[] { ThemeSelection.Light, ThemeSelection.Dark })
                {
                    foreach (var density in new[] { Density.Comfortable, Density.Compact })
                    {
                        using var workspace = new TempWorkspace();
                        var projectPath = Path.Combine(workspace.Path, "recent.mocprj");
                        File.WriteAllText(projectPath, "{}");
                        File.WriteAllText(
                            Path.Combine(workspace.Path, "Mocassin.Settings.json"),
                            $$$"""{"format":"mocassin.settings","theme":"{{{theme.ToString().ToLowerInvariant()}}}","density":"{{{density.ToString().ToLowerInvariant()}}}","recentFiles":[{"path":{{{System.Text.Json.JsonSerializer.Serialize(projectPath)}}},"lastOpenedUtc":"2026-11-20T12:00:00Z"}]}""");
                        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
                        new AvaloniaThemeApplier().Apply(
                            AppSettings.Default with { Theme = theme, Density = density });
                        var window = host.Services.GetRequiredService<ShellWindow>();
                        window.Show();
                        HeadlessSession.Pump(window);

                        try
                        {
                            var welcome = Assert.Single(window.GetVisualDescendants().OfType<WelcomeDocumentView>());
                            AssertCentred(welcome, "New project");
                            AssertCentred(welcome, "Open project");
                            AssertCentred(welcome, "Keyboard shortcuts");
                            AssertCentred(welcome, "Open");
                            AssertCentred(welcome, "Remove");

                            var recent = welcome.GetVisualDescendants()
                                .OfType<Button>()
                                .Single(button => button.Content as string == "Open");
                            Assert.Contains("compact", recent.Classes);
                            var main = welcome.GetVisualDescendants()
                                .OfType<Button>()
                                .Single(button => button.Content as string == "New project");
                            Assert.DoesNotContain("compact", main.Classes);
                        }
                        finally
                        {
                            window.Close();
                        }
                    }
                }
            });

    private static void AssertCentred(WelcomeDocumentView welcome, string label)
    {
        var button = welcome.GetVisualDescendants()
            .OfType<Button>()
            .Single(candidate => candidate.Content as string == label);
        var text = button.GetVisualDescendants()
            .OfType<TextBlock>()
            .First(candidate => candidate.Text == label);
        var centre = text.TranslatePoint(
            new Point(text.Bounds.Width / 2, text.Bounds.Height / 2),
            button) ?? throw new InvalidOperationException($"The {label} label is not attached to its button.");
        var expected = new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        Assert.InRange(Math.Abs(centre.X - expected.X), 0d, 1d);
        Assert.InRange(Math.Abs(centre.Y - expected.Y), 0d, 1d);
    }
}
