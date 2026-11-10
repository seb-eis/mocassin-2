// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Serializes <see cref="ExecutionFlags"/> as a sorted array of camelCase flag names.
/// </summary>
/// <remarks>
/// <see cref="ExecutionFlags.None"/> is written as the empty array. Duplicate, numeric, combined, unknown, and
/// derived flag names are rejected; the project format exposes only the authored flags.
/// </remarks>
public sealed class ExecutionFlagsJsonConverter : JsonConverter<ExecutionFlags>
{
    private static readonly (ExecutionFlags Flag, string Name)[] authoredFlags =
    [
        (ExecutionFlags.NoSaving, "noSaving"),
        (ExecutionFlags.NoJumpLogging, "noJumpLogging"),
        (ExecutionFlags.UseFastExp, "useFastExp"),
        (ExecutionFlags.Deterministic, "deterministic"),
    ];

    private static readonly ExecutionFlags authoredMask =
        ExecutionFlags.NoSaving |
        ExecutionFlags.NoJumpLogging |
        ExecutionFlags.UseFastExp |
        ExecutionFlags.Deterministic;

    /// <summary>
    /// Gets the mask of user-authored execution flags.
    /// </summary>
    internal static ExecutionFlags AuthoredMask => authoredMask;

    /// <inheritdoc />
    public override ExecutionFlags Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Execution flags must be an array of camelCase names.");
        }

        var flags = ExecutionFlags.None;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return flags;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Execution flags must contain only string names.");
            }

            var name = reader.GetString();
            if (string.IsNullOrWhiteSpace(name) ||
                int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
                !Enum.TryParse<ExecutionFlags>(name, ignoreCase: true, out var flag) ||
                flag == ExecutionFlags.None ||
                (flag & ~authoredMask) != 0 ||
                ((int)flag & ((int)flag - 1)) != 0)
            {
                throw new JsonException($"'{name}' is not a valid authored execution flag.");
            }

            if ((flags & flag) != 0)
            {
                throw new JsonException($"Execution flag '{name}' is specified more than once.");
            }

            flags |= flag;
        }

        throw new JsonException("Execution flags array is not terminated.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ExecutionFlags value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if ((value & ~authoredMask) != 0)
        {
            throw new JsonException("Execution flags contain a derived or unknown flag.");
        }

        writer.WriteStartArray();
        foreach (var (flag, name) in authoredFlags)
        {
            if ((value & flag) != 0)
            {
                writer.WriteStringValue(name);
            }
        }

        writer.WriteEndArray();
    }
}
