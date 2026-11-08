// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace Mocassin.Symmetry;

/// <summary>
/// Source-generated JSON context for the internal catalog DTOs.
/// </summary>
/// <remarks>
/// Unknown members are rejected so a catalog with unexpected fields fails fast instead of silently losing
/// data. Property names are camelCase to match the committed UTF-8 JSON.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = false)]
[JsonSerializable(typeof(SpaceGroupCatalogDto))]
[JsonSerializable(typeof(SpaceGroupSettingDto[]))]
[JsonSerializable(typeof(SpaceGroupOperationDto[]))]
internal sealed partial class CatalogJsonContext : JsonSerializerContext;
