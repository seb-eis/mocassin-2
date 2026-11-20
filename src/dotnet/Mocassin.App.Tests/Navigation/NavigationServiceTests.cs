// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.Tests.Navigation;

/// <summary>
/// Verifies real document navigation, provider resolution, and history.
/// </summary>
public sealed class NavigationServiceTests
{
    [Fact]
    public async Task TheProjectProviderOpensOverviewAndJsonDocuments()
    {
        var context = await CreateContextAsync();

        var overview = await context.Navigation.NavigateToAsync(
            new ModelPath("project", context.Entry.Id, "overview"),
            NavigationIntent.Activate);
        var json = await context.Navigation.NavigateToAsync(
            new ModelPath("project", context.Entry.Id, "json"),
            NavigationIntent.Activate);

        Assert.True(overview);
        Assert.True(json);
        Assert.Equal(2, context.Documents.Documents.Count);
        Assert.Equal(DocumentKinds.ProjectJson, context.Documents.ActiveDocument!.Key.Kind);
    }

    [Fact]
    public async Task AnUnknownDomainWritesAConsoleNoticeAndDoesNotThrow()
    {
        var context = await CreateContextAsync();

        var opened = await context.Navigation.NavigateToAsync(
            new ModelPath("particle", Guid.NewGuid(), "name"),
            NavigationIntent.Activate);

        Assert.False(opened);
        Assert.Contains(
            context.Console.ApplicationChannel.VisibleEntries,
            entry => entry.Message.Contains("not available", StringComparison.Ordinal));
        Assert.Empty(context.Documents.Documents);
    }

    [Fact]
    public async Task BackAndForwardRestoreTheDocument()
    {
        var context = await CreateContextAsync();
        var overviewPath = new ModelPath("project", context.Entry.Id, "overview");
        var jsonPath = new ModelPath("project", context.Entry.Id, "json");
        await context.Navigation.NavigateToAsync(overviewPath, NavigationIntent.Activate);
        await context.Navigation.NavigateToAsync(jsonPath, NavigationIntent.Activate);

        await context.Navigation.GoBackAsync();
        await context.Navigation.GoForwardAsync();

        Assert.Equal(DocumentKinds.ProjectJson, context.Documents.ActiveDocument!.Key.Kind);
    }

    [Fact]
    public async Task DuplicateProvidersForTheSameScopeAreRejected()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var workspace = new WorkspaceViewModel();
        workspace.Add(entry);
        var console = new ConsoleService(new InlineUiDispatcher(), new ManualTimeProvider());
        var provider = new ProjectNavigationTargetProvider(workspace, new RecordingClipboardService());

        var exception = Assert.Throws<InvalidOperationException>(
            () => new NavigationService(
                new DocumentCollectionViewModel(),
                workspace,
                console,
                [provider, provider]));

        Assert.Contains("registered more than once", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var workspace = new WorkspaceViewModel();
        workspace.Add(entry);
        var documents = new DocumentCollectionViewModel();
        var console = new ConsoleService(new InlineUiDispatcher(), new ManualTimeProvider());
        var navigation = new NavigationService(
            documents,
            workspace,
            console,
            [new ProjectNavigationTargetProvider(workspace, new RecordingClipboardService())]);
        var explorer = new ProjectExplorerViewModel(
            workspace,
            [new ProjectExplorerNodeProvider()],
            navigation);

        return new TestContext(entry, documents, navigation, console, explorer);
    }

    private sealed record TestContext(
        ProjectWorkspaceEntry Entry,
        DocumentCollectionViewModel Documents,
        INavigationService Navigation,
        IConsoleService Console,
        ProjectExplorerViewModel Explorer);
}
