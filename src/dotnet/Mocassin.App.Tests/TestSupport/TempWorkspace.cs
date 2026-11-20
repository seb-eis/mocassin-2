// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// A temporary directory that is deleted when the test disposes it.
/// </summary>
internal sealed class TempWorkspace : IDisposable
{
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
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
