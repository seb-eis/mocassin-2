// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Model;
using Mocassin.Core.Project;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Editing;

/// <summary>
/// Pins the additive typed-operation overload of <see cref="ProjectDocument.Apply"/>.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectDocumentApplyOverloadTests
{
    private static readonly DateTimeOffset createdAt = DateTimeOffset.Parse(
        "2026-11-04T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void AcceptedTypedOperationRecordsOneChangeWithTheCallerDescription()
    {
        var original = CreateProject();
        var document = new ProjectDocument(original);

        var result = document.Apply(
            "Editor rename",
            static (editor, project) => editor.SetStructureName(project, "Renamed structure"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Change);
        var change = Assert.Single(document.UndoStack);
        Assert.Equal("Editor rename", change.Description);
        Assert.Same(original, change.Before);
        Assert.Same(document.Project, change.After);
        Assert.Equal("Renamed structure", document.Project.Structure.Name);
        Assert.True(document.IsDirty);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void RejectedTypedOperationLeavesTheDocumentAndEveryStackUntouched()
    {
        var document = new ProjectDocument(CreateProject());
        document.MarkSaved();

        var result = document.Apply(
            "Editor rename",
            static (editor, project) => editor.SetStructureName(project, " "));

        Assert.False(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Same(document.LastSavedProject, document.Project);
        Assert.Empty(document.UndoStack);
        Assert.Empty(document.RedoStack);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void RejectedComposedOperationReturnsTheDocumentProjectAndChangesNothing()
    {
        var original = CreateProject();
        var document = new ProjectDocument(original);

        var result = document.Apply(
            "Bulk rename",
            static (editor, project) =>
            {
                var first = editor.SetStructureName(project, "First");
                Assert.True(first.IsSuccess);
                var second = editor.SetStructureName(project, " ");
                Assert.False(second.IsSuccess);

                // A fold returns the original value on a mid-batch rejection, never the intermediate candidate.
                return new ProjectEditResult(project, null, second.Diagnostics);
            });

        Assert.False(result.IsSuccess);
        Assert.Same(original, result.Project);
        Assert.Same(original, document.Project);
        Assert.Empty(document.UndoStack);
        Assert.Empty(document.RedoStack);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void SuccessfulNoOpReturnsWithoutAStackChange()
    {
        var document = new ProjectDocument(CreateProject());

        var result = document.Apply(
            "No change",
            static (_, project) => new ProjectEditResult(project, null, []));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Empty(document.UndoStack);
        Assert.False(document.CanUndo);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void ComposedOperationRecordsOneCanonicalChangeFromTheDocumentToTheFinalValue()
    {
        var original = CreateProject();
        var document = new ProjectDocument(original);

        var result = document.Apply(
            "Bulk rename",
            static (editor, project) =>
            {
                var first = editor.SetStructureName(project, "First");
                Assert.True(first.IsSuccess);
                var second = editor.SetStructureName(first.Project, "Second");
                Assert.True(second.IsSuccess);
                return second;
            });

        Assert.True(result.IsSuccess);
        var change = Assert.Single(document.UndoStack);
        Assert.Equal("Bulk rename", change.Description);
        Assert.Same(original, change.Before);
        Assert.Same(document.Project, change.After);
        Assert.Equal("Second", document.Project.Structure.Name);

        Assert.True(document.Undo());
        Assert.Same(original, document.Project);
        Assert.False(document.IsDirty);
    }

    [Fact]
    public void MalformedResultWhoseChangeDoesNotEndAtTheResultProjectThrows()
    {
        var document = new ProjectDocument(CreateProject());

        var exception = Assert.Throws<InvalidOperationException>(
            () => document.Apply(
                "Malformed",
                static (_, project) => new ProjectEditResult(
                    project,
                    new ProjectChange("Typed", project, CreateProject()),
                    [])));

        Assert.Contains("result project", exception.Message, StringComparison.Ordinal);
        Assert.Empty(document.UndoStack);
    }

    [Fact]
    public void NullOperationAndEmptyDescriptionAreRejected()
    {
        var document = new ProjectDocument(CreateProject());

        Assert.Throws<ArgumentNullException>(
            () => document.Apply("Description", (Func<ProjectEditor, ProjectModel, ProjectEditResult>)null!));
        Assert.Throws<ArgumentException>(
            () => document.Apply(" ", static (_, project) => new ProjectEditResult(project, null, [])));
    }

    private static ProjectModel CreateProject() =>
        new(
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
