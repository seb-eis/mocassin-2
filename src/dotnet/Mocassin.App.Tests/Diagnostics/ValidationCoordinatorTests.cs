// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Diagnostics;
using Mocassin.App.ViewModels.Projects;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects;
using Mocassin.Packages.Projects.Templates;
using Mocassin.Packages.Projects.Validation;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.Diagnostics;

/// <summary>
/// Verifies the live validation coordinator: retained result, debounce, stale guard, and statistics caching.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ValidationCoordinatorTests
{
    private static readonly TimeSpan debounce = TimeSpan.FromMilliseconds(300);

    [Fact]
    public void ConstructionSeedsTheRetainedResultAndNeverRunsOnOpen()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;

        Assert.Same(session.Validation, validation.Result);
        Assert.Equal(0, validation.ValidationRunCount);
        Assert.Equal(0, validation.StatisticsRunCount);
        Assert.False(validation.IsStatisticsActive);
        Assert.Null(validation.Statistics);

        time.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(0, validation.ValidationRunCount);
        Assert.Equal(
            session.Validation!.IsValid ? ValidationState.Valid : ValidationState.Invalid,
            validation.State);
        Assert.Equal(session.Validation.Summary, validation.Summary);
    }

    [Fact]
    public async Task AnEditDebouncesAndAppliesOneRun()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;

        Rename(session, "Renamed");
        Assert.Equal(ValidationState.Validating, validation.State);
        Assert.Equal("Validating…", validation.StatusText);

        time.Advance(debounce - TimeSpan.FromMilliseconds(1));
        Assert.Equal(ValidationState.Validating, validation.State);
        Assert.Equal(0, validation.ValidationRunCount);

        time.Advance(TimeSpan.FromMilliseconds(1));
        await validation.WhenIdleAsync();

        Assert.Equal(1, validation.ValidationRunCount);
        Assert.NotNull(validation.Result);
        Assert.NotEqual(ValidationState.Validating, validation.State);
        Assert.NotNull(validation.LastCompletedUtc);
    }

    [Fact]
    public async Task RapidEditsCoalesceIntoOneRun()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;

        Rename(session, "First");
        Rename(session, "Second");
        time.Advance(debounce);
        await validation.WhenIdleAsync();

        Assert.Equal(1, validation.ValidationRunCount);
        Assert.NotNull(validation.Result);
    }

    [Fact]
    public void AStaleResultIsDiscarded()
    {
        var (session, _) = CreateSession();
        var validation = session.ValidationCoordinator;
        var initial = session.Validation!;
        var previousProject = session.Project;

        Rename(session, "Renamed");

        Assert.False(validation.TryApplyResult(previousProject, initial, runVersion: 0));
        Assert.Same(initial, validation.Result);
        Assert.False(validation.TryApplyResult(previousProject, initial, validation.Version));
        Assert.Same(initial, validation.Result);
    }

    [Fact]
    public void AResultFromAnotherProjectIsDiscarded()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;
        var otherProject = ProjectTemplateFactory.CreateProject(
            ProjectTemplateKind.Blank,
            "Other",
            time.GetUtcNow());
        var otherResult = new ProjectValidationPipeline().Validate(otherProject);

        Assert.False(validation.TryApplyResult(session.Project, otherResult, validation.Version));
        Assert.Same(session.Validation, validation.Result);
    }

    [Fact]
    public async Task RevalidateRunsImmediatelyWithoutTheDebounce()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;

        var pending = validation.RevalidateAsync();
        await pending;

        Assert.Equal(1, validation.ValidationRunCount);
        Assert.NotNull(validation.Result);
        Assert.NotEqual(ValidationState.Validating, validation.State);
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(1, validation.ValidationRunCount);
    }

    [Fact]
    public async Task StatisticsToggleRunsOnceAndReusesTheCachedResult()
    {
        var (session, _) = CreateSession();
        var validation = session.ValidationCoordinator;

        await validation.SetStatisticsActiveAsync(true);

        Assert.True(validation.IsStatisticsActive);
        Assert.Equal(1, validation.StatisticsRunCount);
        Assert.NotNull(validation.Statistics);
        var first = validation.Result;

        await validation.SetStatisticsActiveAsync(false);
        Assert.False(validation.IsStatisticsActive);
        Assert.Null(validation.Statistics);

        await validation.SetStatisticsActiveAsync(true);
        Assert.Equal(1, validation.StatisticsRunCount);
        Assert.Same(first, validation.Result);
        Assert.NotNull(validation.Statistics);
    }

    [Fact]
    public async Task StatisticsRerunAfterAContentChangeWhileActive()
    {
        var (session, time) = CreateSession();
        var validation = session.ValidationCoordinator;
        await validation.SetStatisticsActiveAsync(true);
        Assert.Equal(1, validation.StatisticsRunCount);

        Rename(session, "Renamed");
        time.Advance(debounce);
        await validation.WhenIdleAsync();

        Assert.Equal(2, validation.StatisticsRunCount);
        Assert.NotNull(validation.Statistics);
    }

    [Fact]
    public void LoadStageIsConditionalOnLoadDiagnostics()
    {
        var clean = CreateSession();
        Assert.Equal(ValidationStage.Load, clean.Session.Validation!.Stages[0].Stage);
        Assert.Equal(ValidationStageStatus.Skipped, clean.Session.Validation.Stages[0].Status);
        Assert.Equal(0, clean.Session.Validation.Stages[0].DiagnosticCount);

        var warning = new Diagnostic("PROJ999", DiagnosticSeverity.Warning, "Synthetic load warning.");
        var withWarning = CreateSession(loadDiagnostics: [warning]);
        Assert.Equal(ValidationStageStatus.Completed, withWarning.Session.Validation!.Stages[0].Status);
        Assert.Equal(1, withWarning.Session.Validation.Stages[0].DiagnosticCount);

        var error = new Diagnostic("PROJ999", DiagnosticSeverity.Error, "Synthetic load error.");
        var withError = CreateSession(loadDiagnostics: [error]);
        Assert.Equal(ValidationStageStatus.Failed, withError.Session.Validation!.Stages[0].Status);
        Assert.Equal(1, withError.Session.Validation.Stages[0].DiagnosticCount);
    }

    private static void Rename(ProjectSession session, string name) =>
        session.EditCoordinator.Apply(
            "Rename project",
            project => project with { Metadata = project.Metadata with { Name = name } });

    private static (ProjectSession Session, ManualTimeProvider Time) CreateSession(
        ProjectModel? project = null,
        ImmutableArray<Diagnostic>? loadDiagnostics = null)
    {
        var time = new ManualTimeProvider();
        var value = project
            ?? ProjectTemplateFactory.CreateProject(ProjectTemplateKind.Blank, "Validation", time.GetUtcNow());
        var session = ProjectSession.CreateFromLoad(
            new ProjectLoadResult(value, loadDiagnostics ?? ImmutableArray<Diagnostic>.Empty),
            "/tmp/validation.mocprj",
            null,
            isReadOnly: false,
            lockPath: null,
            new ProjectValidationPipeline(),
            time);
        return (session, time);
    }
}
