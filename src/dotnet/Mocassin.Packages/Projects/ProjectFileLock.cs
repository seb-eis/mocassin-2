// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Projects;

/// <summary>
/// A held project editing lock. Disposing the handle removes the lock if it still belongs to this session.
/// </summary>
public sealed class ProjectFileLock : IDisposable, IAsyncDisposable
{
    private readonly string token;

    internal ProjectFileLock(
        string path,
        string token,
        DateTimeOffset acquiredUtc,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Path = path;
        this.token = token;
        AcquiredUtc = acquiredUtc;
        Diagnostics = diagnostics.IsDefault ? ImmutableArray<Diagnostic>.Empty : diagnostics;
    }

    /// <summary>
    /// Gets the lock-file path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the UTC time at which the lock was acquired.
    /// </summary>
    public DateTimeOffset AcquiredUtc { get; }

    /// <summary>
    /// Gets diagnostics produced while acquiring the lock, including stale-lock recovery warnings.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether the lock handle has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return ValueTask.CompletedTask;
        }

        IsDisposed = true;
        TryDeleteIfOwned();
        return ValueTask.CompletedTask;
    }

    private void TryDeleteIfOwned()
    {
        try
        {
            if (!File.Exists(Path))
            {
                return;
            }

            var bytes = File.ReadAllBytes(Path);
            var content = JsonSerializer.Deserialize(bytes, ProjectStoreJsonContext.Default.LockFileContent);
            if (content is null || string.Equals(content.Token, token, StringComparison.Ordinal))
            {
                File.Delete(Path);
            }
        }
        catch (IOException)
        {
            // A stale lock will be reclaimed by the next session after the timeout.
        }
        catch (UnauthorizedAccessException)
        {
            // A stale lock will be reclaimed by the next session after the timeout.
        }
        catch (JsonException)
        {
            // Malformed lock files are reclaimed by the next session as stale.
        }
    }
}
