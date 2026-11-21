// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Observable workspace of loaded projects.
/// </summary>
internal sealed partial class WorkspaceViewModel : ObservableObject, IProjectWorkspace
{
    [ObservableProperty]
    private ProjectWorkspaceEntry? activeProject;

    /// <inheritdoc />
    public event EventHandler? ActiveProjectChanged;

    /// <inheritdoc />
    public IReadOnlyList<ProjectWorkspaceEntry> Projects => ProjectsInternal;

    /// <summary>
    /// Gets the observable project collection used by the switcher.
    /// </summary>
    public ObservableCollection<ProjectWorkspaceEntry> ProjectsInternal { get; } = [];

    /// <inheritdoc />
    public void Add(ProjectWorkspaceEntry project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (Find(project.Id) is not null)
        {
            Activate(project);
            return;
        }

        ProjectsInternal.Add(project);
        if (ActiveProject is null)
        {
            ActiveProject = project;
        }
    }

    /// <inheritdoc />
    public bool Remove(Guid projectId)
    {
        var project = Find(projectId);
        if (project is null)
        {
            return false;
        }

        ProjectsInternal.Remove(project);
        if (ReferenceEquals(ActiveProject, project))
        {
            ActiveProject = ProjectsInternal.FirstOrDefault();
        }

        return true;
    }

    /// <inheritdoc />
    public void Activate(ProjectWorkspaceEntry project)
    {
        if (!ProjectsInternal.Contains(project))
        {
            Add(project);
            return;
        }

        ActiveProject = project;
    }

    /// <inheritdoc />
    public ProjectWorkspaceEntry? Find(Guid projectId) =>
        ProjectsInternal.FirstOrDefault(project => project.Id == projectId);

    partial void OnActiveProjectChanged(ProjectWorkspaceEntry? value) =>
        ActiveProjectChanged?.Invoke(this, EventArgs.Empty);
}
