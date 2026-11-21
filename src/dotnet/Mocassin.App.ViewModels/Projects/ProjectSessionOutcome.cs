// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// The terminal outcome of a project-session operation.
/// </summary>
internal enum ProjectSessionOutcome
{
    /// <summary>
    /// A project was created.
    /// </summary>
    Created,

    /// <summary>
    /// A project was opened.
    /// </summary>
    Opened,

    /// <summary>
    /// An already-open project was activated.
    /// </summary>
    Activated,

    /// <summary>
    /// A project was opened read-only because its lock is held elsewhere.
    /// </summary>
    OpenedReadOnly,

    /// <summary>
    /// A project was saved.
    /// </summary>
    Saved,

    /// <summary>
    /// A project was reloaded from disk.
    /// </summary>
    Reloaded,

    /// <summary>
    /// A project was closed.
    /// </summary>
    Closed,

    /// <summary>
    /// The file was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The file exceeded the size limit.
    /// </summary>
    TooLarge,

    /// <summary>
    /// The file could not be read or written because of an I/O failure.
    /// </summary>
    Io,

    /// <summary>
    /// The file content is invalid.
    /// </summary>
    InvalidContent,

    /// <summary>
    /// The project is locked or the operation needs a writable session.
    /// </summary>
    Locked,

    /// <summary>
    /// The New target already exists. New never overwrites.
    /// </summary>
    ExistingTarget,

    /// <summary>
    /// The save failed.
    /// </summary>
    SaveFailed,

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    Canceled,

    /// <summary>
    /// The operation needs an unsaved-changes decision.
    /// </summary>
    UnsavedChanges,

    /// <summary>
    /// A recovery snapshot was applied.
    /// </summary>
    Recovered,

    /// <summary>
    /// A recovery snapshot was discarded.
    /// </summary>
    Discarded,
}
