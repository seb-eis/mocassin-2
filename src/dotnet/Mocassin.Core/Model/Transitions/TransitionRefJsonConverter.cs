// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// Serializes a <see cref="TransitionRef"/> as <c>{ "kind": "...", "id": "..." }</c>.
/// </summary>
/// <remarks>
/// Unknown, duplicate, or missing properties are rejected. The kind uses the same camel-case enum rules as the
/// other project enums.
/// </remarks>
public sealed class TransitionRefJsonConverter : JsonConverter<TransitionRef>
{
    /// <inheritdoc />
    public override TransitionRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Transition reference must be a JSON object.");
        }

        TransitionKind? kind = null;
        Guid? id = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Transition reference property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "kind":
                    kind = property.Value.Deserialize<TransitionKind>(options);
                    break;
                case "id":
                    if (property.Value.ValueKind != JsonValueKind.String ||
                        !Guid.TryParseExact(property.Value.GetString(), "D", out var value))
                    {
                        throw new JsonException("Transition reference 'id' must be a UUID string.");
                    }

                    id = value;
                    break;
                default:
                    throw new JsonException($"Unknown transition reference property '{property.Name}'.");
            }
        }

        if (kind is null || id is null)
        {
            throw new JsonException("Transition reference requires both 'kind' and 'id'.");
        }

        return new TransitionRef(kind.Value, id.Value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TransitionRef value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        writer.WritePropertyName("kind");
        JsonSerializer.Serialize(writer, value.Kind, options);
        writer.WriteString("id", value.Id.ToString("D", CultureInfo.InvariantCulture));
        writer.WriteEndObject();
    }
}
