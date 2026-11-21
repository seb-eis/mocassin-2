// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// The kind of change announced by <see cref="ProjectEditCoordinator.ProjectChanged"/>.
/// </summary>
internal enum ProjectChangeKind
{
    /// <summary>
    /// A new edit was accepted and recorded on the undo stack.
    /// </summary>
    Edited,

    /// <summary>
    /// A recorded edit was undone.
    /// </summary>
    Undo,

    /// <summary>
    /// An undone edit was redone.
    /// </summary>
    Redo,

    /// <summary>
    /// The document was marked saved; the values are unchanged and the dirty state is refreshed.
    /// </summary>
    Saved,

    /// <summary>
    /// The coordinator adopted a different document.
    /// </summary>
    Replaced,
}
