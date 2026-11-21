// SPDX-License-Identifier: MIT

using Avalonia.Input;
using Mocassin.App.ViewModels.Commands;

namespace Mocassin.App.Views.Platform;

/// <summary>
/// Converts framework-neutral gestures to Avalonia key gestures.
/// </summary>
internal static class GestureTranslator
{
    /// <summary>
    /// Converts a descriptor for the target platform.
    /// </summary>
    /// <param name="gesture">The descriptor.</param>
    /// <param name="platform">The target platform.</param>
    /// <returns>The Avalonia key gesture.</returns>
    public static KeyGesture ToKeyGesture(KeyGestureDescriptor gesture, GesturePlatform platform)
    {
        var modifiers = KeyModifiers.None;
        if (gesture.Modifiers.HasFlag(GestureModifier.Primary))
        {
            modifiers |= platform == GesturePlatform.MacOS ? KeyModifiers.Meta : KeyModifiers.Control;
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Control))
        {
            modifiers |= KeyModifiers.Control;
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Shift))
        {
            modifiers |= KeyModifiers.Shift;
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Alt))
        {
            modifiers |= KeyModifiers.Alt;
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Meta))
        {
            modifiers |= KeyModifiers.Meta;
        }

        return new KeyGesture(MapKey(gesture.Key), modifiers);
    }

    private static Key MapKey(string key) =>
        key switch
        {
            "0" or "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9" => Enum.Parse<Key>("D" + key),
            "Left" => Key.Left,
            "Right" => Key.Right,
            "Up" => Key.Up,
            "Down" => Key.Down,
            "Tab" => Key.Tab,
            "Escape" => Key.Escape,
            "OemComma" => Key.OemComma,
            "OemPeriod" => Key.OemPeriod,
            _ => Enum.TryParse<Key>(key, ignoreCase: true, out var parsed) ? parsed : Key.None,
        };
}
