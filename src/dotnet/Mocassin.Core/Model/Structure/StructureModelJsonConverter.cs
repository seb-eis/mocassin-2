// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// Serializes the structure model and applies the default structure for missing fields.
/// </summary>
/// <remarks>
/// An empty <c>structure</c> object loads as <see cref="StructureModel.Default"/>. Unknown or duplicate
/// properties are rejected to preserve the project-format contract.
/// </remarks>
public sealed class StructureModelJsonConverter : JsonConverter<StructureModel>
{
    /// <inheritdoc />
    public override StructureModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Structure must be a JSON object.");
        }

        var defaults = StructureModel.Default;
        var name = defaults.Name;
        var spaceGroup = defaults.SpaceGroup;
        var cell = defaults.Cell;
        var sites = ImmutableArray<CellSite>.Empty;
        var dummyPositions = ImmutableArray<CellDummyPosition>.Empty;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Structure property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "name":
                    name = property.Value.GetString()
                        ?? throw new JsonException("Structure name must be a string.");
                    break;
                case "spaceGroup":
                    spaceGroup = property.Value.Deserialize<SpaceGroupRef>(options)
                        ?? throw new JsonException("Structure space group must not be null.");
                    break;
                case "cell":
                    cell = property.Value.Deserialize<CellParameters>(options)
                        ?? throw new JsonException("Structure cell must not be null.");
                    break;
                case "sites":
                    sites = ReadArray<CellSite>(property.Value, options, "sites");
                    break;
                case "dummyPositions":
                    dummyPositions = ReadArray<CellDummyPosition>(property.Value, options, "dummyPositions");
                    break;
                default:
                    throw new JsonException($"Unknown structure property '{property.Name}'.");
            }
        }

        return new StructureModel(name, spaceGroup, cell, sites, dummyPositions);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, StructureModel value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        writer.WritePropertyName("spaceGroup");
        JsonSerializer.Serialize(writer, value.SpaceGroup, options);
        writer.WritePropertyName("cell");
        JsonSerializer.Serialize(writer, value.Cell, options);
        writer.WritePropertyName("sites");
        JsonSerializer.Serialize(writer, value.Sites, options);
        writer.WritePropertyName("dummyPositions");
        JsonSerializer.Serialize(writer, value.DummyPositions, options);
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
            throw new JsonException($"Structure {propertyName} must be an array.");
        }

        var values = element.Deserialize<ImmutableArray<T>>(options);
        if (values.IsDefault)
        {
            return [];
        }

        if (values.Any(static value => value is null))
        {
            throw new JsonException($"Structure {propertyName} must not contain null elements.");
        }

        return values;
    }
}
