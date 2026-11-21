// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// A temporary directory that is deleted when the test disposes it.
/// </summary>
/// <remarks>
/// Asynchronous settings persistence can still hold a temporary file when a headless test finishes. Windows
/// reports that as a sharing violation during recursive deletion, so disposal retries briefly instead of failing
/// the test on a transient lock.
/// </remarks>
internal sealed class TempWorkspace : IDisposable
{
    private const int deleteAttempts = 20;
    private static readonly TimeSpan deleteRetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Initializes a new temporary directory.
    /// </summary>
    public TempWorkspace()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "mocassin-app-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    /// Gets the temporary directory path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a child path under the temporary directory.
    /// </summary>
    /// <param name="parts">The path parts.</param>
    /// <returns>The combined path.</returns>
    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    /// <inheritdoc />
    public void Dispose()
    {
        for (var attempt = 1; attempt <= deleteAttempts; attempt++)
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }

                return;
            }
            catch (Exception exception) when (
                attempt < deleteAttempts
                && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(deleteRetryDelay);
            }
        }
    }
}
