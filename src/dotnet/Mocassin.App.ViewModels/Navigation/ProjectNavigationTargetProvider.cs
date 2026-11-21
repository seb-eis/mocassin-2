// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.App.ViewModels.Projects;
using Mocassin.App.ViewModels.Shell;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// Creates the project overview and project JSON documents for the <c>project</c> domain.
/// </summary>
internal sealed class ProjectNavigationTargetProvider : INavigationTargetProvider
{
    private readonly IProjectWorkspace workspace;
    private readonly IClipboardService clipboard;

    /// <summary>
    /// Initializes the project target provider.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="clipboard">The clipboard boundary.</param>
    public ProjectNavigationTargetProvider(IProjectWorkspace workspace, IClipboardService clipboard)
    {
        this.workspace = workspace;
        this.clipboard = clipboard;
    }

    /// <inheritdoc />
    public Guid? ProjectScopeId => null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Domains => ["project"];

    /// <inheritdoc />
    public Task<DocumentViewModel?> CreateAsync(
        ModelPath path,
        NavigationIntent intent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var project = workspace.ActiveProject;
        if (project is null)
        {
            return Task.FromResult<DocumentViewModel?>(null);
        }

        DocumentViewModel? document = path.Property switch
        {
            "overview" => new ProjectOverviewViewModel(project, clipboard),
            "json" => new ProjectJsonViewModel(project, clipboard),
            _ => null,
        };
        return Task.FromResult(document);
    }
}
