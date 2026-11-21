// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Contributes items to one shell menu.
/// </summary>
internal interface IMenuItemProvider
{
    /// <summary>
    /// Creates the items for a menu.
    /// </summary>
    /// <param name="menu">The menu.</param>
    /// <returns>The contributed commands.</returns>
    public IEnumerable<ShellCommand> CreateMenuItems(MenuKind menu);
}
