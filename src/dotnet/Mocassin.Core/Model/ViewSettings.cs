// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Core.Model;

/// <summary>
/// Projection mode for the 3D viewport.
/// </summary>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<CameraProjection>))]
public enum CameraProjection
{
    /// <summary>
    /// Perspective projection.
    /// </summary>
    Perspective,

    /// <summary>
    /// Orthographic projection.
    /// </summary>
    Orthographic,
}

/// <summary>
/// Camera state persisted with the project.
/// </summary>
public sealed record CameraSettings
{
    /// <summary>
    /// Initializes camera settings.
    /// </summary>
    /// <param name="position">The camera position as an x/y/z array.</param>
    /// <param name="target">The look-at target as an x/y/z array.</param>
    /// <param name="up">The up vector as an x/y/z array.</param>
    /// <param name="projection">The projection mode.</param>
    /// <param name="fieldOfViewDegrees">The vertical field of view in degrees.</param>
    /// <param name="nearPlane">The near clipping plane.</param>
    /// <param name="farPlane">The far clipping plane.</param>
    public CameraSettings(
        ImmutableArray<double> position,
        ImmutableArray<double> target,
        ImmutableArray<double> up,
        CameraProjection projection,
        double fieldOfViewDegrees,
        double nearPlane,
        double farPlane)
    {
        Position = position.IsDefault ? [] : position;
        Target = target.IsDefault ? [] : target;
        Up = up.IsDefault ? [] : up;
        Projection = projection;
        FieldOfViewDegrees = fieldOfViewDegrees;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    /// <summary>
    /// Gets the camera position as an x/y/z array.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleArrayJsonConverter))]
    public ImmutableArray<double> Position { get; }

    /// <summary>
    /// Gets the look-at target as an x/y/z array.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleArrayJsonConverter))]
    public ImmutableArray<double> Target { get; }

    /// <summary>
    /// Gets the up vector as an x/y/z array.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleArrayJsonConverter))]
    public ImmutableArray<double> Up { get; }

    /// <summary>
    /// Gets the projection mode.
    /// </summary>
    public CameraProjection Projection { get; }

    /// <summary>
    /// Gets the vertical field of view in degrees.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleJsonConverter))]
    public double FieldOfViewDegrees { get; }

    /// <summary>
    /// Gets the near clipping plane.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleJsonConverter))]
    public double NearPlane { get; }

    /// <summary>
    /// Gets the far clipping plane.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleJsonConverter))]
    public double FarPlane { get; }
}

/// <summary>
/// Render-box state persisted with the project.
/// </summary>
public sealed record RenderBoxSettings
{
    /// <summary>
    /// Initializes render-box settings.
    /// </summary>
    /// <param name="min">The minimum corner as an x/y/z array.</param>
    /// <param name="max">The maximum corner as an x/y/z array.</param>
    /// <param name="visible">Whether the render box is visible.</param>
    public RenderBoxSettings(ImmutableArray<double> min, ImmutableArray<double> max, bool visible)
    {
        Min = min.IsDefault ? [] : min;
        Max = max.IsDefault ? [] : max;
        Visible = visible;
    }

    /// <summary>
    /// Gets the minimum corner as an x/y/z array.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleArrayJsonConverter))]
    public ImmutableArray<double> Min { get; }

    /// <summary>
    /// Gets the maximum corner as an x/y/z array.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleArrayJsonConverter))]
    public ImmutableArray<double> Max { get; }

    /// <summary>
    /// Gets a value indicating whether the render box is visible.
    /// </summary>
    public bool Visible { get; }
}

/// <summary>
/// The kind of a persisted style value.
/// </summary>
#pragma warning disable CA1720 // The member names describe JSON value kinds, not CLR type names.
public enum StyleValueKind
{
    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// A numeric value.
    /// </summary>
    Number,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean,
}
#pragma warning restore CA1720

/// <summary>
/// A primitive style value that survives round-trips even when the current UI does not interpret it.
/// </summary>
[JsonConverter(typeof(StyleValueJsonConverter))]
public readonly record struct StyleValue
{
    private StyleValue(StyleValueKind kind, string? stringValue, double numberValue, bool booleanValue)
    {
        Kind = kind;
        StringValue = stringValue;
        NumberValue = numberValue;
        BooleanValue = booleanValue;
    }

    /// <summary>
    /// Gets the value kind.
    /// </summary>
    public StyleValueKind Kind { get; }

    /// <summary>
    /// Gets the string value when <see cref="Kind"/> is <see cref="StyleValueKind.String"/>.
    /// </summary>
    public string? StringValue { get; }

    /// <summary>
    /// Gets the numeric value when <see cref="Kind"/> is <see cref="StyleValueKind.Number"/>.
    /// </summary>
    public double NumberValue { get; }

    /// <summary>
    /// Gets the Boolean value when <see cref="Kind"/> is <see cref="StyleValueKind.Boolean"/>.
    /// </summary>
    public bool BooleanValue { get; }

    /// <summary>
    /// Creates a string style value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The style value.</returns>
    public static StyleValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new StyleValue(StyleValueKind.String, value, 0.0, false);
    }

    /// <summary>
    /// Creates a numeric style value.
    /// </summary>
    /// <param name="value">The finite numeric value.</param>
    /// <returns>The style value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    public static StyleValue FromNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Style numbers must be finite.");
        }

        return new StyleValue(StyleValueKind.Number, null, value == 0.0 ? 0.0 : value, false);
    }

    /// <summary>
    /// Creates a Boolean style value.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The style value.</returns>
    public static StyleValue FromBoolean(bool value) => new(StyleValueKind.Boolean, null, 0.0, value);
}

/// <summary>
/// One additional, optionally uninterpreted style property.
/// </summary>
/// <param name="Key">The style property key.</param>
/// <param name="Value">The primitive style value.</param>
public sealed record StyleProperty(string Key, StyleValue Value);

/// <summary>
/// Per-object view style. Additional primitive properties are preserved even when the renderer does
/// not understand them yet.
/// </summary>
[JsonConverter(typeof(ObjectStyleJsonConverter))]
public sealed record ObjectStyle
{
    /// <summary>
    /// Initializes an object style.
    /// </summary>
    /// <param name="objectId">The stable object identifier.</param>
    /// <param name="visible">Whether the object is visible.</param>
    /// <param name="color">The optional color key.</param>
    /// <param name="material">The optional material key.</param>
    /// <param name="quality">The optional quality key.</param>
    /// <param name="scale">The optional scale factor.</param>
    /// <param name="wireframe">Whether the object is rendered as wireframe.</param>
    /// <param name="additionalProperties">Additional primitive style properties.</param>
    public ObjectStyle(
        Guid objectId,
        bool visible,
        string? color,
        string? material,
        string? quality,
        double scale,
        bool wireframe,
        ImmutableArray<StyleProperty> additionalProperties)
    {
        ObjectId = objectId;
        Visible = visible;
        Color = color;
        Material = material;
        Quality = quality;
        Scale = scale;
        Wireframe = wireframe;
        AdditionalProperties = additionalProperties.IsDefault ? [] : additionalProperties;
    }

    /// <summary>
    /// Gets the stable object identifier.
    /// </summary>
    public Guid ObjectId { get; }

    /// <summary>
    /// Gets a value indicating whether the object is visible.
    /// </summary>
    public bool Visible { get; }

    /// <summary>
    /// Gets the optional color key.
    /// </summary>
    public string? Color { get; }

    /// <summary>
    /// Gets the optional material key.
    /// </summary>
    public string? Material { get; }

    /// <summary>
    /// Gets the optional quality key.
    /// </summary>
    public string? Quality { get; }

    /// <summary>
    /// Gets the optional scale factor.
    /// </summary>
    [JsonConverter(typeof(CanonicalDoubleJsonConverter))]
    public double Scale { get; }

    /// <summary>
    /// Gets a value indicating whether the object is rendered as wireframe.
    /// </summary>
    public bool Wireframe { get; }

    /// <summary>
    /// Gets additional primitive style properties.
    /// </summary>
    public ImmutableArray<StyleProperty> AdditionalProperties { get; }
}

/// <summary>
/// Camera, render-box, and per-object style state persisted with the project.
/// </summary>
public sealed record ViewSettings
{
    /// <summary>
    /// Initializes view settings.
    /// </summary>
    /// <param name="camera">The camera state.</param>
    /// <param name="renderBox">The render-box state.</param>
    /// <param name="background">The optional background key.</param>
    /// <param name="objectStyles">The per-object styles.</param>
    public ViewSettings(
        CameraSettings camera,
        RenderBoxSettings renderBox,
        string? background,
        ImmutableArray<ObjectStyle> objectStyles)
    {
        Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        RenderBox = renderBox ?? throw new ArgumentNullException(nameof(renderBox));
        Background = background;
        ObjectStyles = objectStyles.IsDefault ? [] : objectStyles;
    }

    /// <summary>
    /// Gets the default view settings for a new project.
    /// </summary>
    public static ViewSettings Default { get; } = new(
        new CameraSettings(
            [0.0, 0.0, 0.0],
            [0.0, 0.0, -1.0],
            [0.0, 1.0, 0.0],
            CameraProjection.Perspective,
            45.0,
            0.1,
            1000.0),
        new RenderBoxSettings([-1.0, -1.0, -1.0], [1.0, 1.0, 1.0], true),
        null,
        []);

    /// <summary>
    /// Gets the camera state.
    /// </summary>
    public CameraSettings Camera { get; }

    /// <summary>
    /// Gets the render-box state.
    /// </summary>
    public RenderBoxSettings RenderBox { get; }

    /// <summary>
    /// Gets the optional background key.
    /// </summary>
    public string? Background { get; }

    /// <summary>
    /// Gets the per-object styles.
    /// </summary>
    public ImmutableArray<ObjectStyle> ObjectStyles { get; }
}

/// <summary>
/// Serializes an enum as a camel-case string.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class CamelCaseEnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"{typeof(TEnum).Name} must be a string.");
        }

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text) ||
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
            !Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) ||
            !Enum.IsDefined(value))
        {
            throw new JsonException($"'{text}' is not a valid {typeof(TEnum).Name} value.");
        }

        return value;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(JsonNamingPolicy.CamelCase.ConvertName(value.ToString()));
    }
}

/// <summary>
/// Serializes a primitive style value.
/// </summary>
public sealed class StyleValueJsonConverter : JsonConverter<StyleValue>
{
    /// <inheritdoc />
    public override StyleValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => StyleValue.FromString(reader.GetString()!),
            JsonTokenType.Number => StyleValue.FromNumber(reader.GetDouble()),
            JsonTokenType.True => StyleValue.FromBoolean(true),
            JsonTokenType.False => StyleValue.FromBoolean(false),
            _ => throw new JsonException("Style values must be strings, numbers, or booleans."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, StyleValue value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        switch (value.Kind)
        {
            case StyleValueKind.String:
                writer.WriteStringValue(value.StringValue);
                break;
            case StyleValueKind.Number:
                writer.WriteNumberValue(value.NumberValue == 0.0 ? 0.0 : value.NumberValue);
                break;
            case StyleValueKind.Boolean:
                writer.WriteBooleanValue(value.BooleanValue);
                break;
            default:
                throw new JsonException($"Unsupported style value kind '{value.Kind}'.");
        }
    }
}

/// <summary>
/// Serializes an object style as a flat object with known and additional primitive style keys.
/// </summary>
public sealed class ObjectStyleJsonConverter : JsonConverter<ObjectStyle>
{
    /// <inheritdoc />
    public override ObjectStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Object style must be a JSON object.");
        }

        var objectId = Guid.Empty;
        var visible = false;
        string? color = null;
        string? material = null;
        string? quality = null;
        var scale = 1.0;
        var wireframe = false;
        var additional = ImmutableArray.CreateBuilder<StyleProperty>();
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!keys.Add(property.Name))
            {
                throw new JsonException($"Style property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "objectId":
                    if (!Guid.TryParseExact(property.Value.GetString(), "D", out objectId))
                    {
                        throw new JsonException("Object style 'objectId' must be a UUID string.");
                    }

                    break;
                case "visible":
                    visible = property.Value.GetBoolean();
                    break;
                case "color":
                    color = property.Value.GetString();
                    break;
                case "material":
                    material = property.Value.GetString();
                    break;
                case "quality":
                    quality = property.Value.GetString();
                    break;
                case "scale":
                    scale = property.Value.GetDouble();
                    if (!double.IsFinite(scale))
                    {
                        throw new JsonException("Object style 'scale' must be finite.");
                    }

                    break;
                case "wireframe":
                    wireframe = property.Value.GetBoolean();
                    break;
                default:
                    additional.Add(new StyleProperty(property.Name, ReadStyleValue(property.Value)));
                    break;
            }
        }

        if (objectId == Guid.Empty)
        {
            throw new JsonException("Object style 'objectId' is required.");
        }

        return new ObjectStyle(objectId, visible, color, material, quality, scale, wireframe, additional.ToImmutable());
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ObjectStyle value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("objectId", value.ObjectId.ToString("D"));
        writer.WriteBoolean("visible", value.Visible);
        if (value.Color is not null)
        {
            writer.WriteString("color", value.Color);
        }

        if (value.Material is not null)
        {
            writer.WriteString("material", value.Material);
        }

        if (value.Quality is not null)
        {
            writer.WriteString("quality", value.Quality);
        }

        writer.WriteNumber("scale", value.Scale == 0.0 ? 0.0 : value.Scale);
        writer.WriteBoolean("wireframe", value.Wireframe);

        foreach (var property in value.AdditionalProperties.OrderBy(static item => item.Key, StringComparer.Ordinal))
        {
            if (property.Key is "objectId" or "visible" or "color" or "material" or "quality" or "scale" or "wireframe")
            {
                throw new InvalidOperationException($"Additional style property '{property.Key}' collides with a known key.");
            }

            writer.WritePropertyName(property.Key);
            WriteStyleValue(writer, property.Value);
        }

        writer.WriteEndObject();
    }

    private static StyleValue ReadStyleValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => StyleValue.FromString(element.GetString()!),
        JsonValueKind.Number => StyleValue.FromNumber(element.GetDouble()),
        JsonValueKind.True => StyleValue.FromBoolean(true),
        JsonValueKind.False => StyleValue.FromBoolean(false),
        _ => throw new JsonException("Additional style values must be strings, numbers, or booleans."),
    };

    private static void WriteStyleValue(Utf8JsonWriter writer, StyleValue value)
    {
        switch (value.Kind)
        {
            case StyleValueKind.String:
                writer.WriteStringValue(value.StringValue);
                break;
            case StyleValueKind.Number:
                writer.WriteNumberValue(value.NumberValue == 0.0 ? 0.0 : value.NumberValue);
                break;
            case StyleValueKind.Boolean:
                writer.WriteBooleanValue(value.BooleanValue);
                break;
            default:
                throw new JsonException($"Unsupported style value kind '{value.Kind}'.");
        }
    }
}

/// <summary>
/// Serializes a fixed number of doubles and normalizes negative zero to positive zero.
/// </summary>
public sealed class CanonicalDoubleArrayJsonConverter : JsonConverter<ImmutableArray<double>>
{
    /// <inheritdoc />
    public override ImmutableArray<double> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Vector values must be JSON arrays of numbers.");
        }

        var values = ImmutableArray.CreateBuilder<double>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToImmutable();
            }

            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("Vector values must be JSON numbers.");
            }

            var value = reader.GetDouble();
            if (!double.IsFinite(value))
            {
                throw new JsonException("Vector values must be finite.");
            }

            values.Add(value == 0.0 ? 0.0 : value);
        }

        throw new JsonException("Unexpected end of a vector array.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ImmutableArray<double> value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteNumberValue(item == 0.0 ? 0.0 : item);
        }

        writer.WriteEndArray();
    }
}

/// <summary>
/// Serializes a finite double and normalizes negative zero to positive zero.
/// </summary>
public sealed class CanonicalDoubleJsonConverter : JsonConverter<double>
{
    /// <inheritdoc />
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Value must be a JSON number.");
        }

        var value = reader.GetDouble();
        return value == 0.0 ? 0.0 : value;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value == 0.0 ? 0.0 : value);
    }
}
