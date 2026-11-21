// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Default menu provider over the command catalog.
/// </summary>
internal sealed class ShellMenuProvider : IMenuItemProvider
{
    private readonly ICommandCatalog catalog;

    /// <summary>
    /// Initializes the menu provider.
    /// </summary>
    /// <param name="catalog">The command catalog.</param>
    public ShellMenuProvider(ICommandCatalog catalog)
    {
        this.catalog = catalog;
    }

    /// <inheritdoc />
    public IEnumerable<ShellCommand> CreateMenuItems(MenuKind menu) =>
        catalog.Commands.Where(command => command.Menu == menu);
}
