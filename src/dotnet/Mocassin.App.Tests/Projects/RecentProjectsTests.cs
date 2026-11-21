// SPDX-License-Identifier: MIT

using Mocassin.App.Files;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Projects;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Tests.Projects;

/// <summary>
/// Verifies the recent-project open, missing, and removal states.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class RecentProjectsTests
{
    [Fact]
    public async Task RecentEntriesOpenAndMissingEntriesAreDisabled()
    {
        using var workspace = new TempWorkspace();
        var existing = workspace.Combine("existing.mocprj");
        await File.WriteAllTextAsync(existing, "{}");
        var missing = workspace.Combine("missing.mocprj");
        var settings = new Setup(
            new RecentFile(existing, DateTimeOffset.UnixEpoch),
            new RecentFile(missing, DateTimeOffset.UnixEpoch));
        var opened = new List<string>();
        var recent = new RecentProjectsViewModel(
            settings.Service,
            new ProjectFileProbe(),
            path =>
            {
                opened.Add(path);
                return Task.CompletedTask;
            });

        Assert.False(recent.Entries.Single(entry => entry.Path == missing).OpenCommand.CanExecute(null));
        Assert.True(recent.Entries.Single(entry => entry.Path == existing).OpenCommand.CanExecute(null));
        recent.Entries.Single(entry => entry.Path == existing).OpenCommand.Execute(null);
        Assert.Equal([existing], opened);
    }

    [Fact]
    public async Task RemoveDropsTheEntryFromSettings()
    {
        using var workspace = new TempWorkspace();
        var path = workspace.Combine("remove.mocprj");
        await File.WriteAllTextAsync(path, "{}");
        var settings = new Setup(new RecentFile(path, DateTimeOffset.UnixEpoch));
        var recent = new RecentProjectsViewModel(
            settings.Service,
            new ProjectFileProbe(),
            _ => Task.CompletedTask);

        await ((IAsyncRelayCommand)recent.Entries.Single().RemoveCommand).ExecuteAsync(null);

        Assert.Empty(settings.Service.Current.RecentFiles);
        Assert.Empty(recent.Entries);
    }

    private sealed class Setup
    {
        public Setup(params RecentFile[] files)
        {
            var load = new AppSettingsLoadResult(
                AppSettings.Default with { RecentFiles = [.. files] },
                [],
                false,
                true,
                null);
            Service = new SettingsService(new InMemorySettingsStore { LoadResult = load }, load);
        }

        public SettingsService Service { get; }
    }
}
