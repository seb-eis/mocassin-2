// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Documents.Project;

namespace Mocassin.App.Tests.Documents;

/// <summary>
/// Verifies document identity, reuse, activation, and closing.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class DocumentCollectionTests
{
    [Fact]
    public async Task OpeningARealDocumentReplacesTheWelcomeDocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        var welcome = new WelcomeDocumentViewModel(new RelayCommand(() => { }));
        documents.OpenOrActivate(welcome);
        Assert.Same(welcome, documents.ActiveDocument);

        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        documents.OpenOrActivate(overview);

        Assert.Single(documents.Documents);
        Assert.Same(overview, documents.ActiveDocument);
    }

    [Fact]
    public async Task OpeningTheSameKeyActivatesTheExistingDocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        var first = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        var second = new ProjectOverviewViewModel(entry, new RecordingClipboardService());

        documents.OpenOrActivate(first);
        var activated = documents.OpenOrActivate(second);

        Assert.Same(first, activated);
        Assert.Single(documents.Documents);
    }

    [Fact]
    public async Task DocumentKeysAreScopedByProject()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());

        Assert.Equal(DocumentKinds.ProjectOverview, overview.Key.Kind);
        Assert.Equal(entry.Id, overview.Key.ScopeId);
        Assert.NotEqual(new DocumentKey(DocumentKinds.ProjectOverview, Guid.NewGuid()), overview.Key);
    }

    [Fact]
    public async Task ClosingTheActiveDocumentSelectsTheNextDocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        var json = new ProjectJsonViewModel(entry, new RecordingClipboardService());
        documents.OpenOrActivate(overview);
        documents.OpenOrActivate(json);

        await documents.CloseAsync(json);

        Assert.Same(overview, documents.ActiveDocument);
        Assert.Single(documents.Documents);
    }

    [Fact]
    public async Task CloseAllLeavesNoActiveDocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        documents.OpenOrActivate(new ProjectOverviewViewModel(entry, new RecordingClipboardService()));
        documents.OpenOrActivate(new ProjectJsonViewModel(entry, new RecordingClipboardService()));

        await documents.CloseAllAsync();

        Assert.Empty(documents.Documents);
        Assert.Null(documents.ActiveDocument);
    }

    [Fact]
    public async Task ActivateIndexSelectsADocument()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        var json = new ProjectJsonViewModel(entry, new RecordingClipboardService());
        documents.OpenOrActivate(overview);
        documents.OpenOrActivate(json);

        Assert.True(documents.ActivateIndex(0));
        Assert.Same(overview, documents.ActiveDocument);
        Assert.False(documents.ActivateIndex(5));
    }

    [Fact]
    public async Task MoveReordersDocuments()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var documents = new DocumentCollectionViewModel();
        var welcome = new WelcomeDocumentViewModel(new RelayCommand(() => { }));
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        documents.OpenOrActivate(welcome);
        documents.OpenOrActivate(overview);

        documents.Move(1, 0);

        Assert.Same(overview, documents.Documents[0]);
    }
}
