// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Documents.Project;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Documents;

/// <summary>
/// Verifies the Overview metadata form, inline errors, save-point text, statistics, and navigation focus.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProjectOverviewMetadataTests
{
    [Fact]
    public void EachCommittedFieldAddsOneUndoEntryAndUndoRefreshesTheDrafts()
    {
        var (session, overview) = Create();

        overview.NameDraft = "Renamed";
        overview.CommitNameCommand.Execute(null);
        overview.DescriptionDraft = "Description";
        overview.CommitDescriptionCommand.Execute(null);
        overview.AuthorDraft = "Author";
        overview.CommitAuthorCommand.Execute(null);
        overview.NotesDraft = "Notes";
        overview.CommitNotesCommand.Execute(null);

        Assert.Equal("Renamed", session.Name);
        Assert.Equal("Description", session.Project.Metadata.Description);
        Assert.Equal("Author", session.Project.Metadata.Author);
        Assert.Equal("Notes", session.Project.Metadata.Notes);
        Assert.Equal(4, session.Document.UndoStack.Length);
        Assert.Equal(4, session.Document.UndoStack.Select(change => change.Description).Distinct().Count());
        Assert.True(overview.IsDirty);

        Assert.True(session.EditCoordinator.Undo());
        Assert.Equal(string.Empty, overview.NotesDraft);
        Assert.Equal("Author", overview.AuthorDraft);
        Assert.True(overview.IsDirty);

        Assert.True(session.EditCoordinator.Undo());
        Assert.Equal(string.Empty, overview.AuthorDraft);
        Assert.Equal("Description", overview.DescriptionDraft);
    }

    [Fact]
    public void AnInvalidNameIsRejectedInlineAndLeavesTheProjectUnchanged()
    {
        var (session, overview) = Create();

        overview.NameDraft = "Bad/Name";
        overview.CommitNameCommand.Execute(null);

        Assert.True(overview.HasNameError);
        Assert.False(string.IsNullOrWhiteSpace(overview.NameError));
        Assert.Equal("Metadata", session.Name);
        Assert.False(session.EditCoordinator.CanUndo);
        Assert.Equal("Bad/Name", overview.NameDraft);

        overview.NameDraft = string.Empty;
        overview.CommitNameCommand.Execute(null);
        Assert.True(overview.HasNameError);
        Assert.Equal("Metadata", session.Name);
    }

    [Fact]
    public void AnOverlongNameIsRejectedInline()
    {
        var (session, overview) = Create();

        overview.NameDraft = new string('a', ProjectPathRules.MaxNameLength + 1);
        overview.CommitNameCommand.Execute(null);

        Assert.True(overview.HasNameError);
        Assert.Equal("Metadata", session.Name);
    }

    [Fact]
    public void EmptyOptionalFieldsClearToNull()
    {
        var (session, overview) = Create();
        overview.DescriptionDraft = "Kept";
        overview.CommitDescriptionCommand.Execute(null);
        Assert.Equal("Kept", session.Project.Metadata.Description);

        overview.DescriptionDraft = "   ";
        overview.CommitDescriptionCommand.Execute(null);

        Assert.Null(session.Project.Metadata.Description);
        Assert.Equal(string.Empty, overview.DescriptionDraft);
    }

    [Fact]
    public void AReadOnlySessionRefusesTheEditWithTheAppMessage()
    {
        var (session, overview) = Create(isReadOnly: true);
        Assert.False(overview.CanEdit);
        Assert.True(overview.IsReadOnly);

        overview.NameDraft = "Changed";
        overview.CommitNameCommand.Execute(null);

        Assert.Equal(ProjectEditCoordinator.ReadOnlyMessage, overview.InlineError);
        Assert.Equal("Metadata", session.Name);
        Assert.Empty(session.Document.UndoStack);
        Assert.False(overview.IsDirty);
    }

    [Fact]
    public void ARejectedEditShowsTheInlineErrorAndKeepsTheDraft()
    {
        var (session, overview) = Create();
        overview.NameDraft = "Renamed";
        overview.CommitNameCommand.Execute(null);
        Assert.Null(overview.NameError);

        session.EditCoordinator.Apply(
            "Break the seed",
            project => project with { Metadata = project.Metadata with { MasterSeed = "bad" } });

        Assert.True(overview.HasInlineError);
        Assert.Contains("Master seed", overview.InlineError, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSavePointUpdatesTheDirtyText()
    {
        var (session, overview) = Create();

        overview.NameDraft = "Saved";
        overview.CommitNameCommand.Execute(null);
        Assert.True(overview.IsDirty);
        Assert.Equal("Unsaved changes", overview.DirtyText);

        session.Document.MarkSaved();
        session.EditCoordinator.NotifySaved();

        Assert.False(overview.IsDirty);
        Assert.Equal("Saved", overview.DirtyText);

        Assert.True(session.EditCoordinator.Undo());
        Assert.True(overview.IsDirty);
        Assert.True(session.EditCoordinator.Redo());
        Assert.False(overview.IsDirty);
    }

    [Fact]
    public async Task StatisticsAreNotRequestedOnOpenAndToggleOncePerSnapshot()
    {
        var (_, overview) = Create();
        Assert.False(overview.Validation.IsStatisticsActive);
        Assert.Equal(0, overview.Validation.StatisticsRunCount);
        Assert.Null(overview.Validation.Statistics);

        await overview.ToggleStatisticsCommand.ExecuteAsync(null);
        Assert.True(overview.Validation.IsStatisticsActive);
        Assert.Equal(1, overview.Validation.StatisticsRunCount);
        Assert.NotNull(overview.Validation.Statistics);
        Assert.Contains("Particles:", overview.Validation.StatisticsText, StringComparison.Ordinal);
        Assert.Contains("Supercell:", overview.Validation.StatisticsText, StringComparison.Ordinal);

        await overview.ToggleStatisticsCommand.ExecuteAsync(null);
        Assert.False(overview.Validation.IsStatisticsActive);

        await overview.ToggleStatisticsCommand.ExecuteAsync(null);
        Assert.True(overview.Validation.IsStatisticsActive);
        Assert.Equal(1, overview.Validation.StatisticsRunCount);
        Assert.NotNull(overview.Validation.Statistics);
    }

    [Fact]
    public void FocusRequestsAreConsumedOnce()
    {
        var (_, overview) = Create();

        overview.RequestFocus("notes");
        Assert.Equal("notes", overview.PendingFocus);
        Assert.Equal("notes", overview.ConsumePendingFocus());
        Assert.Null(overview.PendingFocus);
    }

    [Fact]
    public async Task MetadataNavigationFocusesTheExistingOverview()
    {
        var (session, overview) = Create();
        var workspace = new WorkspaceViewModel();
        var entry = new ProjectWorkspaceEntry(session);
        workspace.Add(entry);
        var documents = new DocumentCollectionViewModel();
        documents.OpenOrActivate(overview);
        var provider = new ProjectNavigationTargetProvider(
            workspace,
            documents,
            new RecordingClipboardService());

        var byAuthor = await provider.CreateAsync(
            new ModelPath("project", session.Id, "author"),
            NavigationIntent.SelectObject,
            CancellationToken.None);
        var authorOverview = Assert.IsType<ProjectOverviewViewModel>(byAuthor);
        Assert.Same(overview, authorOverview);
        Assert.Equal("author", authorOverview.PendingFocus);

        await provider.CreateAsync(
            new ModelPath("project", session.Id, "name"),
            NavigationIntent.SelectObject,
            CancellationToken.None);
        Assert.Equal("name", overview.PendingFocus);
    }

    [Fact]
    public void TheValidationBannerReflectsLoadErrors()
    {
        var (_, overview) = Create(
            loadDiagnostics: [new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Synthetic load error.")]);

        Assert.Equal(ValidationState.Invalid, overview.Validation.State);
        Assert.True(overview.Validation.HasErrors);
        Assert.Contains("1 errors", overview.Validation.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void TheJsonDocumentRefreshesAfterAMetadataEdit()
    {
        var (session, overview) = Create();
        var entry = new ProjectWorkspaceEntry(session);
        var json = new ProjectJsonViewModel(entry, new RecordingClipboardService());
        var before = json.JsonText;

        overview.NameDraft = "Renamed";
        overview.CommitNameCommand.Execute(null);

        Assert.NotEqual(before, json.JsonText);
        Assert.Contains("Renamed", json.JsonText, StringComparison.Ordinal);
    }

    private static (ProjectSession Session, ProjectOverviewViewModel Overview) Create(
        bool isReadOnly = false,
        ImmutableArray<Diagnostic>? loadDiagnostics = null)
    {
        var time = new ManualTimeProvider();
        var project = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, "Metadata", time.GetUtcNow());
        var session = ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, loadDiagnostics ?? ImmutableArray<Diagnostic>.Empty),
            "/tmp/metadata.mocprj",
            null,
            isReadOnly,
            lockPath: null,
            new ProjectValidationPipeline(),
            time);
        var entry = new ProjectWorkspaceEntry(session);
        var overview = new ProjectOverviewViewModel(entry, new RecordingClipboardService());
        return (session, overview);
    }
}
