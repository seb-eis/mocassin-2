// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.App.ViewModels.Settings;

namespace Mocassin.App.Settings;

/// <summary>
/// Size and temporary-file policy for the atomic settings write.
/// </summary>
internal static class SettingsWritePolicy
{
    /// <summary>
    /// Gets the maximum settings document size.
    /// </summary>
    public const long MaxBytes = SettingsDefaults.MaxSettingsBytes;

    /// <summary>
    /// Gets a value indicating whether a document is oversized.
    /// </summary>
    /// <param name="length">The document length.</param>
    /// <returns><see langword="true"/> when the document is too large.</returns>
    public static bool IsOversized(long length) => length > MaxBytes;

    /// <summary>
    /// Creates a unique sibling temporary path.
    /// </summary>
    /// <param name="settingsPath">The target settings path.</param>
    /// <returns>The temporary path in the same directory.</returns>
    public static string CreateTemporaryPath(string settingsPath) =>
        string.Create(CultureInfo.InvariantCulture, $"{settingsPath}.{Guid.NewGuid():N}.tmp");
}
