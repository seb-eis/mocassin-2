// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Result of checking whether the file changed outside the session.
/// </summary>
internal enum ProjectExternalChange
{
    /// <summary>
    /// The file is unchanged.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The file was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// The file was deleted.
    /// </summary>
    Deleted,
}
