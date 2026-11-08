// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Project;

/// <summary>
/// Stable diagnostic codes emitted by the project format and persistence layer.
/// </summary>
public static class ProjectDiagnosticCodes
{
    /// <summary>
    /// The document declares a schema version newer than this application supports.
    /// </summary>
    public const string FutureSchemaVersion = "PROJ001";

    /// <summary>
    /// The document is malformed or cannot be parsed as a project.
    /// </summary>
    public const string InvalidDocument = "PROJ002";

    /// <summary>
    /// Two objects in the same domain share an identifier.
    /// </summary>
    public const string DuplicateId = "PROJ003";

    /// <summary>
    /// A reference points to an identifier that does not exist.
    /// </summary>
    public const string UnknownReference = "PROJ010";

    /// <summary>
    /// A reference points to an identifier that exists in a different domain.
    /// </summary>
    public const string WrongDomainReference = "PROJ011";

    /// <summary>
    /// Project metadata is missing or invalid.
    /// </summary>
    public const string InvalidMetadata = "PROJ020";

    /// <summary>
    /// View settings are invalid or incomplete.
    /// </summary>
    public const string InvalidViewSettings = "PROJ021";

    /// <summary>
    /// Project symmetry metadata differs from the embedded catalog or contract versions.
    /// </summary>
    public const string SymmetryMetadataMismatch = "PROJ034";

    /// <summary>
    /// A file save failed; the previous file was left intact.
    /// </summary>
    public const string SaveFailed = "PROJ030";

    /// <summary>
    /// Another process holds the project file lock.
    /// </summary>
    public const string LockHeld = "PROJ031";

    /// <summary>
    /// A stale project file lock was reclaimed.
    /// </summary>
    public const string StaleLockRecovered = "PROJ032";

    /// <summary>
    /// An autosave recovery file operation failed.
    /// </summary>
    public const string AutosaveFailed = "PROJ033";
}
