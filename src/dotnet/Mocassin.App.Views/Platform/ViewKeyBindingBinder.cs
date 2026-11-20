// SPDX-License-Identifier: MIT

using Avalonia.Input;
using Mocassin.App.ViewModels.Commands;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Binds the catalog's gestures to a window or other input element.
/// </summary>
internal static class ViewKeyBindingBinder
{
    /// <summary>
    /// Adds one key binding for every command with a gesture.
    /// </summary>
    /// <param name="target">The input element.</param>
    /// <param name="catalog">The command catalog.</param>
    /// <param name="platform">The target platform.</param>
    public static void Apply(InputElement target, ICommandCatalog catalog, GesturePlatform platform)
    {
        foreach (var command in catalog.Commands)
        {
            if (command.Gesture is not { } gesture)
            {
                continue;
            }

            target.KeyBindings.Add(
                new KeyBinding
                {
                    Gesture = GestureTranslator.ToKeyGesture(gesture, platform),
                    Command = command.Command,
                });
        }
    }
}
