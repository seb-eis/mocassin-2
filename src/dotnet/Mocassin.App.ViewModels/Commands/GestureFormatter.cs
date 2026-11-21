// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Formats framework-neutral gestures for the current platform.
/// </summary>
internal sealed class GestureFormatter
{
    private readonly GesturePlatform platform;

    /// <summary>
    /// Initializes a formatter.
    /// </summary>
    /// <param name="platform">The target platform.</param>
    public GestureFormatter(GesturePlatform platform)
    {
        this.platform = platform;
    }

    /// <summary>
    /// Formats a gesture.
    /// </summary>
    /// <param name="gesture">The gesture.</param>
    /// <returns>The display text, for example <c>Ctrl+Shift+P</c>.</returns>
    public string Format(KeyGestureDescriptor gesture)
    {
        var modifiers = new List<string>(4);
        if (gesture.Modifiers.HasFlag(GestureModifier.Primary))
        {
            modifiers.Add(platform == GesturePlatform.MacOS ? "Cmd" : "Ctrl");
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Control))
        {
            modifiers.Add("Ctrl");
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Shift))
        {
            modifiers.Add("Shift");
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Alt))
        {
            modifiers.Add("Alt");
        }

        if (gesture.Modifiers.HasFlag(GestureModifier.Meta))
        {
            modifiers.Add("Cmd");
        }

        modifiers.Add(NormalizeKey(gesture.Key));
        return string.Join('+', modifiers);
    }

    private static string NormalizeKey(string key)
    {
        if (key.Length == 1)
        {
            return key.ToUpperInvariant();
        }

        return key switch
        {
            "Left" => "Left",
            "Right" => "Right",
            "Tab" => "Tab",
            "Escape" => "Esc",
            "OemComma" => ",",
            "OemPeriod" => ".",
            _ => key,
        };
    }
}
