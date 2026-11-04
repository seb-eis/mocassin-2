// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects;

/// <summary>
/// Writes project files atomically. This is an internal seam for failure-injection tests.
/// </summary>
internal interface IProjectFileWriter
{
    /// <summary>
    /// Writes bytes to the target through a temporary file and an atomic rename.
    /// </summary>
    /// <param name="targetPath">The final file path.</param>
    /// <param name="content">The UTF-8 content.</param>
    /// <param name="createBackup">Whether to preserve the previous target as <c>.bak</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the target has been replaced.</returns>
    public Task WriteAtomicallyAsync(string targetPath, byte[] content, bool createBackup, CancellationToken cancellationToken);
}

/// <summary>
/// Default atomic project-file writer.
/// </summary>
internal sealed class AtomicProjectFileWriter : IProjectFileWriter
{
    /// <inheritdoc />
    public async Task WriteAtomicallyAsync(string targetPath, byte[] content, bool createBackup, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(content);

        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new IOException($"Project path '{targetPath}' has no parent directory.");
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            if (createBackup && File.Exists(targetPath))
            {
                File.Copy(targetPath, targetPath + ".bak", overwrite: true);
            }

            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
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
            // Best-effort cleanup; the target was never truncated.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup; the target was never truncated.
        }
    }
}
