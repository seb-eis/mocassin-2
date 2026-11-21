// SPDX-License-Identifier: MIT

using Avalonia.Controls;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.Views.Platform;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Builds the Avalonia menu from the shell menu model.
/// </summary>
internal static class MenuBuilder
{
    /// <summary>
    /// Rebuilds a menu.
    /// </summary>
    /// <param name="menu">The target menu.</param>
    /// <param name="menus">The menu model.</param>
    /// <param name="platform">The target platform.</param>
    public static void Apply(Menu menu, IReadOnlyList<MenuViewModel> menus, GesturePlatform platform)
    {
        menu.Items.Clear();
        foreach (var menuModel in menus)
        {
            var topLevel = new MenuItem { Header = menuModel.Header };
            foreach (var item in menuModel.Items)
            {
                var menuItem = new MenuItem
                {
                    Header = item.Header,
                    Command = item.Command,
                };
                if (item.Gesture is { } gesture)
                {
                    menuItem.InputGesture = GestureTranslator.ToKeyGesture(gesture, platform);
                }

                topLevel.Items.Add(menuItem);
            }

            menu.Items.Add(topLevel);
        }
    }
}
