// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects;

/// <summary>
/// Source-generated JSON metadata for internal project-store files.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(LockFileContent))]
internal sealed partial class ProjectStoreJsonContext : JsonSerializerContext;

/// <summary>
/// On-disk content of a <c>.mocprj.lock</c> file.
/// </summary>
internal sealed record LockFileContent(
    [property: JsonPropertyName("pid")] int ProcessId,
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("acquiredUtc")]
    [property: JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    DateTimeOffset AcquiredUtc);
