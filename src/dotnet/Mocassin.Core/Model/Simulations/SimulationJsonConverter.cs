// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model.Simulations;

/// <summary>
/// Serializes a simulation as the flat project DTO with <c>kind</c>, the matching transition array, and a
/// <c>settings</c> object.
/// </summary>
/// <remarks>
/// The other kind's transition array and settings are absent, not <see langword="null"/>. A persisted shape
/// that disagrees with <c>kind</c> is loaded with the matching concrete record and reported as <c>SIM023</c>
/// by <c>SimulationModelValidator</c>. Unknown or duplicate properties are rejected.
/// </remarks>
public sealed class SimulationJsonConverter : JsonConverter<Simulation>
{
    /// <inheritdoc />
    public override Simulation Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Simulation must be a JSON object.");
        }

        SimulationId? id = null;
        string? name = null;
        SimulationKind? kind = null;
        var kineticTransitionIds = ImmutableArray<TransitionRef>.Empty;
        var metropolisTransitionIds = ImmutableArray<TransitionRef>.Empty;
        var hasKineticTransitionIds = false;
        var hasMetropolisTransitionIds = false;
        var settings = new SettingsBuilder();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Simulation property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "id":
                    id = ReadId(property.Value);
                    break;
                case "name":
                    name = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : throw new JsonException("Simulation 'name' must be a string.");
                    break;
                case "kind":
                    if (property.Value.ValueKind != JsonValueKind.String)
                    {
                        throw new JsonException("Simulation 'kind' must be a string.");
                    }

                    kind = property.Value.Deserialize<SimulationKind>(options);
                    break;
                case "kineticTransitionIds":
                    kineticTransitionIds = ReadTransitionIds(
                        property.Value,
                        TransitionKind.Kinetic,
                        "kineticTransitionIds");
                    hasKineticTransitionIds = true;
                    break;
                case "metropolisTransitionIds":
                    metropolisTransitionIds = ReadTransitionIds(
                        property.Value,
                        TransitionKind.Metropolis,
                        "metropolisTransitionIds");
                    hasMetropolisTransitionIds = true;
                    break;
                case "settings":
                    ReadSettings(property.Value, options, settings);
                    break;
                default:
                    throw new JsonException($"Unknown simulation property '{property.Name}'.");
            }
        }

        if (id is null || name is null || kind is null)
        {
            throw new JsonException("Simulation requires 'id', 'name', and 'kind'.");
        }

        return kind.Value switch
        {
            SimulationKind.Kmc => CreateKinetic(
                id.Value,
                name,
                kineticTransitionIds,
                hasMetropolisTransitionIds || settings.HasMmcOnly,
                settings),
            SimulationKind.Mmc => CreateMetropolis(
                id.Value,
                name,
                metropolisTransitionIds,
                hasKineticTransitionIds || settings.HasKmcOnly,
                settings),
            _ => throw new JsonException($"Unknown simulation kind '{kind.Value}'."),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Simulation value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteString("id", value.Id.Value.ToString("D", CultureInfo.InvariantCulture));
        writer.WriteString("name", value.Name);
        writer.WritePropertyName("kind");
        JsonSerializer.Serialize(writer, value.Kind, options);

        writer.WritePropertyName(
            value.Kind == SimulationKind.Kmc ? "kineticTransitionIds" : "metropolisTransitionIds");
        writer.WriteStartArray();
        foreach (var transition in value.TransitionIds)
        {
            writer.WriteStringValue(transition.Id.ToString("D", CultureInfo.InvariantCulture));
        }

        writer.WriteEndArray();

        writer.WritePropertyName("settings");
        writer.WriteStartObject();
        writer.WriteNumber("temperatureK", value.TemperatureK);
        writer.WriteNumber("targetMcsp", value.TargetMcsp);
        writer.WriteNumber("writeOutCount", value.WriteOutCount);
        WriteNullableNumber(writer, "timeLimitSeconds", value.TimeLimitSeconds);
        WriteNullableNumber(writer, "minSuccessRate", value.MinSuccessRate);
        writer.WriteNumber("jobCount", value.JobCount);
        if (value.RngSeed is { } seed)
        {
            writer.WriteString("rngSeed", seed);
        }
        else
        {
            writer.WriteNull("rngSeed");
        }

        writer.WritePropertyName("executionFlags");
        JsonSerializer.Serialize(writer, value.Flags, options);

        if (value.Instruction is { } instruction)
        {
            writer.WriteString("instruction", instruction);
        }
        else
        {
            writer.WriteNull("instruction");
        }

        switch (value)
        {
            case KineticSimulation kinetic:
                writer.WriteNumber("preRunMcsp", kinetic.PreRunMcsp);
                writer.WriteNumber("electricFieldModulusVm", kinetic.ElectricFieldModulusVm);
                writer.WritePropertyName("electricFieldDirection");
                JsonSerializer.Serialize(writer, kinetic.ElectricFieldDirection, options);
                WriteNullableNumber(writer, "normalizationEnergyEv", kinetic.NormalizationEnergyEv);
                WriteNullableNumber(writer, "maxAttemptFrequencyHz", kinetic.MaxAttemptFrequencyHz);
                break;
            case MetropolisSimulation metropolis:
                writer.WriteNumber("breakTolerance", metropolis.BreakTolerance);
                writer.WriteNumber("breakSampleLengthMcsp", metropolis.BreakSampleLengthMcsp);
                writer.WriteNumber("breakSampleIntervalMcsp", metropolis.BreakSampleIntervalMcsp);
                writer.WriteNumber("resultSampleMcsp", metropolis.ResultSampleMcsp);
                break;
            default:
                throw new JsonException("Unknown simulation type.");
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static KineticSimulation CreateKinetic(
        SimulationId id,
        string name,
        ImmutableArray<TransitionRef> transitionIds,
        bool hasKindMismatch,
        SettingsBuilder settings)
    {
        var result = new KineticSimulation
        {
            Id = id,
            Name = name,
            TransitionIds = transitionIds,
            HasPersistedKindMismatch = hasKindMismatch,
        };

        result = (KineticSimulation)ApplyCommon(result, settings);
        if (settings.PreRunMcsp is { } preRunMcsp)
        {
            result = result with { PreRunMcsp = preRunMcsp };
        }

        if (settings.ElectricFieldModulusVm is { } electricFieldModulusVm)
        {
            result = result with { ElectricFieldModulusVm = electricFieldModulusVm };
        }

        if (settings.ElectricFieldDirection is { } electricFieldDirection)
        {
            result = result with { ElectricFieldDirection = electricFieldDirection };
        }

        if (settings.NormalizationEnergyEv is { } normalizationEnergyEv)
        {
            result = result with { NormalizationEnergyEv = normalizationEnergyEv };
        }

        if (settings.MaxAttemptFrequencyHz is { } maxAttemptFrequencyHz)
        {
            result = result with { MaxAttemptFrequencyHz = maxAttemptFrequencyHz };
        }

        return result;
    }

    private static MetropolisSimulation CreateMetropolis(
        SimulationId id,
        string name,
        ImmutableArray<TransitionRef> transitionIds,
        bool hasKindMismatch,
        SettingsBuilder settings)
    {
        var result = new MetropolisSimulation
        {
            Id = id,
            Name = name,
            TransitionIds = transitionIds,
            HasPersistedKindMismatch = hasKindMismatch,
        };

        result = (MetropolisSimulation)ApplyCommon(result, settings);
        if (settings.BreakTolerance is { } breakTolerance)
        {
            result = result with { BreakTolerance = breakTolerance };
        }

        if (settings.BreakSampleLengthMcsp is { } breakSampleLengthMcsp)
        {
            result = result with { BreakSampleLengthMcsp = breakSampleLengthMcsp };
        }

        if (settings.BreakSampleIntervalMcsp is { } breakSampleIntervalMcsp)
        {
            result = result with { BreakSampleIntervalMcsp = breakSampleIntervalMcsp };
        }

        if (settings.ResultSampleMcsp is { } resultSampleMcsp)
        {
            result = result with { ResultSampleMcsp = resultSampleMcsp };
        }

        return result;
    }

    private static Simulation ApplyCommon(Simulation simulation, SettingsBuilder settings)
    {
        var result = simulation;
        if (settings.TemperatureK is { } temperatureK)
        {
            result = result with { TemperatureK = temperatureK };
        }

        if (settings.TargetMcsp is { } targetMcsp)
        {
            result = result with { TargetMcsp = targetMcsp };
        }

        if (settings.WriteOutCount is { } writeOutCount)
        {
            result = result with { WriteOutCount = writeOutCount };
        }

        if (settings.HasTimeLimitSeconds)
        {
            result = result with { TimeLimitSeconds = settings.TimeLimitSeconds };
        }

        if (settings.MinSuccessRate is { } minSuccessRate)
        {
            result = result with { MinSuccessRate = minSuccessRate };
        }

        if (settings.JobCount is { } jobCount)
        {
            result = result with { JobCount = jobCount };
        }

        if (settings.HasRngSeed)
        {
            result = result with { RngSeed = settings.RngSeed };
        }

        if (settings.Flags is { } flags)
        {
            result = result with { Flags = flags };
        }

        if (settings.HasInstruction)
        {
            result = result with { Instruction = settings.Instruction };
        }

        return result;
    }

    private static SimulationId ReadId(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String ||
            !Guid.TryParseExact(element.GetString(), "D", out var value))
        {
            throw new JsonException("Simulation 'id' must be a UUID string.");
        }

        return SimulationId.Create(value);
    }

    private static ImmutableArray<TransitionRef> ReadTransitionIds(
        JsonElement element,
        TransitionKind kind,
        string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Simulation '{propertyName}' must be an array.");
        }

        var builder = ImmutableArray.CreateBuilder<TransitionRef>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String ||
                !Guid.TryParseExact(item.GetString(), "D", out var value))
            {
                throw new JsonException($"Simulation '{propertyName}' must contain UUID strings.");
            }

            builder.Add(kind == TransitionKind.Kinetic
                ? TransitionRef.From(KineticTransitionId.Create(value))
                : TransitionRef.From(MetropolisTransitionId.Create(value)));
        }

        return builder.ToImmutable();
    }

    private static void ReadSettings(
        JsonElement element,
        JsonSerializerOptions options,
        SettingsBuilder settings)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Simulation 'settings' must be a JSON object.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Simulation setting '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "temperatureK":
                    settings.TemperatureK = ReadDouble(property.Value, property.Name);
                    break;
                case "targetMcsp":
                    settings.TargetMcsp = ReadInt64(property.Value, property.Name);
                    break;
                case "writeOutCount":
                    settings.WriteOutCount = ReadInt32(property.Value, property.Name);
                    break;
                case "timeLimitSeconds":
                    settings.HasTimeLimitSeconds = true;
                    settings.TimeLimitSeconds = ReadNullableInt64(property.Value, property.Name);
                    break;
                case "minSuccessRate":
                    settings.MinSuccessRate = ReadNullableDouble(property.Value, property.Name);
                    break;
                case "jobCount":
                    settings.JobCount = ReadInt32(property.Value, property.Name);
                    break;
                case "rngSeed":
                    settings.HasRngSeed = true;
                    settings.RngSeed = ReadNullableString(property.Value, property.Name);
                    break;
                case "executionFlags":
                    settings.Flags = property.Value.Deserialize<ExecutionFlags>(options);
                    break;
                case "instruction":
                    settings.HasInstruction = true;
                    settings.Instruction = ReadNullableString(property.Value, property.Name);
                    break;
                case "preRunMcsp":
                    settings.HasKmcOnly = true;
                    settings.PreRunMcsp = ReadInt64(property.Value, property.Name);
                    break;
                case "electricFieldModulusVm":
                    settings.HasKmcOnly = true;
                    settings.ElectricFieldModulusVm = ReadDouble(property.Value, property.Name);
                    break;
                case "electricFieldDirection":
                    settings.HasKmcOnly = true;
                    settings.ElectricFieldDirection = property.Value.Deserialize<Fractional3>(options);
                    break;
                case "normalizationEnergyEv":
                    settings.HasKmcOnly = true;
                    settings.NormalizationEnergyEv = ReadNullableDouble(property.Value, property.Name);
                    break;
                case "maxAttemptFrequencyHz":
                    settings.HasKmcOnly = true;
                    settings.MaxAttemptFrequencyHz = ReadNullableDouble(property.Value, property.Name);
                    break;
                case "breakTolerance":
                    settings.HasMmcOnly = true;
                    settings.BreakTolerance = ReadDouble(property.Value, property.Name);
                    break;
                case "breakSampleLengthMcsp":
                    settings.HasMmcOnly = true;
                    settings.BreakSampleLengthMcsp = ReadInt64(property.Value, property.Name);
                    break;
                case "breakSampleIntervalMcsp":
                    settings.HasMmcOnly = true;
                    settings.BreakSampleIntervalMcsp = ReadInt64(property.Value, property.Name);
                    break;
                case "resultSampleMcsp":
                    settings.HasMmcOnly = true;
                    settings.ResultSampleMcsp = ReadInt64(property.Value, property.Name);
                    break;
                default:
                    throw new JsonException($"Unknown simulation setting '{property.Name}'.");
            }
        }
    }

    private static double ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out var value))
        {
            throw new JsonException($"Simulation setting '{propertyName}' must be a number.");
        }

        return value;
    }

    private static long ReadInt64(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt64(out var value))
        {
            throw new JsonException($"Simulation setting '{propertyName}' must be an integer.");
        }

        return value;
    }

    private static int ReadInt32(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new JsonException($"Simulation setting '{propertyName}' must be a 32-bit integer.");
        }

        return value;
    }

    private static double? ReadNullableDouble(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : ReadDouble(element, propertyName);

    private static long? ReadNullableInt64(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : ReadInt64(element, propertyName);

    private static string? ReadNullableString(JsonElement element, string propertyName) =>
        element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            _ => throw new JsonException($"Simulation setting '{propertyName}' must be a string or null."),
        };

    private static void WriteNullableNumber(Utf8JsonWriter writer, string propertyName, double? value)
    {
        if (value is { } number)
        {
            writer.WriteNumber(propertyName, number);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string propertyName, long? value)
    {
        if (value is { } number)
        {
            writer.WriteNumber(propertyName, number);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private sealed class SettingsBuilder
    {
        public double? TemperatureK { get; set; }

        public long? TargetMcsp { get; set; }

        public int? WriteOutCount { get; set; }

        public bool HasTimeLimitSeconds { get; set; }

        public long? TimeLimitSeconds { get; set; }

        public double? MinSuccessRate { get; set; }

        public int? JobCount { get; set; }

        public bool HasRngSeed { get; set; }

        public string? RngSeed { get; set; }

        public ExecutionFlags? Flags { get; set; }

        public bool HasInstruction { get; set; }

        public string? Instruction { get; set; }

        public bool HasKmcOnly { get; set; }

        public long? PreRunMcsp { get; set; }

        public double? ElectricFieldModulusVm { get; set; }

        public Fractional3? ElectricFieldDirection { get; set; }

        public double? NormalizationEnergyEv { get; set; }

        public double? MaxAttemptFrequencyHz { get; set; }

        public bool HasMmcOnly { get; set; }

        public double? BreakTolerance { get; set; }

        public long? BreakSampleLengthMcsp { get; set; }

        public long? BreakSampleIntervalMcsp { get; set; }

        public long? ResultSampleMcsp { get; set; }
    }
}
