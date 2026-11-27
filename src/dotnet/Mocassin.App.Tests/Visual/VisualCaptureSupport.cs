// SPDX-License-Identifier: MIT

using Microsoft.Extensions.DependencyInjection;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;

namespace Mocassin.App.Tests.Visual;

/// <summary>
/// Stabilizes volatile shell text and file paths used by screenshot captures.
/// </summary>
internal static class VisualCaptureSupport
{
    private static readonly DateTimeOffset fixedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void NormalizeShell(IServiceProvider services, ShellViewModel shell)
    {
        var console = services.GetRequiredService<IConsoleService>();
        console.Clear();
        console.ApplicationChannel.Add(
            new ConsoleEntryViewModel(
                fixedTimestamp,
                ConsoleSeverity.Information,
                "Application",
                "MOCASSIN shell started."));
        shell.StatusBar.VersionItem.Text = "visual-test";
    }

    public static async Task<ProjectWorkspaceEntry> LoadProjectAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mocassin-visual-baselines", "project");
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        Directory.CreateDirectory(directory);
        var projectPath = Path.Combine(directory, "ceria.mocprj");
        File.Copy(RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj"), projectPath);
        return await ProjectFixtures.LoadCeriaAsync(projectPath);
    }
}
