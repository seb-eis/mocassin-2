// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Model;

namespace Mocassin.Core.Project;

/// <summary>
/// Serializes and deserializes the versioned project JSON document.
/// </summary>
public static class ProjectJson
{
    /// <summary>
    /// Options for human-readable project files.
    /// </summary>
    public static JsonSerializerOptions FileOptions { get; } = CreateOptions(writeIndented: true);

    /// <summary>
    /// Options for canonical, whitespace-free serialization and hashing.
    /// </summary>
    public static JsonSerializerOptions CanonicalOptions { get; } = CreateOptions(writeIndented: false);

    internal static JsonSerializerOptions DtoOptions { get; } = CreateDtoOptions();

    /// <summary>
    /// Serializes a project to JSON text.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <param name="indented">Whether to write the human-readable indented form.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(Project project, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonSerializer.Serialize(project, indented ? FileOptions : CanonicalOptions);
    }

    /// <summary>
    /// Serializes a project to UTF-8 JSON bytes.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <param name="indented">Whether to write the human-readable indented form.</param>
    /// <returns>The UTF-8 JSON bytes without a byte-order mark.</returns>
    public static byte[] SerializeToUtf8Bytes(Project project, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonSerializer.SerializeToUtf8Bytes(project, indented ? FileOptions : CanonicalOptions);
    }

    /// <summary>
    /// Deserializes a project from JSON text.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed project.</returns>
    /// <exception cref="JsonException">The document is malformed or has an unsupported schema version.</exception>
    public static Project Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<Project>(json, FileOptions)
            ?? throw new JsonException("Project JSON did not contain a project object.");
    }

    /// <summary>
    /// Deserializes a project from UTF-8 JSON bytes.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 JSON bytes.</param>
    /// <returns>The parsed project.</returns>
    /// <exception cref="JsonException">The document is malformed or has an unsupported schema version.</exception>
    public static Project Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<Project>(utf8Json, FileOptions)
            ?? throw new JsonException("Project JSON did not contain a project object.");
    }

    internal static byte[] SerializeCanonicalModel(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return JsonSerializer.SerializeToUtf8Bytes(model, DtoOptions);
    }

    private static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ProjectJsonConverter());
        return options;
    }

    private static JsonSerializerOptions CreateDtoOptions()
    {
        var options = new JsonSerializerOptions(ProjectJsonContext.Default.Options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        return options;
    }
}

/// <summary>
/// Maps the public project value to the flat on-disk envelope.
/// </summary>
public sealed class ProjectJsonConverter : JsonConverter<Project>
{
    /// <inheritdoc />
    public override Project Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<ProjectDto>(ref reader, ProjectJson.DtoOptions)
            ?? throw new JsonException("Project JSON did not contain an object.");

        if (dto.SchemaVersion > ProjectSchema.CurrentVersion)
        {
            throw new JsonException(
                $"Project schema version {dto.SchemaVersion} is newer than the supported version {ProjectSchema.CurrentVersion}. " +
                "Open the project with a newer MOCASSIN release.");
        }

        if (dto.SchemaVersion < ProjectSchema.CurrentVersion)
        {
            throw new JsonException(
                $"Project schema version {dto.SchemaVersion} must be migrated to version {ProjectSchema.CurrentVersion} before reading.");
        }

        if (dto.MasterSeed is null || dto.Name is null || dto.Model is null || dto.ViewSettings is null)
        {
            throw new JsonException("Project JSON is missing required envelope fields.");
        }

        return new Project(
            dto.Schema,
            dto.SchemaVersion,
            new ProjectMetadata(
                dto.ProjectId,
                dto.MasterSeed,
                dto.Name,
                dto.Description,
                dto.CreatedUtc,
                dto.ModifiedUtc,
                dto.Author,
                dto.Notes),
            dto.Model,
            dto.Parameterizations,
            dto.JobTemplates,
            dto.BuildTemplates,
            dto.ViewSettings);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Project value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var parameterizations = value.Parameterizations
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var jobTemplates = value.JobTemplates
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var buildTemplates = value.BuildTemplates
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var objectStyles = value.ViewSettings.ObjectStyles
            .OrderBy(static item => item.ObjectId)
            .Select(static item => new ObjectStyle(
                item.ObjectId,
                item.Visible,
                item.Color,
                item.Material,
                item.Quality,
                item.Scale,
                item.Wireframe,
                item.AdditionalProperties
                    .OrderBy(static property => property.Key, StringComparer.Ordinal)
                    .ToImmutableArray()))
            .ToImmutableArray();
        var viewSettings = new ViewSettings(
            value.ViewSettings.Camera,
            value.ViewSettings.RenderBox,
            value.ViewSettings.Background,
            objectStyles);

        var dto = new ProjectDto
        {
            Schema = value.Schema,
            SchemaVersion = value.SchemaVersion,
            ProjectId = value.Metadata.ProjectId,
            MasterSeed = value.Metadata.MasterSeed,
            Name = value.Metadata.Name,
            Description = value.Metadata.Description,
            Author = value.Metadata.Author,
            Notes = value.Metadata.Notes,
            CreatedUtc = value.Metadata.CreatedUtc,
            ModifiedUtc = value.Metadata.ModifiedUtc,
            Model = value.Model,
            Parameterizations = parameterizations,
            JobTemplates = jobTemplates,
            BuildTemplates = buildTemplates,
            ViewSettings = viewSettings,
        };

        JsonSerializer.Serialize(writer, dto, ProjectJson.DtoOptions);
    }
}

/// <summary>
/// Internal flat representation of the on-disk project envelope.
/// </summary>
internal sealed class ProjectDto
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    [JsonRequired]
    public int SchemaVersion { get; init; }

    [JsonRequired]
    public Guid ProjectId { get; init; }

    [JsonRequired]
    public string? MasterSeed { get; init; }

    [JsonRequired]
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Author { get; init; }

    public string? Notes { get; init; }

    [JsonRequired]
    [JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    public DateTimeOffset CreatedUtc { get; init; }

    [JsonRequired]
    [JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    public DateTimeOffset ModifiedUtc { get; init; }

    [JsonRequired]
    public ModelDefinition? Model { get; init; }

    [JsonRequired]
    public ImmutableArray<ParameterizationTemplate> Parameterizations { get; init; }

    [JsonRequired]
    public ImmutableArray<JobTemplate> JobTemplates { get; init; }

    [JsonRequired]
    public ImmutableArray<BuildTemplate> BuildTemplates { get; init; }

    [JsonRequired]
    public ViewSettings? ViewSettings { get; init; }
}

/// <summary>
/// Source-generated JSON metadata for project DTOs and nested values.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.Strict,
    WriteIndented = false)]
[JsonSerializable(typeof(ProjectDto))]
[JsonSerializable(typeof(ModelDefinition))]
[JsonSerializable(typeof(ProjectMetadata))]
[JsonSerializable(typeof(ParameterizationTemplate))]
[JsonSerializable(typeof(JobTemplate))]
[JsonSerializable(typeof(BuildTemplate))]
[JsonSerializable(typeof(ViewSettings))]
[JsonSerializable(typeof(CameraSettings))]
[JsonSerializable(typeof(RenderBoxSettings))]
[JsonSerializable(typeof(ObjectStyle))]
[JsonSerializable(typeof(StyleProperty))]
[JsonSerializable(typeof(StyleValue))]
internal sealed partial class ProjectJsonContext : JsonSerializerContext;
