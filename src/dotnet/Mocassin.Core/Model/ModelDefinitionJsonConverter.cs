// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Model;

/// <summary>
/// Serializes the flat model envelope and applies defaults for missing sections.
/// </summary>
/// <remarks>
/// Missing <c>particles</c>, <c>particleSets</c>, <c>structure</c>, transition, energy, lattice, or simulation
/// properties load as the empty/default model. Unknown or duplicate properties are rejected to preserve the
/// project-format contract. Empty optional transition, energy, lattice, and simulation sections are omitted when
/// writing so that projects without authored data keep their earlier canonical model and document hashes.
/// </remarks>
public sealed class ModelDefinitionJsonConverter : JsonConverter<ModelDefinition>
{
    /// <inheritdoc />
    public override ModelDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Model must be a JSON object.");
        }

        var particles = ImmutableArray<Particle>.Empty;
        var particleSets = ImmutableArray<ParticleSet>.Empty;
        var structure = StructureModel.Default;
        var stateChanges = ImmutableArray<StateChange>.Empty;
        var stateChangeGroups = ImmutableArray<StateChangeGroup>.Empty;
        var chains = ImmutableArray<TransitionChain>.Empty;
        var kineticTransitions = ImmutableArray<KineticTransition>.Empty;
        var metropolisTransitions = ImmutableArray<MetropolisTransition>.Empty;
        var stableEnvironment = StableEnvironment.Default;
        var unstableEnvironments = ImmutableArray<UnstableEnvironment>.Empty;
        var clusters = ImmutableArray<Cluster>.Empty;
        var siteEnergies = ImmutableArray<SiteEnergySet>.Empty;
        var occupationExchanges = ImmutableArray<OccupationExchange>.Empty;
        var dopings = ImmutableArray<Doping>.Empty;
        var simulations = ImmutableArray<Simulation>.Empty;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                throw new JsonException($"Model property '{property.Name}' is defined more than once.");
            }

            switch (property.Name)
            {
                case "particles":
                    particles = ReadArray<Particle>(property.Value, options, "particles");
                    break;
                case "particleSets":
                    particleSets = ReadArray<ParticleSet>(property.Value, options, "particleSets");
                    break;
                case "structure":
                    structure = property.Value.Deserialize<StructureModel>(options)
                        ?? throw new JsonException("Model structure must not be null.");
                    break;
                case "stateChanges":
                    stateChanges = ReadArray<StateChange>(property.Value, options, "stateChanges");
                    break;
                case "stateChangeGroups":
                    stateChangeGroups = ReadArray<StateChangeGroup>(property.Value, options, "stateChangeGroups");
                    break;
                case "chains":
                    chains = ReadArray<TransitionChain>(property.Value, options, "chains");
                    break;
                case "kineticTransitions":
                    kineticTransitions = ReadArray<KineticTransition>(property.Value, options, "kineticTransitions");
                    break;
                case "metropolisTransitions":
                    metropolisTransitions = ReadArray<MetropolisTransition>(
                        property.Value,
                        options,
                        "metropolisTransitions");
                    break;
                case "stableEnvironment":
                    stableEnvironment = property.Value.Deserialize<StableEnvironment>(options)
                        ?? throw new JsonException("Model stableEnvironment must not be null.");
                    break;
                case "unstableEnvironments":
                    unstableEnvironments = ReadArray<UnstableEnvironment>(
                        property.Value,
                        options,
                        "unstableEnvironments");
                    break;
                case "clusters":
                    clusters = ReadArray<Cluster>(property.Value, options, "clusters");
                    break;
                case "siteEnergies":
                    siteEnergies = ReadArray<SiteEnergySet>(property.Value, options, "siteEnergies");
                    break;
                case "occupationExchanges":
                    occupationExchanges = ReadArray<OccupationExchange>(
                        property.Value,
                        options,
                        "occupationExchanges");
                    break;
                case "dopings":
                    dopings = ReadArray<Doping>(property.Value, options, "dopings");
                    break;
                case "simulations":
                    simulations = ReadArray<Simulation>(property.Value, options, "simulations");
                    break;
                default:
                    throw new JsonException($"Unknown model property '{property.Name}'.");
            }
        }

        return new ModelDefinition
        {
            Particles = particles,
            ParticleSets = particleSets,
            Structure = structure,
            StateChanges = stateChanges,
            StateChangeGroups = stateChangeGroups,
            Chains = chains,
            KineticTransitions = kineticTransitions,
            MetropolisTransitions = metropolisTransitions,
            StableEnvironment = NormalizeEnvironment(stableEnvironment),
            UnstableEnvironments = [.. unstableEnvironments.Select(NormalizeEnvironment)],
            Clusters = [.. clusters.Select(NormalizeCluster)],
            SiteEnergies = [.. siteEnergies.Select(NormalizeSiteEnergySet)],
            OccupationExchanges = occupationExchanges,
            Dopings = dopings,
            Simulations = [.. simulations.Select(SimulationCanonicalizer.Normalize)],
        };
    }

    private static StableEnvironment NormalizeEnvironment(StableEnvironment environment) =>
        environment with { Filters = environment.Filters.IsDefault ? [] : environment.Filters };

    private static UnstableEnvironment NormalizeEnvironment(UnstableEnvironment environment) =>
        environment with { Filters = environment.Filters.IsDefault ? [] : environment.Filters };

    private static Cluster NormalizeCluster(Cluster cluster) =>
        cluster with { OffsetVectors = cluster.OffsetVectors.IsDefault ? [] : cluster.OffsetVectors };

    private static SiteEnergySet NormalizeSiteEnergySet(SiteEnergySet siteEnergySet) =>
        siteEnergySet with { Entries = siteEnergySet.Entries.IsDefault ? [] : siteEnergySet.Entries };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ModelDefinition value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WritePropertyName("particles");
        JsonSerializer.Serialize(writer, value.Particles, options);
        writer.WritePropertyName("particleSets");
        JsonSerializer.Serialize(writer, value.ParticleSets, options);
        writer.WritePropertyName("structure");
        JsonSerializer.Serialize(writer, value.Structure, options);
        WriteArrayIfNotEmpty(writer, "stateChanges", value.StateChanges, options);
        WriteArrayIfNotEmpty(writer, "stateChangeGroups", value.StateChangeGroups, options);
        WriteArrayIfNotEmpty(writer, "chains", value.Chains, options);
        WriteArrayIfNotEmpty(writer, "kineticTransitions", value.KineticTransitions, options);
        WriteArrayIfNotEmpty(writer, "metropolisTransitions", value.MetropolisTransitions, options);
        WriteStableEnvironment(writer, value.StableEnvironment, options);
        WriteArrayIfNotEmpty(writer, "unstableEnvironments", value.UnstableEnvironments, options);
        WriteArrayIfNotEmpty(writer, "clusters", value.Clusters, options);
        WriteArrayIfNotEmpty(writer, "siteEnergies", value.SiteEnergies, options);
        WriteArrayIfNotEmpty(writer, "occupationExchanges", value.OccupationExchanges, options);
        WriteArrayIfNotEmpty(writer, "dopings", value.Dopings, options);
        WriteArrayIfNotEmpty(writer, "simulations", value.Simulations, options);
        writer.WriteEndObject();
    }

    private static void WriteStableEnvironment(
        Utf8JsonWriter writer,
        StableEnvironment environment,
        JsonSerializerOptions options)
    {
        if (IsDefaultEnvironment(environment))
        {
            return;
        }

        writer.WritePropertyName("stableEnvironment");
        JsonSerializer.Serialize(writer, environment, options);
    }

    internal static bool IsDefaultEnvironment(StableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.Cutoff.Value == StableEnvironment.Default.Cutoff.Value
            && environment.Filters.IsDefaultOrEmpty;
    }

    private static void WriteArrayIfNotEmpty<T>(
        Utf8JsonWriter writer,
        string propertyName,
        ImmutableArray<T> values,
        JsonSerializerOptions options)
    {
        if (values.IsDefaultOrEmpty)
        {
            return;
        }

        writer.WritePropertyName(propertyName);
        JsonSerializer.Serialize(writer, values, options);
    }

    private static ImmutableArray<T> ReadArray<T>(
        JsonElement element,
        JsonSerializerOptions options,
        string propertyName)
        where T : class
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Model {propertyName} must be an array.");
        }

        var values = element.Deserialize<ImmutableArray<T>>(options);
        if (values.IsDefault)
        {
            return [];
        }

        if (values.Any(static value => value is null))
        {
            throw new JsonException($"Model {propertyName} must not contain null elements.");
        }

        return values;
    }
}
