// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model;

/// <summary>
/// Serializes the flat model envelope and applies defaults for missing sections.
/// </summary>
/// <remarks>
/// Missing <c>particles</c>, <c>particleSets</c>, <c>structure</c>, or transition properties load as the
/// empty/default model. Unknown or duplicate properties are rejected to preserve the project-format contract.
/// Empty optional transition sections are omitted when writing so that transition-free projects keep their
/// Step 01/03 canonical model and document hashes.
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
        var stateChanges = ImmutableArray<StateChange>.Empty;
        var stateChangeGroups = ImmutableArray<StateChangeGroup>.Empty;
        var chains = ImmutableArray<TransitionChain>.Empty;
        var kineticTransitions = ImmutableArray<KineticTransition>.Empty;
        var metropolisTransitions = ImmutableArray<MetropolisTransition>.Empty;
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
                case "stateChanges":
                    stateChanges = ReadArray<StateChange>(property.Value, options, "stateChanges");
                    break;
                case "stateChangeGroups":
                    stateChangeGroups = ReadArray<StateChangeGroup>(property.Value, options, "stateChangeGroups");
                    break;
                case "chains":
                    chains = ReadArray<TransitionChain>(property.Value, options, "chains");
                    break;
                case "kineticTransitions":
                    kineticTransitions = ReadArray<KineticTransition>(property.Value, options, "kineticTransitions");
                    break;
                case "metropolisTransitions":
                    metropolisTransitions = ReadArray<MetropolisTransition>(
                        property.Value,
                        options,
                        "metropolisTransitions");
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
            StateChanges = stateChanges,
            StateChangeGroups = stateChangeGroups,
            Chains = chains,
            KineticTransitions = kineticTransitions,
            MetropolisTransitions = metropolisTransitions,
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
        WriteArrayIfNotEmpty(writer, "stateChanges", value.StateChanges, options);
        WriteArrayIfNotEmpty(writer, "stateChangeGroups", value.StateChangeGroups, options);
        WriteArrayIfNotEmpty(writer, "chains", value.Chains, options);
        WriteArrayIfNotEmpty(writer, "kineticTransitions", value.KineticTransitions, options);
        WriteArrayIfNotEmpty(writer, "metropolisTransitions", value.MetropolisTransitions, options);
        writer.WriteEndObject();
    }

    private static void WriteArrayIfNotEmpty<T>(
        Utf8JsonWriter writer,
        string propertyName,
        ImmutableArray<T> values,
        JsonSerializerOptions options)
    {
        if (values.IsDefaultOrEmpty)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, values, options);
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
