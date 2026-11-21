// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Theme selection stored in the application settings.
/// </summary>
internal enum ThemeSelection
{
    /// <summary>
    /// Follow the platform theme.
    /// </summary>
    [JsonStringEnumMemberName("system")]
    System,

    /// <summary>
    /// Light theme.
    /// </summary>
    [JsonStringEnumMemberName("light")]
    Light,

    /// <summary>
    /// Dark theme.
    /// </summary>
    [JsonStringEnumMemberName("dark")]
    Dark,
}
