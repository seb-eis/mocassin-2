// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.ViewModels.Settings.Json;

/// <summary>
/// Source-generated JSON context for the application settings.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext;
