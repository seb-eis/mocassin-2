// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Provides the project-level Overview and Data nodes.
/// </summary>
internal sealed class ProjectExplorerNodeProvider : IExplorerNodeProvider
{
    /// <inheritdoc />
    public IEnumerable<ExplorerNode> CreateNodes(ProjectWorkspaceEntry project)
    {
        yield return new ExplorerNode(
            "project.overview",
            "Overview",
            "overview",
            new ModelPath("project", project.Id, "overview"));
        yield return new ExplorerNode(
            "project.json",
            "Data (JSON)",
            "json",
            new ModelPath("project", project.Id, "json"));
    }
}
