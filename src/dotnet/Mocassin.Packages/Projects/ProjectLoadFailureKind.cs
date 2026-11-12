// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects;

/// <summary>
/// Classifies why a project document could not be loaded.
/// </summary>
/// <remarks>
/// The kind lets the CLI distinguish project-content failures (exit code <c>1</c>) from file-system
/// failures (exit code <c>3</c>) without inspecting <c>PROJ002</c>.
/// </remarks>
public enum ProjectLoadFailureKind
{
    /// <summary>
    /// The document loaded. This value is also used by callers that do not classify a failure.
    /// </summary>
    None = 0,

    /// <summary>
    /// The project file does not exist.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// The project file exceeds the configured document-size limit.
    /// </summary>
    TooLarge = 2,

    /// <summary>
    /// The file could not be read because of an operating-system or sharing failure.
    /// </summary>
    Io = 3,

    /// <summary>
    /// The file was read but its content (encoding, JSON, schema, version, or migration) is invalid.
    /// </summary>
    InvalidContent = 4,
}
