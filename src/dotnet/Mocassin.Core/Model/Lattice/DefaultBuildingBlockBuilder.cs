// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model.Lattice;

/// <summary>
/// Derives the implicit default building block from a model snapshot.
/// </summary>
/// <remarks>
/// A stable site uses the first particle of its ordered particle set; an unstable site uses the implicit void
/// particle. Sites with a missing or empty particle set are skipped because the particle and structure
/// validators already report them.
/// </remarks>
internal static class DefaultBuildingBlockBuilder
{
    /// <summary>
    /// Builds the derived default building block in authored site order.
    /// </summary>
    /// <param name="model">The model to derive the building block from.</param>
    /// <returns>The derived entries.</returns>
    public static ImmutableArray<BuildingBlockEntry> Build(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sites = model.Structure.Sites.IsDefault
            ? ImmutableArray<CellSite>.Empty
            : model.Structure.Sites;
        var particleSets = model.ParticleSets.IsDefault
            ? ImmutableArray<ParticleSet>.Empty
            : model.ParticleSets;
        var particleSetById = new Dictionary<ParticleSetId, ParticleSet>();
        foreach (var particleSet in particleSets)
        {
            particleSetById.TryAdd(particleSet.Id, particleSet);
        }

        var builder = ImmutableArray.CreateBuilder<BuildingBlockEntry>(sites.Length);
        foreach (var site in sites)
        {
            if (site.Stability == SiteStability.Unstable)
            {
                builder.Add(new BuildingBlockEntry(site.Id, ParticleId.Void));
                continue;
            }

            if (!particleSetById.TryGetValue(site.ParticleSetId, out var particleSet) ||
                particleSet.ParticleIds.IsDefaultOrEmpty)
            {
                continue;
            }

            builder.Add(new BuildingBlockEntry(site.Id, particleSet.ParticleIds[0]));
        }

        return builder.ToImmutable();
    }
}
