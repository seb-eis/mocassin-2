// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Classifies a rejected project name or target path.
/// </summary>
public enum ProjectPathFailure
{
    /// <summary>
    /// The value is valid.
    /// </summary>
    None = 0,

    /// <summary>
    /// The name is empty after trimming.
    /// </summary>
    EmptyName = 1,

    /// <summary>
    /// The name exceeds 128 characters.
    /// </summary>
    NameTooLong = 2,

    /// <summary>
    /// The name contains a path separator.
    /// </summary>
    PathSeparator = 3,

    /// <summary>
    /// The name contains <c>..</c>.
    /// </summary>
    ParentDirectory = 4,

    /// <summary>
    /// The name contains a control character.
    /// </summary>
    ControlCharacter = 5,

    /// <summary>
    /// The name contains one of <c>:*?"&lt;&gt;|</c>.
    /// </summary>
    ReservedCharacter = 6,

    /// <summary>
    /// The target directory is empty or not a valid path. This failure is GUI-only.
    /// </summary>
    InvalidTargetPath = 7,

    /// <summary>
    /// The target already exists as a file.
    /// </summary>
    ExistingTarget = 8,

    /// <summary>
    /// The target path is an existing directory or cannot be written.
    /// </summary>
    IoFailure = 9,
}
