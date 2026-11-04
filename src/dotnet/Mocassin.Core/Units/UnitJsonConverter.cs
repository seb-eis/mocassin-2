// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Units;

/// <summary>
/// Serializes unit values as plain JSON numbers and validates them on read.
/// </summary>
/// <typeparam name="TUnit">The unit type.</typeparam>
public sealed class UnitJsonConverter<TUnit> : JsonConverter<TUnit>
    where TUnit : struct, IUnitValue<TUnit>
{
    /// <inheritdoc />
    public override TUnit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"{typeof(TUnit).Name} must be a JSON number.");
        }

        var value = reader.GetDouble();
        if (!TUnit.TryCreate(value, out var result))
        {
            throw new JsonException($"'{value}' is not a valid {typeof(TUnit).Name} value.");
        }

        return result;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TUnit value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value.Value);
    }
}
