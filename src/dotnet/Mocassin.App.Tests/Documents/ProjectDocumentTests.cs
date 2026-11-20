// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.Core.Project;

namespace Mocassin.App.Tests.Documents;

/// <summary>
/// Verifies the read-only project documents use the real project APIs.
/// </summary>
public sealed class ProjectDocumentTests
{
    [Fact]
    public async Task TheOverviewShowsRealProjectIdentityAndMetadata()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var clipboard = new RecordingClipboardService();
        var overview = new ProjectOverviewViewModel(entry, clipboard);

        Assert.Equal(entry.Name, overview.Name);
        Assert.Equal(entry.Id.ToString("D"), overview.ProjectIdText);
        Assert.Equal(entry.Project.Metadata.MasterSeed, overview.MasterSeed);
        Assert.Equal(entry.Project.SchemaVersion.ToString(CultureInfo.InvariantCulture), overview.SchemaVersionText);
        Assert.Equal(entry.Path, overview.ToolTip);
        Assert.Contains(entry.Project.Metadata.Name, overview.Title, StringComparison.Ordinal);
        Assert.Contains("Writable", overview.LockStateText, StringComparison.Ordinal);

        await overview.CopyMasterSeedCommand.ExecuteAsync(null);

        Assert.Contains(entry.Project.Metadata.MasterSeed, clipboard.Values);
    }

    [Fact]
    public async Task TheJsonDocumentRoundTripsThroughTheCanonicalProjectJson()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var document = new ProjectJsonViewModel(entry, new RecordingClipboardService());

        var parsed = ProjectJson.Deserialize(document.JsonText);

        Assert.Equal(entry.Id, parsed.Metadata.ProjectId);
        Assert.Equal(entry.Name, parsed.Metadata.Name);
    }

    [Fact]
    public async Task TheOverviewContributesInspectorContent()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        var selection = new Mocassin.App.ViewModels.Navigation.InspectorSelection(
            overview.Key,
            entry.Id,
            [],
            "project");

        var inspector = overview.CreateInspector(selection);

        var summary = Assert.IsType<Mocassin.App.ViewModels.Shell.InspectorSummaryViewModel>(inspector);
        Assert.Contains(entry.Name, summary.Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheReadOnlyStateIsReflected()
    {
        var loaded = await ProjectFixtures.LoadCeriaAsync();
        var entry = new Mocassin.App.ViewModels.Projects.ProjectWorkspaceEntry(
            loaded.Project,
            "/tmp/read-only.mocprj",
            isReadOnly: true,
            lockPath: "/tmp/read-only.mocprj.lock");

        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());

        Assert.True(overview.IsReadOnly);
        Assert.Contains("Read-only", overview.LockStateText, StringComparison.Ordinal);
    }
}
