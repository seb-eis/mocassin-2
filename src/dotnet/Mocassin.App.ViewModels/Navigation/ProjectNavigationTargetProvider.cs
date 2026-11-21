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
    private readonly DocumentCollectionViewModel documents;
    private readonly IClipboardService clipboard;

    /// <summary>
    /// Initializes the project target provider.
    /// </summary>
    /// <param name="workspace">The project workspace.</param>
    /// <param name="documents">The open documents, used to focus an existing Overview.</param>
    /// <param name="clipboard">The clipboard boundary.</param>
    public ProjectNavigationTargetProvider(
        IProjectWorkspace workspace,
        DocumentCollectionViewModel documents,
        IClipboardService clipboard)
    {
        this.workspace = workspace;
        this.documents = documents;
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

        if (string.Equals(path.Property, "json", StringComparison.Ordinal))
        {
            return Task.FromResult<DocumentViewModel?>(new ProjectJsonViewModel(project, clipboard));
        }

        var overview = documents.Find(new DocumentKey(DocumentKinds.ProjectOverview, project.Id))
            as ProjectOverviewViewModel;
        overview ??= new ProjectOverviewViewModel(project, clipboard);
        if (path.Property is "name" or "description" or "author" or "notes")
        {
            overview.RequestFocus(path.Property);
        }

        return Task.FromResult<DocumentViewModel?>(overview);
    }
}
