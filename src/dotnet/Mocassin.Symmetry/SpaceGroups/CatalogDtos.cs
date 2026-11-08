// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Symmetry.SpaceGroups;

/// <summary>
/// Root DTO for the committed space-group catalog.
/// </summary>
internal sealed class SpaceGroupCatalogDto
{
    public int SchemaVersion { get; set; }

    public string DataVersion { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string SourceVersion { get; set; } = string.Empty;

    public CatalogSelectionDto? Selection { get; set; }

    public SpaceGroupSettingDto[] Settings { get; set; } = [];
}

/// <summary>
/// DTO for the catalog selection metadata.
/// </summary>
internal sealed class CatalogSelectionDto
{
    public string Rule { get; set; } = string.Empty;

    public int Standard { get; set; }

    public int OriginChoice { get; set; }

    [JsonPropertyName("monoclinicUniqueAxisBC")]
    public int MonoclinicUniqueAxisBc { get; set; }

    public int RhombohedralAxes { get; set; }

    public int Total { get; set; }
}

/// <summary>
/// DTO for one catalog setting, encoded as a positional JSON array.
/// </summary>
[JsonConverter(typeof(SpaceGroupSettingDtoConverter))]
internal sealed class SpaceGroupSettingDto
{
    public int Number { get; set; }

    public string HermannMauguin { get; set; } = string.Empty;

    public string HallSymbol { get; set; } = string.Empty;

    public string CrystalSystem { get; set; } = string.Empty;

    public string Axis { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;

    public SpaceGroupOperationDto[] Operations { get; set; } = [];
}

/// <summary>
/// DTO for one catalog symmetry operation: nine linear entries followed by three translation numerators.
/// </summary>
[JsonConverter(typeof(SpaceGroupOperationDtoConverter))]
internal sealed class SpaceGroupOperationDto
{
    public int[] Linear { get; set; } = [];

    public int[] Translation { get; set; } = [];
}

/// <summary>
/// Reads and writes the compact positional setting array.
/// </summary>
internal sealed class SpaceGroupSettingDtoConverter : JsonConverter<SpaceGroupSettingDto>
{
    /// <inheritdoc />
    public override SpaceGroupSettingDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("A catalog setting must be a JSON array.");
        }

        var number = ReadInt32(ref reader, "space-group number");
        var hermannMauguin = ReadString(ref reader, "Hermann–Mauguin symbol");
        var hallSymbol = ReadString(ref reader, "Hall symbol");
        var crystalSystem = ReadString(ref reader, "crystal system");
        var axis = ReadString(ref reader, "axis setting");
        var origin = ReadString(ref reader, "origin choice");
        reader.Read();
        var operations = JsonSerializer.Deserialize<SpaceGroupOperationDto[]>(ref reader, options)
            ?? throw new JsonException("Missing operations.");
        reader.Read();

        return new SpaceGroupSettingDto
        {
            Number = number,
            HermannMauguin = hermannMauguin,
            HallSymbol = hallSymbol,
            CrystalSystem = crystalSystem,
            Axis = axis,
            Origin = origin,
            Operations = operations,
        };
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        SpaceGroupSettingDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Number);
        writer.WriteStringValue(value.HermannMauguin);
        writer.WriteStringValue(value.HallSymbol);
        writer.WriteStringValue(value.CrystalSystem);
        writer.WriteStringValue(value.Axis);
        writer.WriteStringValue(value.Origin);
        JsonSerializer.Serialize(writer, value.Operations, options);
        writer.WriteEndArray();
    }

    private static int ReadInt32(ref Utf8JsonReader reader, string name)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"The {name} must be an integer.");
        }

        return reader.GetInt32();
    }

    private static string ReadString(ref Utf8JsonReader reader, string name)
    {
        reader.Read();
        return reader.TokenType == JsonTokenType.String
            ? reader.GetString() ?? throw new JsonException($"The {name} is missing.")
            : throw new JsonException($"The {name} must be a string.");
    }
}

/// <summary>
/// Reads and writes the compact 12-integer operation array.
/// </summary>
internal sealed class SpaceGroupOperationDtoConverter : JsonConverter<SpaceGroupOperationDto>
{
    /// <inheritdoc />
    public override SpaceGroupOperationDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("A catalog operation must be a JSON array.");
        }

        var values = new int[12];
        for (var index = 0; index < values.Length; index++)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("A catalog operation entry must be an integer.");
            }

            values[index] = reader.GetInt32();
        }

        reader.Read();

        return new SpaceGroupOperationDto
        {
            Linear = values[..9],
            Translation = values[9..],
        };
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        SpaceGroupOperationDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var entry in value.Linear)
        {
            writer.WriteNumberValue(entry);
        }

        foreach (var entry in value.Translation)
        {
            writer.WriteNumberValue(entry);
        }

        writer.WriteEndArray();
    }
}
