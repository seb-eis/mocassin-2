// SPDX-License-Identifier: MIT

using Mocassin.Packages.Projects.Templates;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Maps a path to the caller-supplied project target state.
/// </summary>
internal static class ProjectTargetStateMapper
{
    /// <summary>
    /// Gets the target state from booleans supplied by the probe.
    /// </summary>
    /// <param name="exists">Whether the path exists.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>The target state.</returns>
    public static ProjectTargetState From(bool exists, bool isDirectory) =>
        !exists ? ProjectTargetState.Absent : isDirectory ? ProjectTargetState.Directory : ProjectTargetState.File;
}
