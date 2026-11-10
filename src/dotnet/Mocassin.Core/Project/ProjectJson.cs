// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Project;

/// <summary>
/// Serializes and deserializes the versioned project JSON document.
/// </summary>
public static class ProjectJson
{
    /// <summary>
    /// Options for human-readable project files.
    /// </summary>
    public static JsonSerializerOptions FileOptions { get; } = CreateOptions(writeIndented: true);

    /// <summary>
    /// Options for canonical, whitespace-free serialization and hashing.
    /// </summary>
    public static JsonSerializerOptions CanonicalOptions { get; } = CreateOptions(writeIndented: false);

    internal static JsonSerializerOptions DtoOptions { get; } = CreateDtoOptions();

    /// <summary>
    /// Serializes a project to JSON text.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <param name="indented">Whether to write the human-readable indented form.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize(Project project, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonSerializer.Serialize(project, indented ? FileOptions : CanonicalOptions);
    }

    /// <summary>
    /// Serializes a project to UTF-8 JSON bytes.
    /// </summary>
    /// <param name="project">The project to serialize.</param>
    /// <param name="indented">Whether to write the human-readable indented form.</param>
    /// <returns>The UTF-8 JSON bytes without a byte-order mark.</returns>
    public static byte[] SerializeToUtf8Bytes(Project project, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonSerializer.SerializeToUtf8Bytes(project, indented ? FileOptions : CanonicalOptions);
    }

    /// <summary>
    /// Deserializes a project from JSON text.
    /// </summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed project.</returns>
    /// <exception cref="JsonException">The document is malformed or has an unsupported schema version.</exception>
    public static Project Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<Project>(json, FileOptions)
            ?? throw new JsonException("Project JSON did not contain a project object.");
    }

    /// <summary>
    /// Deserializes a project from UTF-8 JSON bytes.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 JSON bytes.</param>
    /// <returns>The parsed project.</returns>
    /// <exception cref="JsonException">The document is malformed or has an unsupported schema version.</exception>
    public static Project Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<Project>(utf8Json, FileOptions)
            ?? throw new JsonException("Project JSON did not contain a project object.");
    }

    internal static byte[] SerializeCanonicalModel(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return JsonSerializer.SerializeToUtf8Bytes(CanonicalizeModel(model), DtoOptions);
    }

    internal static byte[] SerializeCanonicalStructure(StructureModel structure)
    {
        ArgumentNullException.ThrowIfNull(structure);
        return JsonSerializer.SerializeToUtf8Bytes(CanonicalizeStructure(structure), DtoOptions);
    }

    internal static ModelDefinition CanonicalizeModel(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var particles = model.Particles.IsDefault ? [] : model.Particles;
        var particleSets = model.ParticleSets.IsDefault ? [] : model.ParticleSets;
        var stateChanges = model.StateChanges.IsDefault ? [] : model.StateChanges;
        var stateChangeGroups = model.StateChangeGroups.IsDefault ? [] : model.StateChangeGroups;
        var chains = model.Chains.IsDefault ? [] : model.Chains;
        var kineticTransitions = model.KineticTransitions.IsDefault ? [] : model.KineticTransitions;
        var metropolisTransitions = model.MetropolisTransitions.IsDefault ? [] : model.MetropolisTransitions;
        var unstableEnvironments = model.UnstableEnvironments.IsDefault ? [] : model.UnstableEnvironments;
        var clusters = model.Clusters.IsDefault ? [] : model.Clusters;
        var siteEnergies = model.SiteEnergies.IsDefault ? [] : model.SiteEnergies;

        return new ModelDefinition
        {
            Particles = [.. particles.OrderBy(static particle => particle.Id.Value)],
            ParticleSets = [.. particleSets.OrderBy(static particleSet => particleSet.Id.Value)],
            Structure = CanonicalizeStructure(model.Structure),
            StateChanges = [.. stateChanges.OrderBy(static stateChange => stateChange.Id.Value)],
            StateChangeGroups = [.. stateChangeGroups
                .Select(NormalizeStateChangeGroup)
                .OrderBy(static group => group.Id.Value)],
            Chains = [.. chains
                .Select(NormalizeChain)
                .OrderBy(static chain => chain.Id.Value)],
            KineticTransitions = [.. kineticTransitions
                .Select(NormalizeKineticTransition)
                .OrderBy(static transition => transition.Id.Value)],
            MetropolisTransitions = [.. metropolisTransitions.OrderBy(static transition => transition.Id.Value)],
            StableEnvironment = CanonicalizeEnvironment(model.StableEnvironment),
            UnstableEnvironments = [.. unstableEnvironments
                .Select(CanonicalizeEnvironment)
                .OrderBy(static environment => environment.SiteId.Value)],
            Clusters = [.. clusters
                .Select(NormalizeCluster)
                .OrderBy(static cluster => cluster.Id.Value)],
            SiteEnergies = [.. siteEnergies
                .Select(NormalizeSiteEnergySet)
                .OrderBy(static siteEnergySet => siteEnergySet.SiteId.Value)],
        };
    }

    private static StableEnvironment CanonicalizeEnvironment(StableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var normalized = NormalizeEnvironment(environment);
        return ModelDefinitionJsonConverter.IsDefaultEnvironment(normalized)
            ? StableEnvironment.Default
            : normalized;
    }

    private static UnstableEnvironment CanonicalizeEnvironment(UnstableEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return NormalizeEnvironment(environment);
    }

    private static StableEnvironment NormalizeEnvironment(StableEnvironment environment) =>
        environment with
        {
            Filters = [.. (environment.Filters.IsDefault ? [] : environment.Filters)
                .OrderBy(static filter => filter.Start.Value)
                .ThenBy(static filter => filter.End.Value)
                .ThenBy(static filter => filter.SiteAId.Value)
                .ThenBy(static filter => filter.SiteBId.Value)],
        };

    private static UnstableEnvironment NormalizeEnvironment(UnstableEnvironment environment) =>
        environment with
        {
            Filters = [.. (environment.Filters.IsDefault ? [] : environment.Filters)
                .OrderBy(static filter => filter.Start.Value)
                .ThenBy(static filter => filter.End.Value)
                .ThenBy(static filter => filter.SiteAId.Value)
                .ThenBy(static filter => filter.SiteBId.Value)],
        };

    private static Cluster NormalizeCluster(Cluster cluster) =>
        cluster with
        {
            OffsetVectors = cluster.OffsetVectors.IsDefault ? [] : cluster.OffsetVectors,
        };

    private static SiteEnergySet NormalizeSiteEnergySet(SiteEnergySet siteEnergySet) =>
        siteEnergySet with
        {
            Entries = [.. (siteEnergySet.Entries.IsDefault ? [] : siteEnergySet.Entries)
                .OrderBy(static entry => entry.ParticleId.Value)],
        };

    private static StateChangeGroup NormalizeStateChangeGroup(StateChangeGroup group) =>
        group with
        {
            StateChangeIds = group.StateChangeIds.IsDefault ? [] : group.StateChangeIds,
        };

    private static TransitionChain NormalizeChain(TransitionChain chain) =>
        chain with
        {
            GroupIds = chain.GroupIds.IsDefault ? [] : chain.GroupIds,
            Linkers = chain.Linkers.IsDefault ? [] : chain.Linkers,
        };

    private static KineticTransition NormalizeKineticTransition(KineticTransition transition) =>
        transition with
        {
            ReferencePath = transition.ReferencePath.IsDefault ? [] : transition.ReferencePath,
        };

    internal static StructureModel CanonicalizeStructure(StructureModel structure)
    {
        ArgumentNullException.ThrowIfNull(structure);
        var sites = structure.Sites.IsDefault ? [] : structure.Sites;
        var dummyPositions = structure.DummyPositions.IsDefault ? [] : structure.DummyPositions;

        return new StructureModel(
            structure.Name,
            structure.SpaceGroup,
            structure.Cell,
            [.. sites.OrderBy(static site => site.Id.Value)],
            [.. dummyPositions.OrderBy(static dummyPosition => dummyPosition.Id.Value)]);
    }

    private static JsonSerializerOptions CreateOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ProjectJsonConverter());
        return options;
    }

    private static JsonSerializerOptions CreateDtoOptions()
    {
        var options = new JsonSerializerOptions(ProjectJsonContext.Default.Options)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        return options;
    }
}

/// <summary>
/// Maps the public project value to the flat on-disk envelope.
/// </summary>
public sealed class ProjectJsonConverter : JsonConverter<Project>
{
    /// <inheritdoc />
    public override Project Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            return ReadCore(ref reader);
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or NullReferenceException or FormatException or InvalidOperationException)
        {
            throw new JsonException("Project JSON does not match the project schema.", exception);
        }
    }

    private static Project ReadCore(ref Utf8JsonReader reader)
    {
        var dto = JsonSerializer.Deserialize<ProjectDto>(ref reader, ProjectJson.DtoOptions)
            ?? throw new JsonException("Project JSON did not contain an object.");

        if (dto.SchemaVersion > ProjectSchema.CurrentVersion)
        {
            throw new JsonException(
                $"Project schema version {dto.SchemaVersion} is newer than the supported version {ProjectSchema.CurrentVersion}. " +
                "Open the project with a newer MOCASSIN release.");
        }

        if (dto.SchemaVersion < ProjectSchema.CurrentVersion)
        {
            throw new JsonException(
                $"Project schema version {dto.SchemaVersion} must be migrated to version {ProjectSchema.CurrentVersion} before reading.");
        }

        if (dto.MasterSeed is null || dto.Name is null || dto.Model is null || dto.ViewSettings is null)
        {
            throw new JsonException("Project JSON is missing required envelope fields.");
        }

        return new Project(
            dto.Schema,
            dto.SchemaVersion,
            new ProjectMetadata(
                dto.ProjectId,
                dto.MasterSeed,
                dto.Name,
                dto.Description,
                dto.CreatedUtc,
                dto.ModifiedUtc,
                dto.Author,
                dto.Notes),
            dto.Model,
            dto.Parameterizations,
            dto.JobTemplates,
            dto.BuildTemplates,
            dto.ViewSettings,
            dto.Symmetry);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Project value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var parameterizations = value.Parameterizations
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var jobTemplates = value.JobTemplates
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var buildTemplates = value.BuildTemplates
            .OrderBy(static item => item.Id.Value)
            .ToImmutableArray();
        var objectStyles = value.ViewSettings.ObjectStyles
            .OrderBy(static item => item.ObjectId)
            .Select(static item => new ObjectStyle(
                item.ObjectId,
                item.Visible,
                item.Color,
                item.Material,
                item.Quality,
                item.Scale,
                item.Wireframe,
                item.AdditionalProperties
                    .OrderBy(static property => property.Key, StringComparer.Ordinal)
                    .ToImmutableArray()))
            .ToImmutableArray();
        var viewSettings = new ViewSettings(
            value.ViewSettings.Camera,
            value.ViewSettings.RenderBox,
            value.ViewSettings.Background,
            objectStyles);

        var dto = new ProjectDto
        {
            Schema = value.Schema,
            SchemaVersion = value.SchemaVersion,
            ProjectId = value.Metadata.ProjectId,
            MasterSeed = value.Metadata.MasterSeed,
            Name = value.Metadata.Name,
            Description = value.Metadata.Description,
            Author = value.Metadata.Author,
            Notes = value.Metadata.Notes,
            CreatedUtc = value.Metadata.CreatedUtc,
            ModifiedUtc = value.Metadata.ModifiedUtc,
            Symmetry = value.Symmetry,
            Model = ProjectJson.CanonicalizeModel(value.Model),
            Parameterizations = parameterizations,
            JobTemplates = jobTemplates,
            BuildTemplates = buildTemplates,
            ViewSettings = viewSettings,
        };

        JsonSerializer.Serialize(writer, dto, ProjectJson.DtoOptions);
    }
}

/// <summary>
/// Internal flat representation of the on-disk project envelope.
/// </summary>
internal sealed class ProjectDto
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    [JsonRequired]
    public int SchemaVersion { get; init; }

    [JsonRequired]
    public Guid ProjectId { get; init; }

    [JsonRequired]
    public string? MasterSeed { get; init; }

    [JsonRequired]
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Author { get; init; }

    public string? Notes { get; init; }

    [JsonRequired]
    [JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    public DateTimeOffset CreatedUtc { get; init; }

    [JsonRequired]
    [JsonConverter(typeof(UtcDateTimeOffsetJsonConverter))]
    public DateTimeOffset ModifiedUtc { get; init; }

    public ProjectSymmetryMetadata? Symmetry { get; init; }

    [JsonRequired]
    public ModelDefinition? Model { get; init; }

    [JsonRequired]
    public ImmutableArray<ParameterizationTemplate> Parameterizations { get; init; }

    [JsonRequired]
    public ImmutableArray<JobTemplate> JobTemplates { get; init; }

    [JsonRequired]
    public ImmutableArray<BuildTemplate> BuildTemplates { get; init; }

    [JsonRequired]
    public ViewSettings? ViewSettings { get; init; }
}

/// <summary>
/// Source-generated JSON metadata for project DTOs and nested values.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.Strict,
    WriteIndented = false)]
[JsonSerializable(typeof(ProjectDto))]
[JsonSerializable(typeof(ModelDefinition))]
[JsonSerializable(typeof(ProjectSymmetryMetadata))]
[JsonSerializable(typeof(Particle))]
[JsonSerializable(typeof(ParticleSet))]
[JsonSerializable(typeof(ImmutableArray<Particle>))]
[JsonSerializable(typeof(ImmutableArray<ParticleSet>))]
[JsonSerializable(typeof(StructureModel))]
[JsonSerializable(typeof(SpaceGroupRef))]
[JsonSerializable(typeof(CellParameters))]
[JsonSerializable(typeof(CellSite))]
[JsonSerializable(typeof(CellDummyPosition))]
[JsonSerializable(typeof(ImmutableArray<CellSite>))]
[JsonSerializable(typeof(ImmutableArray<CellDummyPosition>))]
[JsonSerializable(typeof(StateChange))]
[JsonSerializable(typeof(StateChangeGroup))]
[JsonSerializable(typeof(TransitionChain))]
[JsonSerializable(typeof(KineticTransition))]
[JsonSerializable(typeof(MetropolisTransition))]
[JsonSerializable(typeof(TransitionPattern))]
[JsonSerializable(typeof(TransitionLinker))]
[JsonSerializable(typeof(TransitionKind))]
[JsonSerializable(typeof(TransitionRef))]
[JsonSerializable(typeof(ImmutableArray<StateChange>))]
[JsonSerializable(typeof(ImmutableArray<StateChangeGroup>))]
[JsonSerializable(typeof(ImmutableArray<TransitionChain>))]
[JsonSerializable(typeof(ImmutableArray<KineticTransition>))]
[JsonSerializable(typeof(ImmutableArray<MetropolisTransition>))]
[JsonSerializable(typeof(ImmutableArray<TransitionLinker>))]
[JsonSerializable(typeof(StableEnvironment))]
[JsonSerializable(typeof(UnstableEnvironment))]
[JsonSerializable(typeof(InteractionFilter))]
[JsonSerializable(typeof(Cluster))]
[JsonSerializable(typeof(SiteEnergySet))]
[JsonSerializable(typeof(SiteEnergyEntry))]
[JsonSerializable(typeof(ImmutableArray<UnstableEnvironment>))]
[JsonSerializable(typeof(ImmutableArray<InteractionFilter>))]
[JsonSerializable(typeof(ImmutableArray<Cluster>))]
[JsonSerializable(typeof(ImmutableArray<SiteEnergySet>))]
[JsonSerializable(typeof(ImmutableArray<SiteEnergyEntry>))]
[JsonSerializable(typeof(ImmutableArray<Fractional3>))]
[JsonSerializable(typeof(Fractional3))]
[JsonSerializable(typeof(ProjectMetadata))]
[JsonSerializable(typeof(ParameterizationTemplate))]
[JsonSerializable(typeof(JobTemplate))]
[JsonSerializable(typeof(BuildTemplate))]
[JsonSerializable(typeof(ViewSettings))]
[JsonSerializable(typeof(CameraSettings))]
[JsonSerializable(typeof(RenderBoxSettings))]
[JsonSerializable(typeof(ObjectStyle))]
[JsonSerializable(typeof(StyleProperty))]
[JsonSerializable(typeof(StyleValue))]
internal sealed partial class ProjectJsonContext : JsonSerializerContext;
