// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Ids;

/// <summary>
/// Serializes strongly typed UUID identifiers as lowercase hyphenated UUID strings.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public sealed class TypedIdJsonConverter<TId> : JsonConverter<TId>
    where TId : struct, ITypedId<TId>
{
    /// <inheritdoc />
    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeof(TId).Name} must be a UUID string.");
        }

        var text = reader.GetString();
        if (!Guid.TryParseExact(text, "D", out var value))
        {
            throw new JsonException($"'{text}' is not a valid UUID value for {typeof(TId).Name}.");
        }

        return TId.Create(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.Value.ToString("D", CultureInfo.InvariantCulture));
    }
}
