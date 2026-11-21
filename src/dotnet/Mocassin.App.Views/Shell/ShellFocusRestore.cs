// SPDX-License-Identifier: MIT

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Mocassin.App.Views.Shell;

/// <summary>
/// Restores focus to a still-usable element, with the contract's document-tab and main-menu fallbacks.
/// </summary>
internal static class ShellFocusRestore
{
    /// <summary>
    /// Focuses the target when it is still attached, visible, and enabled. Otherwise it focuses the document tab
    /// strip, then the main menu.
    /// </summary>
    /// <param name="topLevel">The window that owns the focus manager.</param>
    /// <param name="target">The captured element.</param>
    /// <returns><see langword="true"/> when focus was moved.</returns>
    public static bool TryFocus(TopLevel? topLevel, IInputElement? target)
    {
        var focusManager = topLevel?.FocusManager;
        if (focusManager is null)
        {
            return false;
        }

        var element = IsUsable(target) ? target : FindFallback(topLevel!);
        if (element is null)
        {
            return false;
        }

        focusManager.Focus(element, NavigationMethod.Tab, KeyModifiers.None);
        return true;
    }

    private static bool IsUsable(IInputElement? element) =>
        element is Control control
        && TopLevel.GetTopLevel(control) is not null
        && control.IsEffectivelyVisible
        && control.IsEffectivelyEnabled;

    private static IInputElement? FindFallback(TopLevel topLevel)
    {
        var tabs = topLevel.GetVisualDescendants()
            .OfType<TabControl>()
            .FirstOrDefault(control => AutomationProperties.GetName(control) == "Document tabs");
        return tabs is not null
            ? tabs
            : topLevel.GetVisualDescendants().OfType<Menu>().FirstOrDefault();
    }
}
