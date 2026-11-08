// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Geometry;

/// <summary>
/// Serializes fractional coordinates as a three-number JSON array <c>[a, b, c]</c>.
/// </summary>
public sealed class Fractional3JsonConverter : JsonConverter<Fractional3>
{
    /// <inheritdoc />
    public override Fractional3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Fractional coordinates must be a [a, b, c] array.");
        }

        var values = new double[3];
        for (var index = 0; index < values.Length; index++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetDouble(out values[index]))
            {
                throw new JsonException("Fractional coordinates must contain exactly three finite numbers.");
            }

            if (!double.IsFinite(values[index]))
            {
                throw new JsonException("Fractional coordinates must be finite.");
            }
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Fractional coordinates must contain exactly three finite numbers.");
        }

        return new Fractional3(values[0], values[1], values[2]);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Fractional3 value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartArray();
        writer.WriteNumberValue(Normalize(value.A));
        writer.WriteNumberValue(Normalize(value.B));
        writer.WriteNumberValue(Normalize(value.C));
        writer.WriteEndArray();
    }

    private static double Normalize(double value) => value == 0.0 ? 0.0 : value;
}
