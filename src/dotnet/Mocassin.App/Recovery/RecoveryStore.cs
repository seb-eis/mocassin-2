// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocassin.App.Composition;
using Mocassin.App.ViewModels.Recovery;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Recovery;

/// <summary>
/// Application-local recovery snapshots and manifests in the app-data <c>recovery/</c> directory.
/// </summary>
internal sealed partial class RecoveryStore : IRecoveryStore
{
    private const string format = "mocassin.recovery";
    private const int version = 1;
    private const long maxManifestBytes = 64L * 1024L;
    private const int maxEntries = 100;

    private readonly AppPaths paths;
    private readonly ProjectStore store;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RecoveryStore> logger;

    /// <summary>
    /// Initializes the recovery store.
    /// </summary>
    /// <param name="paths">The application-data paths.</param>
    /// <param name="store">The project store used for the atomic snapshot write.</param>
    /// <param name="timeProvider">The clock used for the manifest timestamp.</param>
    /// <param name="logger">The optional logger for ignored or unreadable recovery files.</param>
    public RecoveryStore(
        AppPaths paths,
        ProjectStore store,
        TimeProvider timeProvider,
        ILogger<RecoveryStore>? logger = null)
    {
        this.paths = paths;
        this.store = store;
        this.timeProvider = timeProvider;
        this.logger = logger ?? NullLogger<RecoveryStore>.Instance;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecoveryCandidate>> ListAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.RecoveryDirectory);
        var candidates = new List<RecoveryCandidate>();
        foreach (var manifestPath in Directory.EnumerateFiles(paths.RecoveryDirectory, "*.json", SearchOption.TopDirectoryOnly)
                     .Take(maxEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(manifestPath);
                if (info.Length is <= 0 or > maxManifestBytes)
                {
                    LogIgnoredManifest(logger, manifestPath, "its size is outside the accepted range");
                    continue;
                }

                var bytes = await File.ReadAllBytesAsync(manifestPath, cancellationToken).ConfigureAwait(false);
                var manifest = JsonSerializer.Deserialize(bytes, RecoveryManifestJsonContext.Default.RecoveryManifest);
                if (manifest is null
                    || !string.Equals(manifest.Format, format, StringComparison.Ordinal)
                    || manifest.Version != version
                    || manifest.ProjectId == Guid.Empty
                    || string.IsNullOrWhiteSpace(manifest.ProjectPath)
                    || !Enum.TryParse<RecoverySource>(manifest.Source, ignoreCase: true, out var source))
                {
                    LogIgnoredManifest(logger, manifestPath, "its manifest fields are invalid");
                    continue;
                }

                var snapshotPath = GetSnapshotPath(manifest.ProjectId);
                if (!File.Exists(snapshotPath))
                {
                    LogIgnoredManifest(logger, manifestPath, "its snapshot is missing");
                    continue;
                }

                candidates.Add(new RecoveryCandidate(
                    manifest.ProjectId,
                    manifest.ProjectPath,
                    snapshotPath,
                    manifest.SavedUtc.ToUniversalTime(),
                    source));
            }
            catch (IOException exception)
            {
                // A locked manifest is ignored; the sweep leaves it alone.
                LogUnreadableManifest(logger, manifestPath, exception);
            }
            catch (UnauthorizedAccessException exception)
            {
                // Leave files the process cannot read.
                LogUnreadableManifest(logger, manifestPath, exception);
            }
            catch (JsonException exception)
            {
                // A malformed manifest is ignored.
                LogUnreadableManifest(logger, manifestPath, exception);
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.SavedUtc)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<RecoveryWriteResult> WriteAsync(
        ProjectModel project,
        string projectPath,
        RecoverySource source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        Directory.CreateDirectory(paths.RecoveryDirectory);
        var projectId = project.Metadata.ProjectId;
        var snapshotBase = GetSnapshotBase(projectId);
        var save = await store.WriteAutosaveAsync(project, snapshotBase, cancellationToken).ConfigureAwait(false);
        if (!save.IsSuccess)
        {
            return new RecoveryWriteResult(false, null, save.Diagnostics);
        }

        var snapshotPath = snapshotBase + ".autosave";
        var manifest = new RecoveryManifest(
            format,
            version,
            projectId,
            projectPath,
            timeProvider.GetUtcNow(),
            source.ToString());
        var manifestPath = GetManifestPath(projectId);
        var temporary = Path.Combine(paths.RecoveryDirectory, $".{projectId:D}.json.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporary,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                        stream,
                        manifest,
                        RecoveryManifestJsonContext.Default.RecoveryManifest,
                        cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, manifestPath, overwrite: true);
            return new RecoveryWriteResult(true, snapshotPath, ImmutableArray<Diagnostic>.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteQuietly(temporary);
            DeleteQuietly(snapshotPath);
            LogWriteFailure(logger, projectPath, exception);
            return new RecoveryWriteResult(
                false,
                null,
                [new Diagnostic(
                    ProjectDiagnosticCodes.AutosaveFailed,
                    DiagnosticSeverity.Error,
                    $"Could not write the recovery manifest '{manifestPath}': {exception.Message}")]);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(RecoveryCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsInsideRecovery(candidate.SnapshotPath))
        {
            return Task.FromResult(false);
        }

        var deleted = DeleteQuietly(candidate.SnapshotPath);
        deleted |= DeleteQuietly(GetManifestPath(candidate.ProjectId));
        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public async Task<int> SweepAsync(
        DateTimeOffset now,
        TimeSpan retention,
        CancellationToken cancellationToken = default)
    {
        var candidates = await ListAsync(cancellationToken).ConfigureAwait(false);
        var deleted = 0;
        foreach (var candidate in candidates)
        {
            if (!RecoveryRetentionPolicy.IsExpired(candidate.SavedUtc, now, retention))
            {
                continue;
            }

            if (await DeleteAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
                deleted++;
            }
        }

        return deleted;
    }

    private string GetSnapshotBase(Guid projectId) =>
        Path.Combine(paths.RecoveryDirectory, projectId.ToString("D", CultureInfo.InvariantCulture) + ".mocprj");

    private string GetSnapshotPath(Guid projectId) => GetSnapshotBase(projectId) + ".autosave";

    private string GetManifestPath(Guid projectId) =>
        Path.Combine(paths.RecoveryDirectory, projectId.ToString("D", CultureInfo.InvariantCulture) + ".json");

    private bool IsInsideRecovery(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(paths.RecoveryDirectory) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private bool DeleteQuietly(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
        catch (IOException exception)
        {
            LogDeleteFailure(logger, path, exception);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            LogDeleteFailure(logger, path, exception);
            return false;
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping recovery manifest '{ManifestPath}': {Reason}.")]
    private static partial void LogIgnoredManifest(ILogger logger, string manifestPath, string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not read recovery manifest '{ManifestPath}'.")]
    private static partial void LogUnreadableManifest(ILogger logger, string manifestPath, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not write the recovery snapshot for '{ProjectPath}'.")]
    private static partial void LogWriteFailure(ILogger logger, string projectPath, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not delete the recovery files at '{SnapshotPath}'.")]
    private static partial void LogDeleteFailure(ILogger logger, string snapshotPath, Exception exception);
}
