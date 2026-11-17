// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Tests.TestSupport;

/// <summary>
/// A unique temporary directory that is deleted on disposal.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    /// <summary>
    /// Creates the directory.
    /// </summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "mocassin-tests",
            Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the directory path.</summary>
    public string Path { get; }

    /// <summary>
    /// Combines a file name with the directory path.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <returns>The combined path.</returns>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort test cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort test cleanup.
        }
    }
}
