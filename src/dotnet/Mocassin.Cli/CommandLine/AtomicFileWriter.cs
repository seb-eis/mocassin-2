// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine;

/// <summary>
/// Writes a file through a temporary sibling and a rename, so a failure never truncates an existing file.
/// </summary>
internal static class AtomicFileWriter
{
    /// <summary>
    /// Writes the bytes atomically.
    /// </summary>
    /// <param name="path">The final path; its parent directory must exist.</param>
    /// <param name="bytes">The exact bytes to write.</param>
    /// <param name="overwrite">Whether an existing file may be replaced.</param>
    /// <exception cref="IOException">
    /// The parent directory is missing, the target exists when <paramref name="overwrite"/> is false, or the
    /// rename fails.
    /// </exception>
    public static void Write(string path, byte[] bytes, bool overwrite)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bytes);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new IOException($"The output directory '{directory}' does not exist.");
        }

        var temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, fullPath, overwrite);
        }
        catch
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // The rename failure remains the reported error.
            }
            catch (UnauthorizedAccessException)
            {
                // The rename failure remains the reported error.
            }

            throw;
        }
    }
}
