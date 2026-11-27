// SPDX-License-Identifier: MIT

using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors;
using Mocassin.App.ViewModels.Editors.Dummies;
using Mocassin.App.ViewModels.Editors.Particles;
using Mocassin.App.ViewModels.Editors.Sets;
using Mocassin.App.ViewModels.Editors.Sites;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;
using Mocassin.App.Views.Theming;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Light, dark, and high-contrast captures of the 24A editors, plus the narrow site/P1 state.
/// </summary>
[Trait("Tier", "PR-medium")]
public sealed class ModelEditorScreenshotTests
{
    private const string regenerateVariable = "MOCASSIN_REGENERATE_VISUAL_BASELINES";

    [Fact]
    public Task TheModelEditorsHaveVisualBaselines() =>
        HeadlessSession.DispatchAsync(
            async () =>
            {
                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                await CaptureEditorAsync("particles-light", Light(), ModelEditorDomains.Particle);
                await CaptureEditorAsync("particles-dark", Dark(), ModelEditorDomains.Particle);
                await CaptureEditorAsync("particles-highcontrast", HighContrast(), ModelEditorDomains.Particle);
                await CaptureEditorAsync("sets-light", Light(), ModelEditorDomains.ParticleSet);
                await CaptureEditorAsync("sets-dark", Dark(), ModelEditorDomains.ParticleSet);
                await CaptureEditorAsync("sets-highcontrast", HighContrast(), ModelEditorDomains.ParticleSet);
                await CaptureEditorAsync("structure-light", Light(), ModelEditorDomains.Structure);
                await CaptureEditorAsync("structure-dark", Dark(), ModelEditorDomains.Structure);
                await CaptureEditorAsync("structure-highcontrast", HighContrast(), ModelEditorDomains.Structure);
                await CaptureEditorAsync("sites-light", Light(), ModelEditorDomains.CellSite);
                await CaptureEditorAsync("sites-dark", Dark(), ModelEditorDomains.CellSite);
                await CaptureEditorAsync("sites-highcontrast", HighContrast(), ModelEditorDomains.CellSite);
                await CaptureEditorAsync("dummies-light", Light(), ModelEditorDomains.CellDummyPosition);
                await CaptureEditorAsync("dummies-dark", Dark(), ModelEditorDomains.CellDummyPosition);
                await CaptureEditorAsync("dummies-highcontrast", HighContrast(), ModelEditorDomains.CellDummyPosition);
                await CaptureEditorAsync(
                    "sites-narrow-light",
                    Light(),
                    ModelEditorDomains.CellSite,
                    width: 760,
                    height: 900);
            });

    private static AppSettings Light() => new() { Theme = ThemeSelection.Light, ReduceMotion = true };

    private static AppSettings Dark() => new() { Theme = ThemeSelection.Dark, ReduceMotion = true };

    private static AppSettings HighContrast() => new()
    {
        Theme = ThemeSelection.Dark,
        HighContrast = true,
        Density = Density.Compact,
        ReduceMotion = true,
    };

    private static async Task CaptureEditorAsync(
        string name,
        AppSettings settings,
        string domain,
        double width = 1280,
        double height = 900)
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
            var entry = await VisualCaptureSupport.LoadProjectAsync();
            shell.Workspace.Add(entry);
            HeadlessSession.Pump(window);
            Assert.True(await shell.Navigation.NavigateToAsync(
                new ModelPath(domain, Guid.Empty),
                NavigationIntent.Activate));
            HeadlessSession.Pump(window);

            if (shell.Documents.ActiveDocument is ModelEditorDocumentViewModel document)
            {
                switch (document.Editor)
                {
                    case ParticlesEditorViewModel particles:
                        particles.RequestSelection(
                            entry.Project.Particles.First(particle => particle.Name == "O2-").Id.Value);
                        break;
                    case ParticleSetsEditorViewModel sets:
                        sets.RequestSelection(entry.Project.Model.ParticleSets[0].Id.Value);
                        break;
                    case SitesEditorViewModel sites:
                        sites.RequestSelection(
                            entry.Project.Structure.Sites.First(site => site.Name == "O").Id.Value);
                        break;
                    case DummiesEditorViewModel dummies when entry.Project.Structure.DummyPositions.Length > 0:
                        dummies.RequestSelection(entry.Project.Structure.DummyPositions[0].Id.Value);
                        break;
                }
            }

            HeadlessSession.Pump(window);
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
