// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Builds the project explorer from the registered node providers.
/// </summary>
internal sealed partial class ProjectExplorerViewModel : ObservableObject
{
    private readonly IProjectWorkspace workspace;
    private readonly IReadOnlyList<IExplorerNodeProvider> providers;
    private readonly INavigationService navigation;

    [ObservableProperty]
    private ExplorerNode? selectedNode;

    /// <summary>
    /// Initializes a project explorer.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="providers">The node providers.</param>
    /// <param name="navigation">The navigation service.</param>
    public ProjectExplorerViewModel(
        IProjectWorkspace workspace,
        IEnumerable<IExplorerNodeProvider> providers,
        INavigationService navigation)
    {
        this.workspace = workspace;
        this.providers = providers.ToList();
        this.navigation = navigation;
        workspace.ActiveProjectChanged += (_, _) => Refresh();
        Refresh();
    }

    /// <summary>
    /// Gets the project nodes.
    /// </summary>
    public ObservableCollection<ExplorerNode> Nodes { get; } = [];

    /// <summary>
    /// Rebuilds the node tree for the active project.
    /// </summary>
    public void Refresh()
    {
        Nodes.Clear();
        if (workspace.ActiveProject is not { } project)
        {
            SelectedNode = null;
            return;
        }

        foreach (var provider in providers)
        {
            foreach (var node in provider.CreateNodes(project))
            {
                Nodes.Add(node);
            }
        }
    }

    /// <summary>
    /// Opens the selected node.
    /// </summary>
    [RelayCommand]
    private async Task OpenSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedNode?.Path is not { } path)
        {
            return;
        }

        await navigation.NavigateToAsync(path, NavigationIntent.Activate, cancellationToken);
    }
}
