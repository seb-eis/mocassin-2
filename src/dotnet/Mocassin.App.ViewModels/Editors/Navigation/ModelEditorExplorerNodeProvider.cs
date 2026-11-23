// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.ViewModels.Editors.Navigation;

/// <summary>
/// Adds the <c>Model</c> explorer node with the five 24A editor children.
/// </summary>
internal sealed class ModelEditorExplorerNodeProvider : IExplorerNodeProvider
{
    /// <inheritdoc />
    public IEnumerable<ExplorerNode> CreateNodes(ProjectWorkspaceEntry project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var children = ModelEditorDomains.All
            .Select(descriptor => new ExplorerNode(
                $"model.{descriptor.Domain}",
                descriptor.Title,
                Path: descriptor.ActivationPath(project.Id)))
            .ToArray();
        yield return new ExplorerNode("model", "Model", Children: children);
    }
}
