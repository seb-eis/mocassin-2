// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects;

/// <summary>
/// Loads and saves versioned <c>.mocprj</c> files with atomic replacement, locking, and autosave support.
/// </summary>
public sealed class ProjectStore
{
    private static readonly UTF8Encoding strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private readonly TimeProvider timeProvider;
    private readonly ProjectStoreOptions options;
    private readonly IProjectFileWriter fileWriter;
    private readonly Func<Project, ImmutableArray<Diagnostic>> validator;

    /// <summary>
    /// Initializes a project store.
    /// </summary>
    /// <param name="timeProvider">Optional time provider used for timestamps and lock-age checks.</param>
    /// <param name="options">Optional store configuration.</param>
    public ProjectStore(TimeProvider? timeProvider = null, ProjectStoreOptions? options = null)
        : this(timeProvider ?? TimeProvider.System, options ?? new ProjectStoreOptions(), new AtomicProjectFileWriter())
    {
    }

    /// <summary>
    /// Initializes a project store with the default composed validator.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="options">The store configuration.</param>
    /// <param name="fileWriter">The atomic file writer.</param>
    internal ProjectStore(TimeProvider timeProvider, ProjectStoreOptions options, IProjectFileWriter fileWriter)
        : this(timeProvider, options, fileWriter, ProjectValidationService.Validate)
    {
    }

    /// <summary>
    /// Initializes a project store with an explicit project validator.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="options">The store configuration.</param>
    /// <param name="fileWriter">The atomic file writer.</param>
    /// <param name="validator">The validator used after loading.</param>
    internal ProjectStore(
        TimeProvider timeProvider,
        ProjectStoreOptions options,
        IProjectFileWriter fileWriter,
        Func<Project, ImmutableArray<Diagnostic>> validator)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));

        if (this.options.MaxDocumentBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDocumentBytes must be positive.");
        }

        if (this.options.StaleLockTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "StaleLockTimeout must be positive.");
        }

        if (this.options.AutosaveInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "AutosaveInterval must be positive.");
        }
    }

    /// <summary>
    /// Gets the store configuration.
    /// </summary>
    public ProjectStoreOptions Options => options;

    /// <summary>
    /// Gets the lock-file path for a project path.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The absolute lock-file path.</returns>
    public static string GetLockPath(string projectPath) => GetFullPath(projectPath) + ".lock";

    /// <summary>
    /// Gets the autosave-file path for a project path.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The absolute autosave-file path.</returns>
    public static string GetAutosavePath(string projectPath) => GetFullPath(projectPath) + ".autosave";

    /// <summary>
    /// Gets the optional backup-file path for a project path.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <returns>The absolute backup-file path.</returns>
    public static string GetBackupPath(string projectPath) => GetFullPath(projectPath) + ".bak";

    /// <summary>
    /// Loads, migrates, deserializes, and validates a project file.
    /// </summary>
    /// <param name="path">The project file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The load result. Parsing failures return no project; validation errors still return the inspected project.</returns>
    public Task<ProjectLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default) =>
        LoadFromPathAsync(GetFullPath(path), cancellationToken);

    /// <summary>
    /// Loads an autosave recovery file without replacing the main project file.
    /// </summary>
    /// <param name="projectPath">The main project path used to derive the autosave path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The load result.</returns>
    public Task<ProjectLoadResult> LoadAutosaveAsync(string projectPath, CancellationToken cancellationToken = default) =>
        LoadFromPathAsync(GetAutosavePath(projectPath), cancellationToken);

    /// <summary>
    /// Saves a project through a temporary file and an atomic replacement, updating the modified timestamp.
    /// </summary>
    /// <param name="project">The project to save.</param>
    /// <param name="path">The project file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The save result.</returns>
    public async Task<ProjectSaveResult> SaveAsync(
        Project project,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var diagnostics = new DiagnosticBag();

        if (project.SchemaVersion != ProjectSchema.CurrentVersion)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Cannot save project schema version {project.SchemaVersion}; expected {ProjectSchema.CurrentVersion}."));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }

        var fullPath = GetFullPath(path);
        var savedProject = project.WithModifiedUtc(timeProvider.GetUtcNow());
        byte[] bytes;
        try
        {
            bytes = ProjectJson.SerializeToUtf8Bytes(savedProject, indented: true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.SaveFailed,
                $"Could not serialize project '{fullPath}': {exception.Message}"));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }

        try
        {
            await fileWriter.WriteAtomicallyAsync(fullPath, bytes, options.CreateBackup, cancellationToken)
                .ConfigureAwait(false);
            return new ProjectSaveResult(savedProject, diagnostics.Sorted);
        }
        catch (Exception exception) when (IsIoFailure(exception) && exception is not OperationCanceledException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.SaveFailed,
                $"Could not save project '{fullPath}': {exception.Message}"));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }
    }

    /// <summary>
    /// Writes an autosave recovery file without changing the project's modified timestamp.
    /// </summary>
    /// <param name="project">The project snapshot to write.</param>
    /// <param name="projectPath">The main project path used to derive the autosave path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The autosave result.</returns>
    public async Task<ProjectSaveResult> WriteAutosaveAsync(
        Project project,
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var diagnostics = new DiagnosticBag();

        if (project.SchemaVersion != ProjectSchema.CurrentVersion)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Cannot autosave project schema version {project.SchemaVersion}; expected {ProjectSchema.CurrentVersion}."));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }

        var autosavePath = GetAutosavePath(projectPath);
        byte[] bytes;
        try
        {
            bytes = ProjectJson.SerializeToUtf8Bytes(project, indented: true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.AutosaveFailed,
                $"Could not serialize autosave '{autosavePath}': {exception.Message}"));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }

        try
        {
            await fileWriter.WriteAtomicallyAsync(autosavePath, bytes, createBackup: false, cancellationToken)
                .ConfigureAwait(false);
            return new ProjectSaveResult(project, diagnostics.Sorted);
        }
        catch (Exception exception) when (IsIoFailure(exception) && exception is not OperationCanceledException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.AutosaveFailed,
                $"Could not write autosave '{autosavePath}': {exception.Message}"));
            return new ProjectSaveResult(null, diagnostics.Sorted);
        }
    }

    /// <summary>
    /// Deletes the autosave recovery file if it exists.
    /// </summary>
    /// <param name="projectPath">The main project path used to derive the autosave path.</param>
    /// <returns><see langword="true"/> when a file was removed.</returns>
    public static bool DeleteAutosave(string projectPath)
    {
        var autosavePath = GetAutosavePath(projectPath);
        try
        {
            if (!File.Exists(autosavePath))
            {
                return false;
            }

            File.Delete(autosavePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a known autosave file at its exact snapshot path.
    /// </summary>
    /// <param name="autosavePath">The autosave snapshot path returned by a recovery candidate.</param>
    /// <returns><see langword="true"/> when a file was removed.</returns>
    public static bool DeleteAutosaveFile(string autosavePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(autosavePath);
        try
        {
            if (!File.Exists(autosavePath))
            {
                return false;
            }

            File.Delete(autosavePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Acquires the project editing lock, reclaiming a stale lock when safe.
    /// </summary>
    /// <param name="projectPath">The project file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lock result.</returns>
    public async Task<ProjectLockResult> AcquireLockAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var lockPath = GetLockPath(projectPath);
        var diagnostics = new DiagnosticBag();
        var now = timeProvider.GetUtcNow();
        var token = Guid.NewGuid().ToString("N");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var directory = Path.GetDirectoryName(lockPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var content = new LockFileContent(Environment.ProcessId, Environment.MachineName, token, now);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(
                    content,
                    ProjectStoreJsonContext.Default.LockFileContent);

                await using (var stream = new FileStream(
                                 lockPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 4096,
                                 FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }

                return new ProjectLockResult(
                    new ProjectFileLock(lockPath, token, now, diagnostics.Sorted),
                    diagnostics.Sorted);
            }
            catch (IOException) when (File.Exists(lockPath))
            {
                var existing = TryReadLockFile(lockPath);
                if (existing is not null && !IsStale(existing, now))
                {
                    diagnostics.Add(Error(
                        ProjectDiagnosticCodes.LockHeld,
                        $"Project '{GetFullPath(projectPath)}' is locked by another session.",
                        "lock"));
                    return new ProjectLockResult(null, diagnostics.Sorted);
                }

                diagnostics.Add(new Diagnostic(
                    ProjectDiagnosticCodes.StaleLockRecovered,
                    DiagnosticSeverity.Warning,
                    $"Reclaimed a stale project lock for '{GetFullPath(projectPath)}'.",
                    new ModelPath("project", Guid.Empty, "lock")));

                TryDelete(lockPath);
            }
            catch (Exception exception) when (IsIoFailure(exception) && exception is not OperationCanceledException)
            {
                diagnostics.Add(Error(
                    ProjectDiagnosticCodes.LockHeld,
                    $"Could not acquire the project lock '{lockPath}': {exception.Message}"));
                return new ProjectLockResult(null, diagnostics.Sorted);
            }
        }

        diagnostics.Add(Error(
            ProjectDiagnosticCodes.LockHeld,
            $"Project '{GetFullPath(projectPath)}' is locked by another session."));
        return new ProjectLockResult(null, diagnostics.Sorted);
    }

    private async Task<ProjectLoadResult> LoadFromPathAsync(string fullPath, CancellationToken cancellationToken)
    {
        var diagnostics = new DiagnosticBag();

        try
        {
            var info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                diagnostics.Add(Error(
                    ProjectDiagnosticCodes.InvalidDocument,
                    $"Project file '{fullPath}' does not exist."));
                return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.NotFound);
            }

            if (info.Length > options.MaxDocumentBytes)
            {
                diagnostics.Add(Error(
                    ProjectDiagnosticCodes.InvalidDocument,
                    $"Project file '{fullPath}' exceeds the {options.MaxDocumentBytes} byte limit."));
                return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.TooLarge);
            }

            var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
            return LoadBytes(bytes, fullPath);
        }
        catch (Exception exception) when (IsIoFailure(exception) && exception is not OperationCanceledException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Could not read project '{fullPath}': {exception.Message}"));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.Io);
        }
    }

    private ProjectLoadResult LoadBytes(byte[] bytes, string path)
    {
        try
        {
            return LoadBytesCore(bytes, path);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            var diagnostics = new DiagnosticBag();
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' could not be loaded: {exception.Message}"));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }
    }

    private ProjectLoadResult LoadBytesCore(byte[] bytes, string path)
    {
        var diagnostics = new DiagnosticBag();

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' must be UTF-8 without a byte-order mark."));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        string text;
        try
        {
            text = strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' is not valid UTF-8."));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(text);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' is not valid JSON: {exception.Message}"));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        if (node is not JsonObject root || !TryReadSchemaVersion(root, out var version))
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' must be a JSON object with an integer schemaVersion."));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        var migration = ProjectMigrations.Apply(root, version);
        diagnostics.AddRange(migration.Diagnostics);
        if (migration.Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        Project project;
        try
        {
            var migratedBytes = Encoding.UTF8.GetBytes(migration.Document.ToJsonString());
            project = ProjectJson.Deserialize(migratedBytes);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(Error(
                ProjectDiagnosticCodes.InvalidDocument,
                $"Project file '{path}' does not match the project schema: {exception.Message}"));
            return new ProjectLoadResult(null, diagnostics.Sorted, ProjectLoadFailureKind.InvalidContent);
        }

        if (options.ValidateOnLoad)
        {
            diagnostics.AddRange(validator(project));
        }

        return new ProjectLoadResult(project, diagnostics.Sorted);
    }

    private static bool TryReadSchemaVersion(JsonObject root, out int version)
    {
        version = 0;
        if (!root.TryGetPropertyValue("schemaVersion", out var node) || node is not JsonValue value)
        {
            return false;
        }

        try
        {
            version = value.GetValue<int>();
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            return false;
        }
    }

    private bool IsStale(LockFileContent content, DateTimeOffset now)
    {
        if (string.Equals(content.Host, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            if (content.ProcessId == Environment.ProcessId)
            {
                return false;
            }

            return !IsProcessRunning(content.ProcessId);
        }

        return now - content.AcquiredUtc > options.StaleLockTimeout;
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static LockFileContent? TryReadLockFile(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            return JsonSerializer.Deserialize(bytes, ProjectStoreJsonContext.Default.LockFileContent);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // The next attempt or session will retry.
        }
        catch (UnauthorizedAccessException)
        {
            // The next attempt or session will retry.
        }
    }

    private static string GetFullPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    private static bool IsIoFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException;

    private static Diagnostic Error(string code, string message, string? property = null) =>
        new(
            code,
            DiagnosticSeverity.Error,
            message,
            property is null ? null : new ModelPath("project", Guid.Empty, property));
}
