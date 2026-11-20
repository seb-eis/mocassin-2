// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Contributes status-bar items without changing the shell.
/// </summary>
internal interface IStatusBarItemProvider
{
    /// <summary>
    /// Creates the contributed items.
    /// </summary>
    /// <returns>The status-bar items.</returns>
    public IEnumerable<StatusBarItemViewModel> CreateItems();
}
