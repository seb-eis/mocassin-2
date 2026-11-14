// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Energy;
using Mocassin.Compilation.Structure;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Model.Simulations;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// One compiled pair table together with the representative geometry and the admissible particle masks used by
/// template creation and summaries.
/// </summary>
/// <param name="TableIndex">The dense pair table index.</param>
/// <param name="EnergyId">The shared pair-energy identity.</param>
/// <param name="GeometryId">The representative directed geometry identity.</param>
/// <param name="CenterSiteId">The representative centre reference site.</param>
/// <param name="PartnerSiteId">The representative partner reference site.</param>
/// <param name="DistanceBucketFm">The representative distance bucket in femtometres.</param>
/// <param name="Stability">The centre reference-site stability.</param>
/// <param name="IsChiral">Whether the reciprocal direction is a distinct chiral partner.</param>
/// <param name="CenterMask">The compiled particles that may occupy the centre reference site.</param>
/// <param name="PartnerMask">The compiled particles that may occupy the partner reference site.</param>
/// <param name="RequiredSlotCount">The number of independent values required by the shared table.</param>
internal sealed record PairTableDescriptor(
    int TableIndex,
    string EnergyId,
    string GeometryId,
    CellSiteId CenterSiteId,
    CellSiteId PartnerSiteId,
    int DistanceBucketFm,
    SiteStability Stability,
    bool IsChiral,
    ulong CenterMask,
    ulong PartnerMask,
    int RequiredSlotCount);

/// <summary>
/// Validates the compiled-model invariants the parameterization factory depends on and derives pair-table
/// descriptors from the compiled geometry.
/// </summary>
/// <remarks>
/// A <see cref="CompiledModel"/> is compiler-owned, so a violation is a programmer/contract error and throws
/// <see cref="InvalidOperationException"/>. It is never reported as user-data diagnostics.
/// </remarks>
internal static class CompiledModelContract
{
    /// <summary>
    /// Validates the particle, structure, energy, and transition tables.
    /// </summary>
    /// <param name="model">The compiled model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compiled model violates a contract invariant.</exception>
    public static void Validate(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Particles.Count < 1)
        {
            throw Invalid("The compiled particle table must contain the implicit void particle.");
        }

        ValidateStructure(model);
        ValidateEnergy(model);
        ValidateTransitions(model);
    }

    /// <summary>
    /// Derives one descriptor per compiled pair table.
    /// </summary>
    /// <param name="model">A validated compiled model.</param>
    /// <returns>The descriptors in ascending table-index order.</returns>
    /// <exception cref="InvalidOperationException">A pair table has no representative geometry.</exception>
    public static ImmutableArray<PairTableDescriptor> DescribePairTables(CompiledModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var sites = model.Structure.Sites;
        var geometriesByTable = GroupGeometries(model);
        var descriptors = ImmutableArray.CreateBuilder<PairTableDescriptor>(model.Energy.PairTables.Length);
        foreach (var table in model.Energy.PairTables)
        {
            var geometries = geometriesByTable[table.Index];
            var representative = SelectRepresentative(table, geometries);
            var stability = sites[representative.CenterSiteIndex].Stability;
            var centerMask = SiteMask(model.Structure, representative.CenterSiteIndex);
            var partnerMask = SiteMask(model.Structure, representative.PartnerSiteIndex);
            var required = representative.IsChiral
                ? CountUnorderedSlots(centerMask, partnerMask)
                : checked(PopCount(centerMask) * PopCount(partnerMask));
            descriptors.Add(new PairTableDescriptor(
                table.Index,
                table.EnergyId,
                representative.GeometryId,
                representative.CenterSiteId,
                representative.PartnerSiteId,
                representative.DistanceBucketFm,
                stability,
                representative.IsChiral,
                centerMask,
                partnerMask,
                required));
        }

        return descriptors.MoveToImmutable();
    }

    /// <summary>
    /// Gets the union of the allowed particles of every compiled position owned by one reference site.
    /// </summary>
    /// <param name="structure">The compiled structure.</param>
    /// <param name="siteIndex">The dense reference-site index.</param>
    /// <returns>The allowed-particle mask without the implicit void particle.</returns>
    public static ulong SiteMask(CompiledStructureModel structure, int siteIndex)
    {
        var positions = structure.Positions;
        if (positions.IsDefault)
        {
            return 0;
        }

        ulong mask = 0;
        var positionIndices = structure.Sites[siteIndex].PositionIndices;
        if (positionIndices.IsDefault)
        {
            return 0;
        }

        foreach (var positionIndex in positionIndices)
        {
            mask |= positions[positionIndex].AllowedParticleMask;
        }

        return mask;
    }

    /// <summary>
    /// Counts the set bits of a particle mask.
    /// </summary>
    /// <param name="mask">The particle mask.</param>
    /// <returns>The number of set bits.</returns>
    public static int PopCount(ulong mask) => System.Numerics.BitOperations.PopCount(mask);

    private static void ValidateStructure(CompiledModel model)
    {
        var structure = model.Structure;
        var sites = model.Structure.Sites;
        var positions = structure.Positions;
        if (sites.IsDefault || positions.IsDefault)
        {
            throw Invalid("The compiled structure must provide site and position arrays.");
        }

        for (var index = 0; index < sites.Length; index++)
        {
            var site = sites[index];
            if (site.Index != index)
            {
                throw Invalid($"Compiled site index {site.Index} does not match array position {index}.");
            }

            var positionIndices = site.PositionIndices;
            if (positionIndices.IsDefault)
            {
                throw Invalid($"Compiled site {site.Id:D} has no position index array.");
            }

            foreach (var positionIndex in positionIndices)
            {
                if (positionIndex < 0 || positionIndex >= positions.Length)
                {
                    throw Invalid($"Compiled site {site.Id:D} references an unknown position {positionIndex}.");
                }

                if (positions[positionIndex].SiteIndex != site.Index)
                {
                    throw Invalid($"Compiled position {positionIndex} does not belong to site {site.Index}.");
                }
            }
        }

        for (var index = 0; index < positions.Length; index++)
        {
            var position = positions[index];
            if (position.P != index)
            {
                throw Invalid($"Compiled position {position.P} does not match array position {index}.");
            }

            if (position.SiteIndex < 0 || position.SiteIndex >= sites.Length)
            {
                throw Invalid($"Compiled position {position.P} references an unknown site {position.SiteIndex}.");
            }
        }
    }

    private static void ValidateEnergy(CompiledModel model)
    {
        var particles = model.Particles.Count;
        var sites = model.Structure.Sites;
        var positionCount = model.Structure.PositionCount;
        var pairTables = model.Energy.PairTables;
        if (pairTables.IsDefault || model.Energy.PairGeometries.IsDefault || model.Energy.ClusterTables.IsDefault)
        {
            throw Invalid("The compiled energy model must provide geometry, pair-table, and cluster-table arrays.");
        }

        var geometriesByTable = GroupGeometries(model);
        for (var index = 0; index < pairTables.Length; index++)
        {
            var table = pairTables[index];
            if (table.Index != index)
            {
                throw Invalid($"Compiled pair table index {table.Index} does not match array position {index}.");
            }

            if (table.ParticleCount != particles)
            {
                throw Invalid(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Pair table {table.Index} declares {table.ParticleCount} particles but the compiled particle table has {particles}."));
            }

            var cellCount = checked(particles * particles);
            if (table.Values.IsDefault || table.Values.Length != cellCount)
            {
                throw Invalid(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Pair table {table.Index} has {Length(table.Values)} values; expected {cellCount}."));
            }

            var geometries = geometriesByTable[index];
            if (geometries.Count == 0)
            {
                throw Invalid($"Pair table {table.Index} has no compiled geometry.");
            }

            var representative = SelectRepresentative(table, geometries);
            var stability = sites[representative.CenterSiteIndex].Stability;
            foreach (var geometry in geometries)
            {
                if (!string.Equals(geometry.EnergyId, table.EnergyId, StringComparison.Ordinal))
                {
                    throw Invalid($"Pair table {table.Index} contains geometry '{geometry.GeometryId}' with another energy ID.");
                }

                ValidateSiteIndex(sites.Length, geometry.CenterSiteIndex, geometry.GeometryId, "centre");
                ValidateSiteIndex(sites.Length, geometry.PartnerSiteIndex, geometry.GeometryId, "partner");
                if (sites[geometry.CenterSiteIndex].Stability != stability)
                {
                    throw Invalid($"Pair table {table.Index} mixes centre reference-site stabilities.");
                }

                if (geometry.IsChiral != representative.IsChiral)
                {
                    throw Invalid($"Pair table {table.Index} mixes chiral and achiral geometries.");
                }

                if (!representative.IsChiral &&
                    (geometry.CenterSiteId != representative.CenterSiteId ||
                     geometry.PartnerSiteId != representative.PartnerSiteId ||
                     geometry.DistanceBucketFm != representative.DistanceBucketFm))
                {
                    throw Invalid($"Pair table {table.Index} has conflicting display descriptors.");
                }
            }

            if (representative.IsChiral)
            {
                if (geometries.Count < 2)
                {
                    throw Invalid($"Chiral pair table {table.Index} has no reciprocal geometry.");
                }

                foreach (var geometry in geometries)
                {
                    if (geometry.ChiralPartnerGeometryId is not { } partner ||
                        !geometries.Any(item => string.Equals(item.GeometryId, partner, StringComparison.Ordinal)))
                    {
                        throw Invalid($"Chiral pair table {table.Index} has a broken chiral partner link.");
                    }
                }
            }
        }

        var clusterTables = model.Energy.ClusterTables;
        for (var index = 0; index < clusterTables.Length; index++)
        {
            var table = clusterTables[index];
            if (table.Index != index)
            {
                throw Invalid($"Compiled cluster table index {table.Index} does not match array position {index}.");
            }

            if (table.CenterSiteIndex < 0 || table.CenterSiteIndex >= sites.Length)
            {
                throw Invalid($"Cluster table {table.Index} references an unknown centre site {table.CenterSiteIndex}.");
            }

            if (table.CenterP < 0 || table.CenterP >= positionCount)
            {
                throw Invalid($"Cluster table {table.Index} references an unknown centre position {table.CenterP}.");
            }

            if (table.Energies.IsDefault || table.Occupations.IsDefault ||
                table.Energies.Length != table.Occupations.Length)
            {
                throw Invalid($"Cluster table {table.Index} has inconsistent occupation and energy arrays.");
            }

            var siteCount = table.SitePIndices.IsDefault ? 0 : table.SitePIndices.Length;
            foreach (var occupation in table.Occupations)
            {
                if (occupation.CanonicalTuple.IsDefault || occupation.CanonicalTuple.Length != siteCount)
                {
                    throw Invalid($"Cluster table {table.Index} has an occupation tuple with the wrong length.");
                }

                foreach (var particle in occupation.CanonicalTuple)
                {
                    if (particle < 0 || particle >= particles)
                    {
                        throw Invalid($"Cluster table {table.Index} has an occupation with an unknown particle index.");
                    }
                }
            }
        }
    }

    private static void ValidateTransitions(CompiledModel model)
    {
        var transitions = model.Transitions;
        var collections = transitions.Collections;
        var directions = transitions.Directions;
        var rules = transitions.Rules;
        if (collections.IsDefault || directions.IsDefault || rules.IsDefault)
        {
            throw Invalid("The compiled transition model must provide collection, direction, and rule arrays.");
        }

        var directionsByCollection = new List<int>[collections.Length];
        var rulesByCollection = new List<int>[collections.Length];
        for (var index = 0; index < collections.Length; index++)
        {
            directionsByCollection[index] = [];
            rulesByCollection[index] = [];
        }

        for (var index = 0; index < directions.Length; index++)
        {
            var direction = directions[index];
            if (direction.Index != index || direction.CollectionIndex < 0 || direction.CollectionIndex >= collections.Length)
            {
                throw Invalid($"Compiled direction {index} has an inconsistent collection index.");
            }

            directionsByCollection[direction.CollectionIndex].Add(index);
        }

        for (var index = 0; index < rules.Length; index++)
        {
            var rule = rules[index];
            if (rule.Index != index || rule.CollectionIndex < 0 || rule.CollectionIndex >= collections.Length)
            {
                throw Invalid($"Compiled rule {index} has an inconsistent collection index.");
            }

            rulesByCollection[rule.CollectionIndex].Add(index);
        }

        for (var index = 0; index < collections.Length; index++)
        {
            var collection = collections[index];
            if (collection.Index != index)
            {
                throw Invalid($"Compiled collection index {collection.Index} does not match array position {index}.");
            }

            var expectedKind = collection.Transition.Kind == TransitionKind.Kinetic
                ? SimulationKind.Kmc
                : SimulationKind.Mmc;
            if (collection.Kind != expectedKind)
            {
                throw Invalid($"Compiled collection {collection.Index} has an inconsistent simulation kind.");
            }

            if (collection.DirectionIndices.IsDefault || collection.DirectionIndices.Length != directionsByCollection[index].Count)
            {
                throw Invalid($"Compiled collection {collection.Index} has an inconsistent direction index array.");
            }

            for (var item = 0; item < collection.DirectionIndices.Length; item++)
            {
                if (collection.DirectionIndices[item] != directionsByCollection[index][item])
                {
                    throw Invalid($"Compiled collection {collection.Index} has an inconsistent direction index array.");
                }
            }

            if (collection.RuleIndices.IsDefault || collection.RuleIndices.Length != rulesByCollection[index].Count)
            {
                throw Invalid($"Compiled collection {collection.Index} has an inconsistent rule index array.");
            }

            for (var item = 0; item < collection.RuleIndices.Length; item++)
            {
                if (collection.RuleIndices[item] != rulesByCollection[index][item])
                {
                    throw Invalid($"Compiled collection {collection.Index} has an inconsistent rule index array.");
                }
            }
        }
    }

    private static List<CompiledPairGeometry>[] GroupGeometries(CompiledModel model)
    {
        var tableCount = model.Energy.PairTables.Length;
        var grouped = new List<CompiledPairGeometry>[tableCount];
        for (var index = 0; index < tableCount; index++)
        {
            grouped[index] = [];
        }

        foreach (var geometry in model.Energy.PairGeometries)
        {
            if (geometry.PairTableIndex < 0 || geometry.PairTableIndex >= tableCount)
            {
                throw Invalid($"Compiled pair geometry '{geometry.GeometryId}' references an unknown table.");
            }

            grouped[geometry.PairTableIndex].Add(geometry);
        }

        return grouped;
    }

    private static CompiledPairGeometry SelectRepresentative(
        CompiledPairTable table,
        List<CompiledPairGeometry> geometries)
    {
        foreach (var geometry in geometries)
        {
            if (string.Equals(geometry.GeometryId, table.EnergyId, StringComparison.Ordinal))
            {
                return geometry;
            }
        }

        if (geometries.Count == 0)
        {
            throw Invalid($"Pair table {table.Index} has no geometry.");
        }

        if (geometries[0].IsChiral)
        {
            throw Invalid($"Chiral pair table {table.Index} has no geometry matching its energy identity.");
        }

        return geometries[0];
    }

    private static void ValidateSiteIndex(int siteCount, int index, string geometryId, string role)
    {
        if (index < 0 || index >= siteCount)
        {
            throw Invalid($"Compiled pair geometry '{geometryId}' references an unknown {role} site {index}.");
        }
    }

    private static int CountUnorderedSlots(ulong centerMask, ulong partnerMask)
    {
        var pairs = new HashSet<(int Center, int Partner)>();
        for (var center = 0; center < 64; center++)
        {
            if ((centerMask & (1UL << center)) == 0)
            {
                continue;
            }

            for (var partner = 0; partner < 64; partner++)
            {
                if ((partnerMask & (1UL << partner)) == 0)
                {
                    continue;
                }

                pairs.Add(center <= partner ? (center, partner) : (partner, center));
            }
        }

        return pairs.Count;
    }

    private static int Length<T>(ImmutableArray<T> values) => values.IsDefault ? 0 : values.Length;

    private static InvalidOperationException Invalid(string message) =>
        new(string.Create(CultureInfo.InvariantCulture, $"Compiled-model contract violation: {message}"));
}
