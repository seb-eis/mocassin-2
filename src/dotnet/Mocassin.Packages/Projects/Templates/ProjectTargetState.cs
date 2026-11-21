// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// The only file-system fact the pure target validator needs.
/// </summary>
public enum ProjectTargetState
{
    /// <summary>
    /// The target path does not exist.
    /// </summary>
    Absent = 0,

    /// <summary>
    /// The target path is an existing file.
    /// </summary>
    File = 1,

    /// <summary>
    /// The target path is an existing directory.
    /// </summary>
    Directory = 2,
}
