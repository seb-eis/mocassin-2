// SPDX-License-Identifier: MIT

namespace Mocassin.App.Composition;

/// <summary>
/// Enforces the log-file size budget outside the rolling sink.
/// </summary>
internal static class LogRetention
{
    /// <summary>
    /// The default total log-size budget in bytes.
    /// </summary>
    public const long MaxTotalBytes = 100L * 1024 * 1024;

    /// <summary>
    /// Deletes the oldest closed log files until the directory is under the size budget.
    /// </summary>
    /// <param name="directory">The log directory.</param>
    /// <param name="activeFileName">The active log file name, which is never deleted.</param>
    /// <param name="maxTotalBytes">The total size budget.</param>
    /// <returns>The deleted file paths.</returns>
    public static IReadOnlyList<string> Sweep(
        string directory,
        string activeFileName,
        long maxTotalBytes = MaxTotalBytes)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var files = new DirectoryInfo(directory)
            .GetFiles("mocassin-*.log")
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();

        var totalBytes = files.Sum(file => file.Length);
        var deleted = new List<string>();
        foreach (var file in files)
        {
            if (totalBytes <= maxTotalBytes)
            {
                break;
            }

            if (string.Equals(file.Name, activeFileName, StringComparison.Ordinal))
            {
                continue;
            }

            totalBytes -= file.Length;
            file.Delete();
            deleted.Add(file.FullName);
        }

        return deleted;
    }
}
