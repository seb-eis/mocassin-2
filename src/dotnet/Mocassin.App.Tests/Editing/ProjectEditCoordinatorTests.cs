// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Packages.Projects.Templates;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.Editing;

/// <summary>
/// Verifies the single write path, change notifications, and save-point semantics of the edit coordinator.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectEditCoordinatorTests
{
    private static readonly DateTimeOffset createdAt = new(2026, 11, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ApplyUndoRedoRecordOneChangeAndAnnounceEachKind()
    {
        var coordinator = CreateCoordinator();
        var events = new List<ProjectChangedEventArgs>();
        coordinator.ProjectChanged += (_, args) => events.Add(args);

        var applied = coordinator.Apply("Rename project", project => Rename(project, "Renamed"));

        Assert.Equal(EditApplyOutcome.Applied, applied.Outcome);
        Assert.True(applied.HasChange);
        Assert.Null(applied.Message);
        Assert.True(coordinator.CanUndo);
        Assert.False(coordinator.CanRedo);
        Assert.True(coordinator.IsDirty);
        var edited = Assert.Single(events);
        Assert.Equal(ProjectChangeKind.Edited, edited.Kind);
        Assert.Equal("Editable", edited.Previous.Metadata.Name);
        Assert.Equal("Renamed", edited.Current.Metadata.Name);
        Assert.Equal("Rename project", edited.Description);

        Assert.True(coordinator.Undo());
        Assert.Equal("Editable", coordinator.Document.Project.Metadata.Name);
        Assert.False(coordinator.CanUndo);
        Assert.True(coordinator.CanRedo);
        Assert.False(coordinator.IsDirty);
        Assert.Equal(ProjectChangeKind.Undo, events[^1].Kind);
        Assert.Equal("Renamed", events[^1].Previous.Metadata.Name);
        Assert.Equal("Editable", events[^1].Current.Metadata.Name);

        Assert.True(coordinator.Redo());
        Assert.Equal("Renamed", coordinator.Document.Project.Metadata.Name);
        Assert.True(coordinator.CanUndo);
        Assert.False(coordinator.CanRedo);
        Assert.True(coordinator.IsDirty);
        Assert.Equal(ProjectChangeKind.Redo, events[^1].Kind);
    }

    [Fact]
    public void NoOpSameHashEditIsAcceptedWithoutChangeOrEvent()
    {
        var coordinator = CreateCoordinator();
        var changed = 0;
        var rejected = 0;
        coordinator.ProjectChanged += (_, _) => changed++;
        coordinator.EditRejected += (_, _) => rejected++;

        var result = coordinator.Apply(
            "Rename to the same name",
            static project => project with
            {
                Metadata = project.Metadata with { Name = project.Metadata.Name },
            });

        Assert.Equal(EditApplyOutcome.Applied, result.Outcome);
        Assert.False(result.HasChange);
        Assert.NotNull(result.Edit);
        Assert.Null(result.Edit.Change);
        Assert.Equal(0, changed);
        Assert.Equal(0, rejected);
        Assert.False(coordinator.CanUndo);
        Assert.False(coordinator.IsDirty);
    }

    [Fact]
    public void RejectedEditLeavesTheProjectStacksAndSavePointUnchanged()
    {
        var project = CreateProject();
        var document = new ProjectDocument(project);
        var coordinator = new ProjectEditCoordinator(document);
        coordinator.Apply("Rename project", projectValue => Rename(projectValue, "Renamed"));
        Assert.True(coordinator.Undo());
        Assert.True(coordinator.CanRedo);
        var lastSaved = document.LastSavedProject;
        var changed = 0;
        EditRejectedEventArgs? rejection = null;
        coordinator.ProjectChanged += (_, _) => changed++;
        coordinator.EditRejected += (_, args) => rejection = args;

        var result = coordinator.Apply("Clear the name", projectValue => Rename(projectValue, string.Empty));

        Assert.Equal(EditApplyOutcome.Rejected, result.Outcome);
        Assert.NotNull(result.Edit);
        Assert.False(result.Edit.IsSuccess);
        Assert.Null(result.Edit.Change);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Same(project, document.Project);
        Assert.Same(lastSaved, document.LastSavedProject);
        Assert.False(coordinator.IsDirty);
        Assert.False(coordinator.CanUndo);
        Assert.True(coordinator.CanRedo);
        Assert.Equal(0, changed);
        Assert.NotNull(rejection);
        Assert.Contains(
            rejection.Diagnostics,
            static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReadOnlyCoordinatorRefusesEditsWithoutADiagnosticCode()
    {
        var project = CreateProject();
        var session = ProjectSession.CreateStandalone(project, "/tmp/read-only.mocprj", isReadOnly: true);
        var coordinator = session.EditCoordinator;
        var changed = 0;
        var rejected = 0;
        coordinator.ProjectChanged += (_, _) => changed++;
        coordinator.EditRejected += (_, _) => rejected++;

        var result = coordinator.Apply("Rename project", projectValue => Rename(projectValue, "Changed"));

        Assert.False(coordinator.CanEdit);
        Assert.Equal(EditApplyOutcome.ReadOnly, result.Outcome);
        Assert.Null(result.Edit);
        Assert.Equal(ProjectEditCoordinator.ReadOnlyMessage, result.Message);
        Assert.Same(project, coordinator.Document.Project);
        Assert.False(coordinator.IsDirty);
        Assert.Equal(0, changed);
        Assert.Equal(0, rejected);
    }

    [Fact]
    public void SavePointStaysCorrectAcrossSaveUndoAndRedo()
    {
        var coordinator = CreateCoordinator();
        var dirtyNotifications = new List<string?>();
        var events = new List<ProjectChangedEventArgs>();
        coordinator.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProjectEditCoordinator.IsDirty))
            {
                dirtyNotifications.Add(args.PropertyName);
            }
        };
        coordinator.ProjectChanged += (_, args) => events.Add(args);

        coordinator.Apply("Rename project", project => Rename(project, "Saved"));
        Assert.True(coordinator.IsDirty);
        coordinator.Document.MarkSaved();
        coordinator.NotifySaved();

        Assert.False(coordinator.IsDirty);
        Assert.False(coordinator.Document.IsDirty);
        var saved = events[^1];
        Assert.Equal(ProjectChangeKind.Saved, saved.Kind);
        Assert.Same(saved.Previous, saved.Current);

        Assert.True(coordinator.Undo());
        Assert.True(coordinator.IsDirty);
        Assert.True(coordinator.Redo());
        Assert.False(coordinator.IsDirty);

        coordinator.Apply("Rename again", project => Rename(project, "After save"));
        Assert.True(coordinator.IsDirty);
        Assert.True(coordinator.Undo());
        Assert.False(coordinator.IsDirty);

        coordinator.Apply("Rename third time", project => Rename(project, "Third"));
        Assert.True(coordinator.IsDirty);
        coordinator.Document.MarkSaved();
        coordinator.NotifySaved();
        Assert.False(coordinator.IsDirty);

        Assert.Contains(nameof(ProjectEditCoordinator.IsDirty), dirtyNotifications);
    }

    [Fact]
    public void ReplaceDocumentResetsStacksAndReadOnlyState()
    {
        var coordinator = CreateCoordinator();
        coordinator.Apply("Rename project", project => Rename(project, "Dirty"));
        Assert.True(coordinator.CanUndo);
        Assert.True(coordinator.IsDirty);
        var propertyNotifications = new List<string?>();
        var events = new List<ProjectChangedEventArgs>();
        coordinator.PropertyChanged += (_, args) => propertyNotifications.Add(args.PropertyName);
        coordinator.ProjectChanged += (_, args) => events.Add(args);
        var replacement = new ProjectDocument(CreateProject("Replacement"));

        coordinator.ReplaceDocument(replacement, canEdit: false);

        Assert.Same(replacement, coordinator.Document);
        Assert.False(coordinator.CanEdit);
        Assert.False(coordinator.CanUndo);
        Assert.False(coordinator.CanRedo);
        Assert.False(coordinator.IsDirty);
        Assert.Contains(nameof(ProjectEditCoordinator.CanEdit), propertyNotifications);
        var replaced = Assert.Single(events);
        Assert.Equal(ProjectChangeKind.Replaced, replaced.Kind);
        Assert.Equal("Dirty", replaced.Previous.Metadata.Name);
        Assert.Equal("Replacement", replaced.Current.Metadata.Name);
    }

    [Fact]
    public void AReadOnlySessionCoordinatorFollowsTheLockState()
    {
        var project = CreateProject();
        var session = ProjectSession.CreateStandalone(project, "/tmp/session.mocprj");
        Assert.True(session.EditCoordinator.CanEdit);

        session.IsReadOnly = true;

        Assert.False(session.EditCoordinator.CanEdit);
        var result = session.EditCoordinator.Apply(
            "Rename project",
            projectValue => Rename(projectValue, "Changed"));
        Assert.Equal(EditApplyOutcome.ReadOnly, result.Outcome);
    }

    private static ProjectEditCoordinator CreateCoordinator() =>
        new(new ProjectDocument(CreateProject()));

    private static ProjectModel CreateProject(string name = "Editable") =>
        ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, name, createdAt);

    private static ProjectModel Rename(ProjectModel project, string name) =>
        project with { Metadata = project.Metadata with { Name = name } };
}
