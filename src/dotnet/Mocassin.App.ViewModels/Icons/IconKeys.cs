// SPDX-License-Identifier: MIT

using System.Collections.Frozen;

namespace Mocassin.App.ViewModels.Icons;

/// <summary>
/// The stable, application-local icon keys and the geometry resource each key maps to.
/// </summary>
/// <remarks>
/// One key names one semantic target; a tree node and a document tab for the same target share the key. The
/// <c>Missing</c> geometry is not a key: it is the fallback marker for an unresolved non-null key.
/// </remarks>
internal static class IconKeys
{
    /// <summary>
    /// The welcome document tab.
    /// </summary>
    public const string Welcome = "welcome";

    /// <summary>
    /// The project overview node and tab.
    /// </summary>
    public const string Overview = "overview";

    /// <summary>
    /// The project JSON node and tab.
    /// </summary>
    public const string Json = "json";

    /// <summary>
    /// An open-project row.
    /// </summary>
    public const string Project = "project";

    /// <summary>
    /// The fallback marker for a key with no vendored geometry, never mapped from a kind or node.
    /// </summary>
    public const string MissingResource = "Mocassin.Icon.Missing";

    private static readonly FrozenDictionary<string, string> resourceNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Welcome] = "Mocassin.Icon.Welcome",
            [Overview] = "Mocassin.Icon.Overview",
            [Json] = "Mocassin.Icon.Json",
            [Project] = "Mocassin.Icon.Project",
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether a key has a vendored geometry.
    /// </summary>
    /// <param name="key">The icon key.</param>
    /// <returns><see langword="true"/> when the key is mapped.</returns>
    public static bool IsKnown(string? key) => key is not null && resourceNames.ContainsKey(key);

    /// <summary>
    /// Resolves the geometry resource name for an icon key.
    /// </summary>
    /// <param name="key">The icon key.</param>
    /// <returns>The resource name, or <see langword="null"/> when the key is not mapped.</returns>
    public static string? ResourceKeyFor(string? key) =>
        key is not null && resourceNames.TryGetValue(key, out var resource) ? resource : null;
}
