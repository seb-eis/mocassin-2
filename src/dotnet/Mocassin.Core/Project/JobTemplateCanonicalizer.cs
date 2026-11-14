// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using Mocassin.Core.Model.Jobs;

namespace Mocassin.Core.Project;

/// <summary>
/// Produces the canonical representation of job templates for persistence and hashing.
/// </summary>
internal static class JobTemplateCanonicalizer
{
    /// <summary>
    /// Normalizes one job template against the owning project.
    /// </summary>
    /// <param name="project">The owning project, used to resolve doping priorities.</param>
    /// <param name="template">The template to normalize.</param>
    /// <returns>The normalized template.</returns>
    public static JobTemplate Normalize(Project project, JobTemplate template)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(template);

        return template with
        {
            Name = template.Name ?? string.Empty,
            Collections = NormalizeCollections(project, template.Collections),
        };
    }

    private static ImmutableArray<JobCollection>? NormalizeCollections(
        Project project,
        ImmutableArray<JobCollection>? collections)
    {
        if (collections is null || collections.Value.IsDefaultOrEmpty)
        {
            return null;
        }

        return
        [
            .. collections.Value
                .Where(static collection => collection is not null)
                .Select(collection => NormalizeCollection(project, collection)),
        ];
    }

    private static JobCollection NormalizeCollection(Project project, JobCollection collection)
    {
        var optimizers = collection.Optimizers;
        var configs = collection.Configs;

        return collection with
        {
            Name = collection.Name ?? string.Empty,
            BaseSettings = NormalizeSettings(collection.BaseSettings),
            RngSeed = string.IsNullOrEmpty(collection.RngSeed) ? null : collection.RngSeed,
            Optimizers = optimizers is { IsDefaultOrEmpty: false } ? optimizers : null,
            Configs = configs is null || configs.Value.IsDefaultOrEmpty
                ? null
                : [.. configs.Value.Where(static config => config is not null).Select(config => NormalizeConfig(project, config))],
        };
    }

    private static JobConfig NormalizeConfig(Project project, JobConfig config)
    {
        var dopings = config.Dopings;
        var priorities = project.Model.Dopings
            .Where(static doping => doping is not null)
            .ToDictionary(static doping => doping.Id, static doping => doping.Priority);

        return config with
        {
            Name = config.Name ?? string.Empty,
            Lattice = config.Lattice ?? new LatticeRequest(0, 0, 0),
            Overrides = NormalizeSettings(config.Overrides),
            Dopings = dopings is null || dopings.Value.IsDefaultOrEmpty
                ? null
                : [.. dopings.Value
                    .Where(static doping => doping is not null)
                    .OrderBy(doping => priorities.TryGetValue(doping.DopingId, out var priority) ? priority : int.MaxValue)
                    .ThenBy(static doping => doping.DopingId.Value)],
        };
    }

    private static JobSettings? NormalizeSettings(JobSettings? settings)
    {
        if (settings is null)
        {
            return null;
        }

        return settings with
        {
            Instruction = string.IsNullOrEmpty(settings.Instruction) ? null : settings.Instruction,
            Extensions = NormalizeExtensions(settings.Extensions),
        };
    }

    private static ImmutableArray<JobExtension>? NormalizeExtensions(ImmutableArray<JobExtension>? extensions)
    {
        if (extensions is null)
        {
            return null;
        }

        if (extensions.Value.IsDefaultOrEmpty)
        {
            return [];
        }

        return
        [
            .. extensions.Value
                .Where(static extension => extension is not null)
                .OrderBy(static extension => extension.Id, StringComparer.Ordinal)
                .Select(static extension => extension with
                {
                    Id = extension.Id ?? string.Empty,
                    Config = CanonicalJson.Normalize(extension.Config),
                }),
        ];
    }
}

/// <summary>
/// Produces deterministic JSON for opaque extension configuration objects.
/// </summary>
internal static class CanonicalJson
{
    /// <summary>
    /// Normalizes an arbitrary JSON value: object properties are sorted ordinally, arrays keep their order, and
    /// numbers are written as IEEE-754 binary64 values.
    /// </summary>
    /// <param name="element">The value to normalize.</param>
    /// <returns>The normalized value.</returns>
    public static JsonElement Normalize(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            Write(writer, element);
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(static property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteNumberValue(element.GetDouble());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                writer.WriteNullValue();
                break;
            default:
                throw new JsonException($"Unsupported JSON value kind '{element.ValueKind}'.");
        }
    }
}
