// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.Tests.Navigation;

/// <summary>
/// Verifies the project explorer nodes and opening.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectExplorerTests
{
    [Fact]
    public async Task TheExplorerBuildsProjectLevelNodes()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var workspace = new WorkspaceViewModel();
        workspace.Add(entry);
        var console = new ConsoleService(new InlineUiDispatcher(), new ManualTimeProvider());
        var documents = new DocumentCollectionViewModel();
        var navigation = new NavigationService(
            documents,
            workspace,
            console,
            [new ProjectNavigationTargetProvider(workspace, new RecordingClipboardService())]);
        var explorer = new ProjectExplorerViewModel(
            workspace,
            [new ProjectExplorerNodeProvider()],
            navigation);

        Assert.Equal(2, explorer.Nodes.Count);
        Assert.Equal("Overview", explorer.Nodes[0].Label);
        Assert.Equal("Data (JSON)", explorer.Nodes[1].Label);

        explorer.SelectedNode = explorer.Nodes[0];
        await explorer.OpenSelectedCommand.ExecuteAsync(null);

        Assert.Single(documents.Documents);
        Assert.Equal(DocumentKinds.ProjectOverview, documents.ActiveDocument!.Key.Kind);
    }
}
