// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Consoles;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Documents;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Navigation;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;

namespace Mocassin.App.Tests.Diagnostics;

/// <summary>
/// Verifies the Problems channel: active-project scope, filters, sorting, edit diagnostics, navigation, and help.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ProblemsChannelViewModelTests
{
    [Fact]
    public void TheChannelShowsOnlyTheActiveProjectsDiagnostics()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Load warning."),
                new Diagnostic("PROJ011", DiagnosticSeverity.Info, "Load information."),
            ]);

        Assert.True(context.Channel.HasProject);
        Assert.Equal(3, context.Channel.Items.Count);
        Assert.True(context.Channel.HasItems);
        Assert.Equal(DiagnosticSeverity.Error, context.Channel.Items[0].Severity);
        Assert.Contains("Validation —", context.Channel.HeaderText, StringComparison.Ordinal);
        Assert.Equal("1 errors, 1 warnings, 1 infos", context.Channel.SummaryText);
    }

    [Fact]
    public void FiltersApplyToSeverityCodeSourceAndText()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Load warning."),
                new Diagnostic("PROJ011", DiagnosticSeverity.Info, "Load information."),
            ]);

        context.Channel.SeverityOption = context.Channel.SeverityOptions.Single(
            option => option.Severity == DiagnosticSeverity.Error);
        Assert.Single(context.Channel.Items);
        Assert.Equal("PROJ001", context.Channel.Items[0].Code);

        context.Channel.SeverityOption = context.Channel.SeverityOptions[0];
        context.Channel.CodeFilter = "PROJ034";
        Assert.Single(context.Channel.Items);

        context.Channel.CodeFilter = string.Empty;
        context.Channel.TextFilter = "information";
        Assert.Single(context.Channel.Items);

        context.Channel.TextFilter = string.Empty;
        context.Channel.SourceFilter = "edit";
        Assert.Empty(context.Channel.Items);
        context.Channel.SourceFilter = "All";
        Assert.Equal(3, context.Channel.Items.Count);
    }

    [Fact]
    public void SortingChangesTheRowOrder()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Load warning."),
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ011", DiagnosticSeverity.Info, "Load information."),
            ]);

        context.Channel.SortMode = ProblemsSortMode.Code;
        context.Channel.SortDescending = false;
        Assert.Equal(
            ["PROJ001", "PROJ011", "PROJ034"],
            context.Channel.Items.Select(item => item.Code).ToArray());

        context.Channel.SortDescending = true;
        Assert.Equal(
            ["PROJ034", "PROJ011", "PROJ001"],
            context.Channel.Items.Select(item => item.Code).ToArray());
    }

    [Fact]
    public void EditDiagnosticsAreAddedAndClearedByASuccessfulEdit()
    {
        var context = CreateContext([]);
        var rejected = context.Session.EditCoordinator.Apply(
            "Break the seed",
            project => project with { Metadata = project.Metadata with { MasterSeed = "bad" } });

        Assert.Equal(EditApplyOutcome.Rejected, rejected.Outcome);
        Assert.Contains(context.Channel.Items, item => item.Source == "edit");

        Rename(context.Session, "Renamed");
        Assert.DoesNotContain(context.Channel.Items, item => item.Source == "edit");
    }

    [Fact]
    public void SwitchingTheActiveProjectReplacesTheList()
    {
        var first = CreateContext(
            [new Diagnostic("PROJ001", DiagnosticSeverity.Error, "First project error.")]);
        var second = CreateSession(
            "Second",
            [new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Second project warning.")]);
        var secondEntry = new ProjectWorkspaceEntry(second.Session);
        first.Workspace.Add(secondEntry);
        first.Workspace.Activate(secondEntry);

        Assert.Contains("Second", first.Channel.HeaderText, StringComparison.Ordinal);
        var item = Assert.Single(first.Channel.Items);
        Assert.Equal("PROJ034", item.Code);
        Assert.DoesNotContain(first.Channel.Items, row => row.Code == "PROJ001");
    }

    [Fact]
    public void NoProjectShowsTheEmptyState()
    {
        var workspace = new WorkspaceViewModel();
        var documents = new DocumentCollectionViewModel();
        var console = new ConsoleService(new InlineUiDispatcher(), new ManualTimeProvider());
        var navigation = new NavigationService(
            documents,
            workspace,
            console,
            [new ProjectNavigationTargetProvider(workspace, documents, new RecordingClipboardService())]);
        var channel = new ProblemsChannelViewModel(workspace, navigation, new RecordingShellLauncher());

        Assert.False(channel.HasProject);
        Assert.Empty(channel.Items);
        Assert.Equal("Open a project to see validation problems.", channel.EmptyText);
    }

    [Fact]
    public async Task NavigatingAProjectDiagnosticOpensTheOverviewAndFocusesTheField()
    {
        var context = CreateContext(
            [
                new Diagnostic(
                    "PROJ001",
                    DiagnosticSeverity.Error,
                    "Project name problem.",
                    new ModelPath("project", Guid.Empty, "name")),
            ]);
        var item = Assert.Single(context.Channel.Items);

        await item.NavigateCommand.ExecuteAsync(null);

        var overview = Assert.IsType<Mocassin.App.ViewModels.Documents.Project.ProjectOverviewViewModel>(
            context.Documents.ActiveDocument);
        Assert.Equal("name", overview.PendingFocus);
    }

    [Fact]
    public async Task AnUnknownDomainProducesTheNavigationNotice()
    {
        var context = CreateContext(
            [
                new Diagnostic(
                    "PROJ001",
                    DiagnosticSeverity.Error,
                    "Particle problem.",
                    new ModelPath("particle", Guid.NewGuid())),
            ]);
        var item = Assert.Single(context.Channel.Items);

        await item.NavigateCommand.ExecuteAsync(null);

        Assert.Contains(
            context.Console.ApplicationChannel.VisibleEntries,
            entry => entry.Message.Contains("particle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RelatedPathsAreOfferedAndNavigate()
    {
        var context = CreateContext(
            [
                new Diagnostic(
                    "PROJ001",
                    DiagnosticSeverity.Error,
                    "Project name problem.",
                    new ModelPath("project", Guid.Empty, "name"),
                    related: [new ModelPath("project", Guid.Empty, "author")]),
            ]);
        var item = Assert.Single(context.Channel.Items);
        var related = Assert.Single(item.RelatedItems);

        await related.NavigateCommand.ExecuteAsync(null);

        var overview = Assert.IsType<Mocassin.App.ViewModels.Documents.Project.ProjectOverviewViewModel>(
            context.Documents.ActiveDocument);
        Assert.Equal("author", overview.PendingFocus);
    }

    [Fact]
    public async Task ValidationRowsRefreshAfterAnEditAndUndo()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Load warning."),
            ]);

        Rename(context.Session, "Renamed");
        context.Time.Advance(TimeSpan.FromMilliseconds(300));
        await context.Session.ValidationCoordinator.WhenIdleAsync();
        Assert.Equal(1, context.Session.ValidationCoordinator.ValidationRunCount);
        Assert.Equal(2, context.Channel.Items.Count);

        Assert.True(context.Session.EditCoordinator.Undo());
        context.Time.Advance(TimeSpan.FromMilliseconds(300));
        await context.Session.ValidationCoordinator.WhenIdleAsync();
        Assert.Equal(2, context.Session.ValidationCoordinator.ValidationRunCount);
        Assert.Equal(2, context.Channel.Items.Count);
    }

    [Fact]
    public void ClearingFiltersRestoresEveryRow()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ034", DiagnosticSeverity.Warning, "Load warning."),
            ]);
        context.Channel.CodeFilter = "PROJ001";
        context.Channel.SourceFilter = "edit";
        context.Channel.SortMode = ProblemsSortMode.Message;
        context.Channel.SortDescending = false;
        Assert.Empty(context.Channel.Items);

        context.Channel.ClearFiltersCommand.Execute(null);

        Assert.Equal(2, context.Channel.Items.Count);
        Assert.Equal(string.Empty, context.Channel.CodeFilter);
        Assert.Equal("All", context.Channel.SourceFilter);
        Assert.Equal(ProblemsSortMode.Severity, context.Channel.SortMode);
        Assert.True(context.Channel.SortDescending);
    }

    [Fact]
    public async Task HelpOpensTheDocumentedAnchorAndUnknownCodesStayUnlinked()
    {
        var context = CreateContext(
            [
                new Diagnostic("PROJ001", DiagnosticSeverity.Error, "Load error."),
                new Diagnostic("PROJ999", DiagnosticSeverity.Warning, "Undocumented warning."),
            ]);
        var known = context.Channel.Items.Single(item => item.Code == "PROJ001");
        var unknown = context.Channel.Items.Single(item => item.Code == "PROJ999");

        Assert.True(known.HasHelp);
        Assert.False(unknown.HasHelp);
        await known.OpenHelpCommand.ExecuteAsync(null);
        Assert.Contains(DiagnosticHelpLinks.Resolve("PROJ001")!, context.Launcher.Uris);
    }

    private static void Rename(ProjectSession session, string name) =>
        session.EditCoordinator.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = name } });

    private static TestContext CreateContext(ImmutableArray<Diagnostic> diagnostics)
    {
        var session = CreateSession("Problems", diagnostics);
        var workspace = new WorkspaceViewModel();
        workspace.Add(new ProjectWorkspaceEntry(session.Session));
        var documents = new DocumentCollectionViewModel();
        var console = new ConsoleService(new InlineUiDispatcher(), session.Time);
        var navigation = new NavigationService(
            documents,
            workspace,
            console,
            [new ProjectNavigationTargetProvider(workspace, documents, new RecordingClipboardService())]);
        var launcher = new RecordingShellLauncher();
        var channel = new ProblemsChannelViewModel(workspace, navigation, launcher);
        return new TestContext(session.Session, session.Time, workspace, documents, console, launcher, channel);
    }

    private static SessionContext CreateSession(string name, ImmutableArray<Diagnostic> diagnostics)
    {
        var time = new ManualTimeProvider();
        var project = ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, name, time.GetUtcNow());
        var session = ProjectSession.CreateFromLoad(
            new ProjectLoadResult(project, diagnostics),
            $"/tmp/{name}.mocprj",
            null,
            isReadOnly: false,
            lockPath: null,
            new ProjectValidationPipeline(),
            time);
        return new SessionContext(session, time);
    }

    private sealed record SessionContext(ProjectSession Session, ManualTimeProvider Time);

    private sealed record TestContext(
        ProjectSession Session,
        ManualTimeProvider Time,
        WorkspaceViewModel Workspace,
        DocumentCollectionViewModel Documents,
        ConsoleService Console,
        RecordingShellLauncher Launcher,
        ProblemsChannelViewModel Channel);
}
