// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Framework-neutral gesture modifiers. <see cref="Primary"/> maps to Control or Command by platform.
/// </summary>
[Flags]
internal enum GestureModifier
{
    /// <summary>
    /// No modifier.
    /// </summary>
    None = 0,

    /// <summary>
    /// The Control key.
    /// </summary>
    Control = 1,

    /// <summary>
    /// The Shift key.
    /// </summary>
    Shift = 2,

    /// <summary>
    /// The Alt key.
    /// </summary>
    Alt = 4,

    /// <summary>
    /// The Meta/Windows/Command key.
    /// </summary>
    Meta = 8,

    /// <summary>
    /// The platform primary modifier.
    /// </summary>
    Primary = 16,
}
