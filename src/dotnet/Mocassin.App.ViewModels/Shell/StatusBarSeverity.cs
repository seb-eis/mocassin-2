// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Severity used by a status-bar item to select its glyph colour without referencing Avalonia types.
/// </summary>
internal enum StatusBarSeverity
{
    /// <summary>
    /// Neutral information.
    /// </summary>
    Normal,

    /// <summary>
    /// An informational or in-progress state.
    /// </summary>
    Information,

    /// <summary>
    /// A successful state.
    /// </summary>
    Success,

    /// <summary>
    /// A warning state.
    /// </summary>
    Warning,

    /// <summary>
    /// An error state.
    /// </summary>
    Error,
}
