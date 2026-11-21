// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Mocassin.App.ViewModels.Editing;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.Projects.Validation;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// Per-session live validation over the shared <see cref="ProjectValidationPipeline"/>.
/// </summary>
/// <remarks>
/// The initial result is the one 23A produced at load/create/reload; the coordinator never runs the pipeline on
/// open. Content changes are debounced and validated off the UI thread over an immutable snapshot. A result is
/// applied only when it belongs to the current document version and the snapshot is still the document's project,
/// so a late result cannot overwrite a newer state. Statistics are an explicit, cached request, never part of the
/// open-time validation.
/// </remarks>
internal sealed partial class ValidationCoordinator : ObservableObject
{
    /// <summary>
    /// The default debounce applied after a content change.
    /// </summary>
    public static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(300);

    private readonly ProjectEditCoordinator editor;
    private readonly ProjectValidationPipeline pipeline;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan debounce;
    private readonly SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
    private readonly Dictionary<string, StatisticsCacheEntry> statisticsCache = new(StringComparer.Ordinal);
    private ImmutableArray<Diagnostic> loadDiagnostics = [];
    private CancellationTokenSource? pendingCancellation;
    private Task pendingWork = Task.CompletedTask;
    private long version;
    private ValidationResult? result;
    private DiagnosticSummary summary = new(0, 0, 0);
    private ValidationState state = ValidationState.NotValidated;
    private string statusText = "Not validated";
    private string statusGlyph = "○";
    private ModelStatistics? statistics;
    private bool isStatisticsActive;
    private DateTimeOffset? lastCompletedUtc;

    /// <summary>
    /// Initializes the coordinator over one project edit coordinator.
    /// </summary>
    /// <param name="editor">The session's single edit coordinator.</param>
    /// <param name="pipeline">The shared validation pipeline.</param>
    /// <param name="initialResult">The result retained by 23A, or <see langword="null"/> for a standalone document.</param>
    /// <param name="loadDiagnostics">The load diagnostics retained for the load stage of later runs.</param>
    /// <param name="timeProvider">The clock used by the debounce and timestamps.</param>
    /// <param name="debounce">The debounce delay; the default is <see cref="DefaultDebounce"/>.</param>
    public ValidationCoordinator(
        ProjectEditCoordinator editor,
        ProjectValidationPipeline pipeline,
        ValidationResult? initialResult,
        ImmutableArray<Diagnostic> loadDiagnostics,
        TimeProvider? timeProvider = null,
        TimeSpan? debounce = null)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
        this.pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.debounce = debounce ?? DefaultDebounce;
        this.editor.ProjectChanged += OnProjectChanged;
        Reseed(initialResult, loadDiagnostics);
    }

    /// <summary>
    /// Gets the current validation result, or <see langword="null"/> before the first completed run.
    /// </summary>
    public ValidationResult? Result => result;

    /// <summary>
    /// Gets the current diagnostic counts.
    /// </summary>
    public DiagnosticSummary Summary => summary;

    /// <summary>
    /// Gets the current validation state.
    /// </summary>
    public ValidationState State => state;

    /// <summary>
    /// Gets the status-bar text.
    /// </summary>
    public string StatusText => statusText;

    /// <summary>
    /// Gets the status glyph used together with the text and colour.
    /// </summary>
    public string StatusGlyph => statusGlyph;

    /// <summary>
    /// Gets the diagnostics of the current result, sorted by the pipeline.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics =>
        result is null ? ImmutableArray<Diagnostic>.Empty : result.Diagnostics;

    /// <summary>
    /// Gets a value indicating whether the current result has errors.
    /// </summary>
    public bool HasErrors => state == ValidationState.Invalid && summary.Errors > 0;

    /// <summary>
    /// Gets a value indicating whether the statistics panel is active.
    /// </summary>
    public bool IsStatisticsActive => isStatisticsActive;

    /// <summary>
    /// Gets the cached model statistics for the current snapshot, or <see langword="null"/>.
    /// </summary>
    public ModelStatistics? Statistics => statistics;

    /// <summary>
    /// Gets the complete-runtime text shown by the statistics panel.
    /// </summary>
    public string StatisticsText => statistics is null
        ? string.Empty
        : string.Create(
            CultureInfo.InvariantCulture,
            $"Particles: {statistics.ParticleCount} · Particle sets: {statistics.ParticleSetCount} · Reference sites: {statistics.ReferenceSiteCount}\n" +
            $"P1 positions: {statistics.P1PositionCount} ({statistics.StablePositionCount} stable, {statistics.UnstablePositionCount} unstable)\n" +
            $"KMC transitions: {statistics.KineticTransitionCount} · MMC transitions: {statistics.MetropolisTransitionCount} · Clusters: {statistics.ClusterCount}\n" +
            $"Pair tables: {statistics.EstimatedPairInteractionCount} · Unique cluster permutations: {statistics.EstimatedUniqueClusterPermutations}\n" +
            $"Estimated rules: {statistics.EstimatedRuleCount} · Estimated memory: {statistics.EstimatedMemoryBytes / (1024.0 * 1024.0):F1} MiB\n" +
            $"Supercell: {statistics.CellCountA}×{statistics.CellCountB}×{statistics.CellCountC} · Estimated positions: {statistics.EstimatedPositions}");

    /// <summary>
    /// Gets the statistics toggle label.
    /// </summary>
    public string StatisticsToggleText => isStatisticsActive ? "Hide statistics" : "Show statistics";

    /// <summary>
    /// Gets the time of the last applied result.
    /// </summary>
    public DateTimeOffset? LastCompletedUtc => lastCompletedUtc;

    /// <summary>
    /// Gets the last-completed timestamp text shown by the Problems channel.
    /// </summary>
    public string LastCompletedText => lastCompletedUtc is null
        ? "Not validated"
        : string.Create(
            CultureInfo.InvariantCulture,
            $"Validated {lastCompletedUtc.Value.ToUniversalTime():HH:mm:ss} UTC");

    /// <summary>
    /// Occurs when the result, state, statistics, or timestamp changes.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Gets the number of pipeline runs started by this coordinator (not open-time validation).
    /// </summary>
    internal int ValidationRunCount { get; private set; }

    /// <summary>
    /// Gets the number of statistics-enabled pipeline runs started by this coordinator.
    /// </summary>
    internal int StatisticsRunCount { get; private set; }

    /// <summary>
    /// Gets the current document version used by the stale guard.
    /// </summary>
    internal long Version => version;

    /// <summary>
    /// Gets the latest debounce or validation task, so tests and callers can wait for it to settle.
    /// </summary>
    internal Task WhenIdleAsync() => pendingWork;

    /// <summary>
    /// Replaces the retained initial result, for example after a reload or recovery.
    /// </summary>
    /// <param name="validation">The retained result, or <see langword="null"/>.</param>
    /// <param name="diagnostics">The new load diagnostics, when the caller has them.</param>
    public void Reseed(ValidationResult? validation, ImmutableArray<Diagnostic>? diagnostics = null)
    {
        CancelPending();
        version++;
        if (diagnostics is { } load)
        {
            loadDiagnostics = load.IsDefault ? ImmutableArray<Diagnostic>.Empty : load;
        }

        result = validation;
        summary = validation?.Summary ?? new DiagnosticSummary(0, 0, 0);
        lastCompletedUtc = validation is null ? null : timeProvider.GetUtcNow();
        state = validation switch
        {
            null => ValidationState.NotValidated,
            { IsValid: true } => ValidationState.Valid,
            _ => ValidationState.Invalid,
        };
        statistics = null;
        isStatisticsActive = false;
        statisticsCache.Clear();
        UpdateStatus();
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(IsStatisticsActive));
        OnPropertyChanged(nameof(StatisticsToggleText));
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(StatisticsText));
        OnPropertyChanged(nameof(LastCompletedUtc));
        OnPropertyChanged(nameof(LastCompletedText));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Runs validation immediately, outside the debounce, with the current statistics setting.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the run has settled.</returns>
    public Task RevalidateAsync(CancellationToken cancellationToken = default)
    {
        version++;
        CancelPending();
        pendingWork = RunNowAsync(version, isStatisticsActive, cancellationToken);
        return pendingWork;
    }

    /// <summary>
    /// Enables or disables the statistics panel and runs one cached statistics-enabled validation when needed.
    /// </summary>
    /// <param name="active">Whether the panel is active.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the toggle has settled.</returns>
    public async Task SetStatisticsActiveAsync(bool active, CancellationToken cancellationToken = default)
    {
        if (!active)
        {
            isStatisticsActive = false;
            statistics = null;
            UpdateStatus();
            OnPropertyChanged(nameof(IsStatisticsActive));
            OnPropertyChanged(nameof(StatisticsToggleText));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(StatisticsText));
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (isStatisticsActive)
        {
            return;
        }

        isStatisticsActive = true;
        version++;
        OnPropertyChanged(nameof(IsStatisticsActive));
        OnPropertyChanged(nameof(StatisticsToggleText));
        Changed?.Invoke(this, EventArgs.Empty);

        var current = editor.Document.Project;
        if (statisticsCache.TryGetValue(CacheKey(current), out var cached)
            && ReferenceEquals(cached.Snapshot, current))
        {
            ApplyStatisticsCache(cached);
            return;
        }

        CancelPending();
        pendingWork = RunNowAsync(version, includeStatistics: true, cancellationToken);
        await pendingWork;
    }

    /// <summary>
    /// Applies one completed result when it still belongs to the current document version and snapshot.
    /// </summary>
    /// <param name="snapshot">The project snapshot that produced the result.</param>
    /// <param name="validation">The completed result.</param>
    /// <param name="runVersion">The document version captured before the run.</param>
    /// <returns><see langword="true"/> when the result was applied.</returns>
    internal bool TryApplyResult(ProjectModel snapshot, ValidationResult validation, long runVersion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(validation);
        if (runVersion != version || !ReferenceEquals(snapshot, editor.Document.Project))
        {
            return false;
        }

        if (validation.ProjectId is { } projectId && projectId != snapshot.Metadata.ProjectId)
        {
            return false;
        }

        result = validation;
        summary = validation.Summary;
        state = validation.IsValid ? ValidationState.Valid : ValidationState.Invalid;
        if (validation.Statistics is not null)
        {
            statistics = validation.Statistics;
            statisticsCache[CacheKey(snapshot)] = new StatisticsCacheEntry(snapshot, validation);
        }

        lastCompletedUtc = timeProvider.GetUtcNow();
        UpdateStatus();
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(StatisticsText));
        OnPropertyChanged(nameof(LastCompletedUtc));
        OnPropertyChanged(nameof(LastCompletedText));
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args)
    {
        switch (args.Kind)
        {
            case ProjectChangeKind.Edited:
            case ProjectChangeKind.Undo:
            case ProjectChangeKind.Redo:
                version++;
                statistics = null;
                OnPropertyChanged(nameof(Statistics));
                OnPropertyChanged(nameof(StatisticsText));
                ScheduleAsync(version, isStatisticsActive);
                break;
            case ProjectChangeKind.Replaced:
                CancelPending();
                version++;
                UpdateStatus();
                OnPropertyChanged(nameof(Result));
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(Diagnostics));
                OnPropertyChanged(nameof(HasErrors));
                Changed?.Invoke(this, EventArgs.Empty);
                break;
            case ProjectChangeKind.Saved:
            default:
                break;
        }
    }

    private void ScheduleAsync(long runVersion, bool includeStatistics)
    {
        CancelPending();
        state = ValidationState.Validating;
        UpdateStatus();
        OnPropertyChanged(nameof(HasErrors));
        Changed?.Invoke(this, EventArgs.Empty);
        var cancellation = new CancellationTokenSource();
        pendingCancellation = cancellation;
        pendingWork = DebounceAndRunAsync(runVersion, includeStatistics, cancellation.Token);
    }

    private async Task DebounceAndRunAsync(long runVersion, bool includeStatistics, CancellationToken cancellationToken)
    {
        try
        {
            if (debounce > TimeSpan.Zero)
            {
                await Task.Delay(debounce, timeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RunNowAsync(runVersion, includeStatistics, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunNowAsync(long runVersion, bool includeStatistics, CancellationToken cancellationToken)
    {
        if (state != ValidationState.Validating)
        {
            state = ValidationState.Validating;
            UpdateStatus();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        var snapshot = editor.Document.Project;
        var load = loadDiagnostics;
        var options = includeStatistics ? new ValidationOptions { IncludeStatistics = true } : ValidationOptions.Default;
        ValidationRunCount++;
        if (includeStatistics)
        {
            StatisticsRunCount++;
        }

        ValidationResult validation;
        try
        {
            validation = await Task.Run(
                    () => pipeline.Validate(snapshot, load, options, compiledModel: null, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await DispatchAsync(() => TryApplyResult(snapshot, validation, runVersion)).ConfigureAwait(false);
    }

    private Task DispatchAsync(Action action)
    {
        if (synchronizationContext is null || SynchronizationContext.Current == synchronizationContext)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        synchronizationContext.Post(
            _ =>
            {
                try
                {
                    action();
                    completion.SetResult();
                }
#pragma warning disable CA1031 // The dispatched UI action is arbitrary; its failure must reach the awaiting task.
                catch (Exception exception)
#pragma warning restore CA1031
                {
                    completion.SetException(exception);
                }
            },
            null);
        return completion.Task;
    }

    private void ApplyStatisticsCache(StatisticsCacheEntry cached)
    {
        result = cached.Validation;
        summary = cached.Validation.Summary;
        state = cached.Validation.IsValid ? ValidationState.Valid : ValidationState.Invalid;
        statistics = cached.Validation.Statistics;
        UpdateStatus();
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Diagnostics));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(StatisticsText));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void CancelPending()
    {
        pendingCancellation?.Cancel();
        pendingCancellation?.Dispose();
        pendingCancellation = null;
    }

    private void UpdateStatus()
    {
        statusText = state switch
        {
            ValidationState.NotValidated => "Not validated",
            ValidationState.Validating => "Validating…",
            ValidationState.Valid => "Valid",
            _ => string.Create(
                CultureInfo.InvariantCulture,
                $"{summary.Errors} errors, {summary.Warnings} warnings, {summary.Infos} infos"),
        };
        statusGlyph = state switch
        {
            ValidationState.NotValidated => "○",
            ValidationState.Validating => "…",
            ValidationState.Valid => "✔",
            _ => summary.Errors > 0 ? "✖" : "⚠",
        };
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusGlyph));
        OnPropertyChanged(nameof(HasErrors));
    }

    private static string CacheKey(ProjectModel project) =>
        project.Metadata.ProjectId.ToString("D", CultureInfo.InvariantCulture);

    private sealed record StatisticsCacheEntry(ProjectModel Snapshot, ValidationResult Validation);
}
