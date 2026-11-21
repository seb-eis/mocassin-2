// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Commands;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Builds the shell menu model from the registered menu contributions.
/// </summary>
internal static class ShellMenuFactory
{
    private static readonly MenuKind[] menuOrder = [MenuKind.File, MenuKind.Edit, MenuKind.View, MenuKind.Help];

    /// <summary>
    /// Builds the menus in their stable order.
    /// </summary>
    /// <param name="provider">The menu contribution provider.</param>
    /// <returns>The non-empty menus.</returns>
    public static List<MenuViewModel> Build(IMenuItemProvider provider)
    {
        var menus = new List<MenuViewModel>();
        foreach (var menu in menuOrder)
        {
            var items = provider.CreateMenuItems(menu)
                .Select(command => new MenuItemViewModel(
                    command.Title,
                    command.Command,
                    command.Gesture,
                    command.GestureText))
                .ToList();
            if (items.Count > 0)
            {
                menus.Add(new MenuViewModel(HeaderFor(menu), items));
            }
        }

        return menus;
    }

    private static string HeaderFor(MenuKind menu) =>
        menu switch
        {
            MenuKind.File => "_File",
            MenuKind.Edit => "_Edit",
            MenuKind.View => "_View",
            MenuKind.Help => "_Help",
            _ => menu.ToString(),
        };
}
