// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.ViewModels.Settings;

/// <summary>
/// Logging settings.
/// </summary>
internal sealed record LoggingSettings
{
    /// <summary>
    /// Gets the minimum log level.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<SettingsLogLevel>))]
    public SettingsLogLevel Level { get; init; } = SettingsLogLevel.Information;
}
