// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// The shell menus. Later steps add model, build, and run items when they contribute them.
/// </summary>
internal enum MenuKind
{
    /// <summary>
    /// File menu.
    /// </summary>
    File,

    /// <summary>
    /// Edit menu.
    /// </summary>
    Edit,

    /// <summary>
    /// View menu.
    /// </summary>
    View,

    /// <summary>
    /// Model menu.
    /// </summary>
    Model,

    /// <summary>
    /// Build menu.
    /// </summary>
    Build,

    /// <summary>
    /// Run menu.
    /// </summary>
    Run,

    /// <summary>
    /// Help menu.
    /// </summary>
    Help,
}
