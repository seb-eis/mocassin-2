// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model;

/// <summary>
/// Serializes the flat model envelope and applies defaults for missing sections.
/// </summary>
/// <remarks>
/// Missing <c>particles</c>, <c>particleSets</c>, or <c>structure</c> properties load as the empty/default
/// model. Unknown or duplicate properties are rejected to preserve the project-format contract.
/// </remarks>
public sealed class ModelDefinitionJsonConverter : JsonConverter<ModelDefinition>
{
    /// <inheritdoc />
    public override ModelDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Model must be a JSON object.");
        }

        var particles = ImmutableArray<Particle>.Empty;
        var particleSets = ImmutableArray<ParticleSet>.Empty;
        var structure = StructureModel.Default;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Model property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "particles":
                    particles = ReadArray<Particle>(property.Value, options, "particles");
                    break;
                case "particleSets":
                    particleSets = ReadArray<ParticleSet>(property.Value, options, "particleSets");
                    break;
                case "structure":
                    structure = property.Value.Deserialize<StructureModel>(options)
                        ?? throw new JsonException("Model structure must not be null.");
                    break;
                default:
                    throw new JsonException($"Unknown model property '{property.Name}'.");
            }
        }

        return new ModelDefinition
        {
            Particles = particles,
            ParticleSets = particleSets,
            Structure = structure,
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ModelDefinition value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WritePropertyName("particles");
        JsonSerializer.Serialize(writer, value.Particles, options);
        writer.WritePropertyName("particleSets");
        JsonSerializer.Serialize(writer, value.ParticleSets, options);
        writer.WritePropertyName("structure");
        JsonSerializer.Serialize(writer, value.Structure, options);
        writer.WriteEndObject();
    }

    private static ImmutableArray<T> ReadArray<T>(
        JsonElement element,
        JsonSerializerOptions options,
        string propertyName)
        where T : class
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Model {propertyName} must be an array.");
        }

        var values = element.Deserialize<ImmutableArray<T>>(options);
        if (values.IsDefault)
        {
            return [];
        }

        if (values.Any(static value => value is null))
        {
            throw new JsonException($"Model {propertyName} must not contain null elements.");
        }

        return values;
    }
}
