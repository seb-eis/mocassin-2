// SPDX-License-Identifier: MIT

using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// File facts used by the session state machine. The implementation lives in the application host.
/// </summary>
internal interface IProjectFileProbe
{
    /// <summary>
    /// Gets the full path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The full path.</returns>
    public string GetFullPath(string path);

    /// <summary>
    /// Combines a directory and file name.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <param name="fileName">The file name.</param>
    /// <returns>The combined path.</returns>
    public string Combine(string directory, string fileName);

    /// <summary>
    /// Gets the parent directory.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The parent directory, or <see langword="null"/>.</returns>
    public string? GetDirectoryName(string path);

    /// <summary>
    /// Gets whether a path exists and what it is.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The target state.</returns>
    public ProjectTargetState GetTargetState(string path);

    /// <summary>
    /// Gets the comparison used for project paths on this platform.
    /// </summary>
    public StringComparison PathComparison { get; }

    /// <summary>
    /// Creates a directory if it does not exist.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <param name="error">The failure message, if any.</param>
    /// <returns><see langword="true"/> when the directory exists.</returns>
    public bool TryCreateDirectory(string directory, out string? error);

    /// <summary>
    /// Gets the current file stamp.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The stamp, or a missing stamp.</returns>
    public ProjectFileStamp GetStamp(string path);
}
