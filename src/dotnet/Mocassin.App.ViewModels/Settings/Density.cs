// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// UI density stored in the application settings.
/// </summary>
internal enum Density
{
    /// <summary>
    /// Comfortable spacing.
    /// </summary>
    [JsonStringEnumMemberName("comfortable")]
    Comfortable,

    /// <summary>
    /// Compact spacing.
    /// </summary>
    [JsonStringEnumMemberName("compact")]
    Compact,
}
