// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Editing;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditingTests
{
    private static readonly DateTimeOffset createdAt = DateTimeOffset.Parse(
        "2026-11-04T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void ApplyCommitsValidEditAndCreatesUndo()
    {
        var document = new ProjectDocument(CreateProject());

        var result = document.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Change);
        Assert.Equal("Renamed", document.Project.Metadata.Name);
        Assert.True(document.CanUndo);
        Assert.False(document.CanRedo);
        Assert.True(document.IsDirty);
    }

    [Fact]
    public void ApplyInvalidEditLeavesDocumentUnchanged()
    {
        var original = CreateProject();
        var document = new ProjectDocument(original);

        var result = document.Apply(
            "Break seed",
            project => project with { Metadata = project.Metadata with { MasterSeed = "bad" } });

        Assert.False(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Same(original, document.Project);
        Assert.False(document.CanUndo);
        Assert.False(document.IsDirty);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.InvalidMetadata);
    }

    [Fact]
    public void UndoRedoRestoresProjectValues()
    {
        var document = new ProjectDocument(CreateProject());
        var originalName = document.Project.Metadata.Name;
        document.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });

        Assert.True(document.Undo());
        Assert.Equal(originalName, document.Project.Metadata.Name);
        Assert.True(document.CanRedo);

        Assert.True(document.Redo());
        Assert.Equal("Renamed", document.Project.Metadata.Name);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void NewEditClearsRedoStack()
    {
        var document = new ProjectDocument(CreateProject());
        document.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = "First" } });
        document.Undo();
        Assert.True(document.CanRedo);

        document.Apply(
            "Rename project again",
            project => project with { Metadata = project.Metadata with { Name = "Second" } });

        Assert.False(document.CanRedo);
        Assert.Single(document.UndoStack);
    }

    [Fact]
    public void MarkSavedClearsDirtyUntilUndo()
    {
        var document = new ProjectDocument(CreateProject());
        document.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });
        Assert.True(document.IsDirty);

        document.MarkSaved();
        Assert.False(document.IsDirty);

        Assert.True(document.Undo());
        Assert.True(document.IsDirty);

        Assert.True(document.Redo());
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void BulkEditIsSingleUndoStep()
    {
        var document = new ProjectDocument(CreateProject());

        document.Apply(
            "Update metadata",
            project => project with
            {
                Metadata = project.Metadata with
                {
                    Name = "Renamed",
                    Description = "Updated description",
                },
            });

        Assert.Single(document.UndoStack);
        Assert.True(document.Undo());
        Assert.Equal("Ceria", document.Project.Metadata.Name);
        Assert.Null(document.Project.Metadata.Description);
    }

    [Fact]
    public void NoOpMutationDoesNotCreateChange()
    {
        var document = new ProjectDocument(CreateProject());

        var result = document.Apply("No change", static project => project);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.False(document.CanUndo);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void AutosaveDueTracksDirtyStateAndInterval()
    {
        var now = createdAt;
        var interval = TimeSpan.FromMinutes(5);
        var document = new ProjectDocument(CreateProject());

        Assert.False(document.IsAutosaveDue(now, interval));

        document.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = "Renamed" } });

        Assert.True(document.IsAutosaveDue(now, interval));
        document.MarkAutosaved(now);
        Assert.False(document.IsAutosaveDue(now.AddMinutes(4), interval));
        Assert.True(document.IsAutosaveDue(now.AddMinutes(5), interval));
    }

    [Fact]
    public void MarkSavedAcceptsStoreTimestamp()
    {
        var document = new ProjectDocument(CreateProject());
        var saved = document.Project.WithModifiedUtc(createdAt.AddMinutes(10));

        document.MarkSaved(saved);

        Assert.Equal(saved, document.Project);
        Assert.Equal(saved, document.LastSavedProject);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void MaxUndoDepthRejectsNegativeAndZeroIsUnbounded()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProjectDocument(CreateProject()) { MaxUndoDepth = -1 });

        var document = new ProjectDocument(CreateProject()) { MaxUndoDepth = 0 };
        Assert.Equal(0, document.MaxUndoDepth);
        for (var index = 0; index < 5; index++)
        {
            document.Apply(
                $"Rename {index}",
                project => project with { Metadata = project.Metadata with { Name = $"Name {index}" } });
        }

        for (var index = 0; index < 5; index++)
        {
            Assert.True(document.Undo());
        }

        Assert.False(document.CanUndo);
        Assert.Equal("Ceria", document.Project.Metadata.Name);
    }

    [Fact]
    public void MaxUndoDepthDropsTheOldestEntriesFirst()
    {
        var document = new ProjectDocument(CreateProject()) { MaxUndoDepth = 2 };

        for (var index = 1; index <= 3; index++)
        {
            document.Apply(
                $"Rename {index}",
                project => project with { Metadata = project.Metadata with { Name = $"Name {index}" } });
        }

        Assert.Equal(2, document.UndoStack.Length);
        Assert.Equal("Rename 2", document.UndoStack[0].Description);
        Assert.Equal("Rename 3", document.UndoStack[1].Description);

        Assert.True(document.Undo());
        Assert.Equal("Name 2", document.Project.Metadata.Name);
        Assert.True(document.Undo());
        Assert.Equal("Name 1", document.Project.Metadata.Name);
        Assert.False(document.CanUndo);
    }

    [Fact]
    public void CreateRecoveredUsesTheSnapshotAsTheDirtyCurrentValue()
    {
        var saved = CreateProject();
        var snapshot = saved.WithModifiedUtc(createdAt.AddMinutes(5));

        var document = ProjectDocument.CreateRecovered(snapshot, saved, maxUndoDepth: 200);

        Assert.Same(snapshot, document.Project);
        Assert.Same(saved, document.LastSavedProject);
        Assert.True(document.IsDirty);
        Assert.Equal(200, document.MaxUndoDepth);
        Assert.False(document.CanUndo);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void CreateRecoveredRejectsNullProjects()
    {
        Assert.Throws<ArgumentNullException>(
            () => ProjectDocument.CreateRecovered(CreateProject(), null!));
    }

    private static ProjectModel CreateProject()
    {
        return new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f"),
                new string('a', 64),
                "Ceria",
                null,
                createdAt,
                createdAt),
            new ModelDefinition(),
            [],
            [],
            [],
            ViewSettings.Default);
    }
}
