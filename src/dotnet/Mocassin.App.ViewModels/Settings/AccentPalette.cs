// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// The small fixed accent palette. A <see langword="null"/> accent uses the Fluent default.
/// </summary>
internal static class AccentPalette
{
    /// <summary>
    /// Gets the supported accent keys.
    /// </summary>
    public static IReadOnlyList<string> Keys { get; } = ["blue", "teal", "green", "amber", "violet", "rose"];

    /// <summary>
    /// Gets a value indicating whether the key is in the palette.
    /// </summary>
    /// <param name="key">The accent key.</param>
    /// <returns><see langword="true"/> for a supported key.</returns>
    public static bool IsKnown(string? key) =>
        key is not null && Keys.Contains(key, StringComparer.Ordinal);
}
