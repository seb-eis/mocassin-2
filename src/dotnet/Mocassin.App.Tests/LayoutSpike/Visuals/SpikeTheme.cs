// SPDX-License-Identifier: MIT

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Mocassin.App.Tests.LayoutSpike.Visuals;

/// <summary>
/// Resolves the production theme tokens for the spike's code-built views.
/// </summary>
/// <remarks>
/// The spike restyles nothing. It reads the same <c>Mocassin.*</c> resources the shipped views use, resolved after
/// <c>AvaloniaThemeApplier</c> has applied the settings, so light, dark, and high-contrast captures exercise the
/// real palette and accent.
/// </remarks>
internal static class SpikeTheme
{
    /// <summary>
    /// Resolves a brush token.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush, or a transparent brush when the token is missing.</returns>
    public static IBrush Brush(string key) =>
        TryGet(key, out var value) && value is IBrush brush ? brush : Brushes.Transparent;

    /// <summary>
    /// Resolves a double token.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The value.</returns>
    public static double Double(string key, double fallback) =>
        TryGet(key, out var value) && value is double number ? number : fallback;

    /// <summary>
    /// Resolves a thickness token.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The thickness.</returns>
    public static Thickness Thickness(string key, Thickness fallback) =>
        TryGet(key, out var value) && value is Thickness thickness ? thickness : fallback;

    private static bool TryGet(string key, out object? value)
    {
        var application = Application.Current;
        if (application is not null
            && application.Resources.TryGetResource(key, application.ActualThemeVariant, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets the active theme variant, treating the default as light.
    /// </summary>
    public static ThemeVariant Variant =>
        Application.Current?.ActualThemeVariant
        ?? throw new InvalidOperationException("The spike needs the production application resources.");
}
