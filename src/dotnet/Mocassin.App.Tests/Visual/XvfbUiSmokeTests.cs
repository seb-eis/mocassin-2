// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Composition;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Dialogs;
using Mocassin.App.ViewModels.Dialogs.Requests;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Settings;
using Mocassin.App.ViewModels.Shell;
using Mocassin.App.Views.Shell;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Runs the real desktop shell on the X11 platform under Xvfb and captures the workspace, dialog, appearance,
/// and lifecycle evidence. The test is inert unless <c>MOCASSIN_XVFB_CAPTURE</c> names an output directory.
/// </summary>
[Trait("Tier", "release")]
public sealed class XvfbUiSmokeTests
{
    private const int windowWidth = 1280;
    private const int windowHeight = 800;

    [Fact]
    public void TheRealShellRendersUnderXvfb()
    {
        var output = Environment.GetEnvironmentVariable("MOCASSIN_XVFB_CAPTURE");
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        Directory.CreateDirectory(output);
        AppBuilder.Configure<App>(() => new App(AppStartupOptions.Default, startHost: false))
            .UsePlatformDetect()
            .WithInterFont()
            .SetupWithoutStarting();

        RunOnUiThread(() => CaptureAsync(output));
    }

    private static async Task CaptureAsync(string output)
    {
        var steps = new List<string>();
        using var workspace = new TempWorkspace();
        using var host = AppHost.Build(new AppStartupOptions { DataDirectory = workspace.Path });
        var window = host.Services.GetRequiredService<ShellWindow>();
        window.Width = windowWidth;
        window.Height = windowHeight;
        window.Show();
        Pump();

        var shell = Assert.IsType<ShellViewModel>(window.DataContext);
        Assert.Contains(shell.Documents.Documents, document => document.Key.Kind == DocumentKinds.Welcome);
        steps.Add("Shell started on X11 with the welcome document.");

        var themes = host.Services.GetRequiredService<IThemeApplier>();
        var settings = host.Services.GetRequiredService<ISettingsService>();

        var light = Capture(window, output, "01-light-welcome.png");
        themes.Apply(settings.Current with { Theme = ThemeSelection.Dark, HighContrast = false });
        Pump();
        var dark = Capture(window, output, "02-dark-welcome.png");
        themes.Apply(settings.Current with { Theme = ThemeSelection.Light, HighContrast = true });
        Pump();
        var contrast = Capture(window, output, "03-high-contrast-welcome.png");
        themes.Apply(settings.Current with { Theme = ThemeSelection.Light, HighContrast = false });
        Pump();
        Assert.NotEqual(Sha256(light), Sha256(dark));
        Assert.NotEqual(Sha256(dark), Sha256(contrast));
        steps.Add("Captured light, dark, and high-contrast appearances; the three images differ.");

        var sessions = host.Services.GetRequiredService<IProjectSessionService>();
        var created = await sessions.CreateAsync(ProjectTemplateKind.Ceria, "XvfbCeria", workspace.Combine("projects"));
        Assert.True(created.IsSuccess, created.Message);
        var session = Assert.IsType<ProjectSession>(created.Session);
        await shell.Navigation.NavigateToAsync(
            new ModelPath("project", session.Id, "overview"),
            NavigationIntent.Activate);
        Pump();
        var project = Capture(window, output, "04-workspace-ceria.png");
        Assert.Contains(
            shell.Documents.Documents,
            document => document.Key.Kind == DocumentKinds.ProjectOverview && document.Key.ScopeId == session.Id);
        Assert.Contains(
            window.GetVisualDescendants().OfType<TreeView>(),
            control => control.ItemsSource is not null);
        steps.Add($"Created and opened the ceria template at '{session.Path}'.");

        var saved = await sessions.SaveAsync(session);
        Assert.True(saved.IsSuccess, saved.Message);
        Pump();
        var savedShot = Capture(window, output, "05-lifecycle-saved.png");
        steps.Add($"Saved the project; outcome {saved.Outcome}.");

        var dialogs = host.Services.GetRequiredService<IDialogService>();
        var confirmation = dialogs.ConfirmAsync(
            new ConfirmDialogRequest("Delete project", "Automated xvfb dialog capture."));
        Pump();
        var dialogShot = Capture(window, output, "06-dialog-confirm.png");
        var confirmButton = window.GetVisualDescendants().OfType<Button>().Single(button => button.IsDefault);
        confirmButton.Command!.Execute(null);
        Assert.True(await confirmation);
        steps.Add("Opened and confirmed the real in-shell dialog overlay.");

        var closed = await sessions.CloseAsync(session);
        Assert.True(closed.IsSuccess, closed.Message);
        var reopened = await sessions.OpenAsync(session.Path);
        Assert.True(reopened.IsSuccess, reopened.Message);
        Assert.Equal(ProjectSessionOutcome.Opened, reopened.Outcome);
        await shell.Navigation.NavigateToAsync(
            new ModelPath("project", reopened.Session!.Id, "overview"),
            NavigationIntent.Activate);
        Pump();
        var reopenedShot = Capture(window, output, "07-lifecycle-reopened.png");
        steps.Add($"Closed and reopened the project from '{session.Path}'.");

        var images = new[]
        {
            light,
            dark,
            contrast,
            project,
            savedShot,
            dialogShot,
            reopenedShot,
        };
        foreach (var image in images)
        {
            var length = new FileInfo(image).Length;
            Assert.True(length > 4096, $"The screenshot '{image}' is unexpectedly small: {length} bytes.");
        }

        var report = new StringBuilder();
        report.AppendLine("# Automated xvfb UI smoke");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Date (UTC): {DateTimeOffset.UtcNow:O}"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- OS: {Environment.OSVersion}"));
        report.AppendLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"- Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}"));
        report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- Window: {windowWidth}x{windowHeight}"));
        report.AppendLine("- Result: passed");
        report.AppendLine();
        foreach (var step in steps)
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture, $"- {step}"));
        }

        report.AppendLine();
        report.AppendLine("| Image | Bytes | SHA-256 |");
        report.AppendLine("| --- | --- | --- |");
        foreach (var image in images)
        {
            report.AppendLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"| {Path.GetFileName(image)} | {new FileInfo(image).Length} | {Sha256(image)} |"));
        }

        await File.WriteAllTextAsync(Path.Combine(output, "run.md"), report.ToString());
        await File.WriteAllTextAsync(Path.Combine(output, "result.txt"), "passed");
        window.Close();
    }

    private static string Capture(Window window, string output, string name)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        var width = Math.Max(1, (int)Math.Round(window.Bounds.Width));
        var height = Math.Max(1, (int)Math.Round(window.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(window);
        var path = Path.Combine(output, name);
        bitmap.Save(path, PngBitmapEncoderOptions.Default);
        return path;
    }

    private static string Sha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(10);
        Dispatcher.UIThread.RunJobs();
    }

    private static void RunOnUiThread(Func<Task> action)
    {
        var task = action();
        while (!task.IsCompleted)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        task.GetAwaiter().GetResult();
    }
}
