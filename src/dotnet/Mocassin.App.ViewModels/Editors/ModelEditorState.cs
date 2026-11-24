// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The coarse state a model editor is in.
/// </summary>
internal enum ModelEditorState
{
    /// <summary>
    /// The domain has no objects and shows its empty state.
    /// </summary>
    Empty,

    /// <summary>
    /// The domain has objects and is editable.
    /// </summary>
    Ready,

    /// <summary>
    /// The session is read-only; every edit command is disabled.
    /// </summary>
    ReadOnly,
}
