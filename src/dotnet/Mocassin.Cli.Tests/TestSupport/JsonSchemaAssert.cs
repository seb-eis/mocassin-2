// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Cli.Tests.TestSupport;

/// <summary>
/// Validates a report against a generated contract schema.
/// </summary>
internal static class JsonSchemaAssert
{
    /// <summary>
    /// Asserts that the JSON text validates against the generated schema.
    /// </summary>
    /// <param name="schemaName">The schema stem, for example <c>build-v1</c>.</param>
    /// <param name="json">The report JSON.</param>
    public static void Matches(string schemaName, string json)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        ArgumentNullException.ThrowIfNull(json);
        var schema = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Combine(
                "contracts",
                "generated",
                "schema",
                schemaName + ".schema.json")));
        using var document = JsonDocument.Parse(json);
        var errors = new List<string>();
        Validate(schema.RootElement, document.RootElement, "$", errors);
        Assert.True(errors.Count == 0, string.Join(" | ", errors));
    }

    private static void Validate(JsonElement schema, JsonElement value, string path, List<string> errors)
    {
        if (schema.TryGetProperty("type", out var type))
        {
            if (!MatchesType(type, value))
            {
                errors.Add($"{path}: expected {type.GetRawText()}, got {value.ValueKind}");
                return;
            }
        }

        if (schema.TryGetProperty("enum", out var enumValues) &&
            !enumValues.EnumerateArray().Any(item => JsonElement.DeepEquals(item, value)))
        {
            errors.Add($"{path}: value {value.GetRawText()} is not in the schema enum");
        }

        if (value.ValueKind == JsonValueKind.Object &&
            schema.TryGetProperty("properties", out var properties))
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (value.TryGetProperty(property.Name, out var child))
                {
                    Validate(property.Value, child, $"{path}.{property.Name}", errors);
                }
            }

            if (schema.TryGetProperty("required", out var required))
            {
                foreach (var name in required.EnumerateArray())
                {
                    if (!value.TryGetProperty(name.GetString()!, out _))
                    {
                        errors.Add($"{path}: missing required property {name.GetString()}");
                    }
                }
            }

            if (schema.TryGetProperty("additionalProperties", out var additional) &&
                additional.ValueKind == JsonValueKind.False)
            {
                foreach (var property in value.EnumerateObject())
                {
                    if (!properties.TryGetProperty(property.Name, out _))
                    {
                        errors.Add($"{path}: unexpected property {property.Name}");
                    }
                }
            }
        }

        if (value.ValueKind == JsonValueKind.Array &&
            schema.TryGetProperty("items", out var items))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                Validate(items, item, $"{path}[{index}]", errors);
                index++;
            }
        }
    }

    private static bool MatchesType(JsonElement type, JsonElement value)
    {
        if (type.ValueKind == JsonValueKind.Array)
        {
            return type.EnumerateArray().Any(item => MatchesType(item, value));
        }

        return type.GetString() switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "number" => value.ValueKind == JsonValueKind.Number,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => false,
        };
    }
}
