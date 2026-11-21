// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// A cheap project-file stamp used to detect external modification.
/// </summary>
/// <param name="Length">The file length in bytes.</param>
/// <param name="LastWriteUtc">The last write time in UTC.</param>
internal readonly record struct ProjectFileStamp(long Length, DateTimeOffset LastWriteUtc)
{
    /// <summary>
    /// Gets a stamp that represents a missing file.
    /// </summary>
    public static ProjectFileStamp Missing { get; } = new(-1, DateTimeOffset.MinValue);

    /// <summary>
    /// Gets a value indicating whether the file exists.
    /// </summary>
    public bool Exists => Length >= 0;
}
