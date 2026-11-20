// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Log level stored in the application settings.
/// </summary>
internal enum SettingsLogLevel
{
    /// <summary>
    /// Trace.
    /// </summary>
    [JsonStringEnumMemberName("trace")]
    Trace,

    /// <summary>
    /// Debug.
    /// </summary>
    [JsonStringEnumMemberName("debug")]
    Debug,

    /// <summary>
    /// Information.
    /// </summary>
    [JsonStringEnumMemberName("information")]
    Information,

    /// <summary>
    /// Warning.
    /// </summary>
    [JsonStringEnumMemberName("warning")]
    Warning,

    /// <summary>
    /// Error.
    /// </summary>
    [JsonStringEnumMemberName("error")]
    Error,

    /// <summary>
    /// Fatal.
    /// </summary>
    [JsonStringEnumMemberName("fatal")]
    Fatal,
}
