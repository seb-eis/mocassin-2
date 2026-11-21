// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.App.Recovery;

/// <summary>
/// Source-generated JSON context for the application-local recovery manifest.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RecoveryManifest))]
internal partial class RecoveryManifestJsonContext : JsonSerializerContext;
