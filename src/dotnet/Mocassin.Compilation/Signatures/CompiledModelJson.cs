// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Compilation.Compiler;

namespace Mocassin.Compilation.Signatures;

/// <summary>
/// Writes the canonical <c>compiled-inspection-v1</c> JSON projection of a compiled model.
/// </summary>
/// <remarks>
/// The projection is pure and never writes to a file. It includes stable identifiers, index maps, table
/// shapes, and provenance that are not persisted in <c>.msl</c>; it is not a package contract.
/// </remarks>
public static class CompiledModelJson
{
    /// <summary>
    /// The inspection format identifier.
    /// </summary>
    public const string Format = "compiled-inspection-v1";

    /// <summary>
    /// The inspection format version.
    /// </summary>
    public const int FormatVersion = 1;

    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Writes the JSON inspection projection.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <returns>The deterministic JSON text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    public static string Write(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var projection = new
        {
            format = Format,
            formatVersion = FormatVersion,
            modelSignature = model.ModelSignature,
            symmetryContractVersion = model.SymmetryContractVersion,
            spaceGroupDataVersion = model.SpaceGroupDataVersion,
            spaceGroupDataSha256 = model.SpaceGroupDataSha256,
            counts = new
            {
                particles = model.Particles.Count,
                positions = model.Structure.PositionCount,
                sites = model.Structure.SiteCount,
                environments = model.Structure.EnvironmentCount,
                pairGeometries = model.Energy.PairGeometryCount,
                pairTables = model.Energy.PairTableCount,
                clusterTables = model.Energy.ClusterTableCount,
                clusterOccupations = model.Energy.ClusterOccupationCount,
                collections = model.Transitions.CollectionCount,
                directions = model.Transitions.DirectionCount,
                rules = model.Transitions.RuleCount,
                inverseRules = model.Transitions.InverseRuleCount,
                simulations = model.Simulations.Length,
            },
            particles = model.Particles.Particles.Select(static particle => new
            {
                index = particle.Index,
                id = particle.Id.Value,
                symbol = particle.Symbol,
                charge = particle.ChargeElementary,
                isVacancy = particle.IsVacancy,
            }),
            structure = new
            {
                cellVectors = model.Structure.CellVectors.Select(static vector => new[] { vector.X, vector.Y, vector.Z }),
                volume = model.Structure.CellVolumeAngstrom3,
                interactionRange = model.Structure.InteractionRange,
                sites = model.Structure.Sites.Select(static site => new
                {
                    index = site.Index,
                    id = site.Id.Value,
                    stability = site.Stability.ToString(),
                    environment = site.EnvironmentIndex,
                    positions = site.PositionIndices,
                }),
                environments = model.Structure.Environments.Select(static environment => new
                {
                    index = environment.Index,
                    siteIndex = environment.SiteIndex,
                    stability = environment.Stability.ToString(),
                    allowedMask = environment.AllowedParticleMask,
                    updateParticles = environment.UpdateParticleIndices,
                }),
                positions = model.Structure.Positions.Select(static position => new
                {
                    p = position.P,
                    fractional = new[] { position.Fractional.A, position.Fractional.B, position.Fractional.C },
                    siteIndex = position.SiteIndex,
                    stability = position.Stability.ToString(),
                    environment = position.EnvironmentIndex,
                    orbit = position.OrbitIndex,
                    allowedMask = position.AllowedParticleMask,
                    pairLinks = position.PairLinks.Select(static link => new
                    {
                        table = link.PairTableIndex,
                        targetP = link.TargetP,
                        offset = new[] { link.Offset.A, link.Offset.B, link.Offset.C, link.Offset.P },
                        bucketFm = link.DistanceBucketFm,
                    }),
                    clusterLinks = position.ClusterLinks.Select(static link => new
                    {
                        table = link.ClusterTableIndex,
                        slot = link.SiteSlot,
                    }),
                }),
            },
            energy = new
            {
                pairGeometries = model.Energy.PairGeometries.Select(static geometry => new
                {
                    geometryId = geometry.GeometryId,
                    energyId = geometry.EnergyId,
                    table = geometry.PairTableIndex,
                    centerP = geometry.CenterP,
                    partnerP = geometry.PartnerP,
                    bucketFm = geometry.DistanceBucketFm,
                    distance = geometry.DistanceAngstrom,
                    isChiral = geometry.IsChiral,
                    chiralPartner = geometry.ChiralPartnerGeometryId,
                }),
                pairTables = model.Energy.PairTables.Select(static table => new
                {
                    index = table.Index,
                    energyId = table.EnergyId,
                    particleCount = table.ParticleCount,
                }),
                clusterTables = model.Energy.ClusterTables.Select(static table => new
                {
                    index = table.Index,
                    energyId = table.ClusterEnergyId,
                    clusterId = table.ClusterId.Value,
                    centerP = table.CenterP,
                    centerParticles = table.ParticleTableMapping,
                    occupations = table.Occupations.Select(static occupation => new
                    {
                        code = occupation.CanonicalCode,
                        tuple = occupation.CanonicalTuple,
                        expansions = occupation.Expansions.Select(static expansion => new
                        {
                            code = expansion.OccupationCode,
                            permutation = expansion.PermutationIndex,
                        }),
                    }),
                }),
            },
            transitions = new
            {
                collections = model.Transitions.Collections.Select(static collection => new
                {
                    index = collection.Index,
                    kind = collection.Kind.ToString(),
                    transitionKind = collection.Transition.Kind.ToString(),
                    transitionId = collection.Transition.Id,
                    mobileMask = collection.MobileParticleMask,
                    directions = collection.DirectionIndices,
                    rules = collection.RuleIndices,
                }),
                directions = model.Transitions.Directions.Select(static direction => new
                {
                    index = direction.Index,
                    objectId = direction.ObjectId,
                    collection = direction.CollectionIndex,
                    position = direction.PositionId,
                    path = direction.Path.Select(static encoding => new[] { encoding.A, encoding.B, encoding.C, encoding.P }),
                    movement = direction.Movement.Select(static value => new[] { value.A, value.B, value.C }),
                    pathVector = new[] { direction.PathVector.X, direction.PathVector.Y, direction.PathVector.Z },
                    pathLength = direction.PathLength,
                    isInverse = direction.IsInverse,
                    equivalentStarts = direction.EquivalentStartPositions,
                }),
                rules = model.Transitions.Rules.Select(static rule => new
                {
                    index = rule.Index,
                    ruleGeometryId = rule.RuleGeometryId,
                    collection = rule.CollectionIndex,
                    directions = rule.DirectionIndices,
                    state0 = rule.State0,
                    state1 = rule.State1,
                    state2 = rule.State2,
                    movement = rule.Movement.Select(static value => new[] { value.A, value.B, value.C }),
                    chargeTransportVector = new[]
                    {
                        rule.ChargeTransportVector.X,
                        rule.ChargeTransportVector.Y,
                        rule.ChargeTransportVector.Z,
                    },
                    staticCorrection = rule.StaticCorrection is null
                        ? null
                        : new[]
                        {
                            rule.StaticCorrection.Value.X,
                            rule.StaticCorrection.Value.Y,
                            rule.StaticCorrection.Value.Z,
                        },
                    trackerOrder = rule.TrackerOrder,
                    isInverse = rule.IsInverse,
                    frequencyHz = rule.FrequencyHz,
                }),
                jumpCounts = model.Transitions.JumpCounts,
                jumpOffsets = model.Transitions.JumpOffsets,
                jumpMappings = model.Transitions.JumpMappings,
                globalTrackers = model.Transitions.GlobalTrackers.Select(static tracker => new
                {
                    index = tracker.Index,
                    collection = tracker.CollectionIndex,
                    particle = tracker.ParticleIndex,
                }),
                staticTrackers = model.Transitions.StaticTrackers.Select(static tracker => new
                {
                    index = tracker.Index,
                    position = tracker.PositionIndex,
                    particle = tracker.ParticleIndex,
                }),
            },
            simulations = model.Simulations.Select(static simulation => new
            {
                id = simulation.Id.Value,
                name = simulation.Name,
                kind = simulation.Kind.ToString(),
                activeCollections = simulation.ActiveCollectionIndices,
                intent = new
                {
                    kind = simulation.Intent.Kind.ToString(),
                    useDualDofCorrection = simulation.Intent.UseDualDofCorrection,
                    usePreRun = simulation.Intent.UsePreRun,
                    preRunMcsp = simulation.Intent.PreRunMcsp,
                },
                trackers = new
                {
                    global = simulation.Trackers.GlobalTrackerIndices,
                    @static = simulation.Trackers.StaticTrackerIndices,
                },
            }),
            indexMaps = new
            {
                directions = model.IndexMaps.DirectionObjectIds,
                rules = model.IndexMaps.RuleGeometryIds,
                pairs = model.IndexMaps.PairGeometryIds,
                clusters = model.IndexMaps.ClusterEnergyIds,
            },
        };
        return JsonSerializer.Serialize(projection, options);
    }
}
