// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Security.Cryptography;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Core.Project;

/// <summary>
/// Computes SHA-256 content hashes for project values.
/// </summary>
public static class ProjectHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of the canonical model section.
    /// </summary>
    /// <param name="project">The project whose model section is hashed.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// This is the Step 01 model-content identity used for dirty tracking and for the future compiled
    /// model signature. It is deliberately distinct from <see cref="ComputeDocument"/> and
    /// <see cref="ComputeStructure"/>.
    /// </remarks>
    public static string Compute(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var bytes = ProjectJson.SerializeCanonicalModel(project.Model);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the canonical structure section.
    /// </summary>
    /// <param name="structure">The structure to hash.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// The projection covers the structure name, space group, cell parameters, sites, and dummy positions.
    /// Non-semantic site and dummy arrays are sorted by ID. This is the structure-preview cache key, not a
    /// model or document identity.
    /// </remarks>
    public static string ComputeStructure(StructureModel structure)
    {
        ArgumentNullException.ThrowIfNull(structure);
        var bytes = ProjectJson.SerializeCanonicalStructure(structure);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the entire canonical project document.
    /// </summary>
    /// <param name="project">The project to hash.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    public static string ComputeDocument(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var bytes = ProjectCanonicalizer.ToCanonicalUtf8(project);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the canonical energy projection of a model.
    /// </summary>
    /// <param name="model">The model whose energy sections are hashed.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// The projection covers the stable environment, unstable environments, clusters, and site energies only.
    /// It is invariant to particle, structure, and transition edits, so it is the energy-model half of the
    /// Step 08 compilation cache key. Canonical sorting is applied before hashing.
    /// </remarks>
    public static string ComputeEnergy(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var projection = new ModelDefinition
        {
            StableEnvironment = model.StableEnvironment,
            UnstableEnvironments = model.UnstableEnvironments,
            Clusters = model.Clusters,
            SiteEnergies = model.SiteEnergies,
        };
        var bytes = ProjectJson.SerializeCanonicalModel(projection);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the canonical simulation active-transition selection.
    /// </summary>
    /// <param name="project">The project to hash.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// The projection covers the simulation identifiers, kinds, and authored transition references only. It is
    /// the Step 08 rules cache-key input: adding or removing a reference changes the required rule set while
    /// runtime-only simulation edits do not.
    /// </remarks>
    public static string ComputeSimulationSelection(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var simulations = project.Simulations.IsDefault
            ? ImmutableArray<Simulation>.Empty
            : project.Simulations
                .Select(static simulation => simulation.Kind == SimulationKind.Kmc
                    ? (Simulation)new KineticSimulation
                    {
                        Id = simulation.Id,
                        Name = string.Empty,
                        TransitionIds = simulation.TransitionIds,
                    }
                    : new MetropolisSimulation
                    {
                        Id = simulation.Id,
                        Name = string.Empty,
                        TransitionIds = simulation.TransitionIds,
                    })
                .ToImmutableArray();
        var projection = new ModelDefinition
        {
            Simulations = simulations,
        };
        var bytes = ProjectJson.SerializeCanonicalModel(projection);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Computes the SHA-256 hash of the canonical closure of one transition.
    /// </summary>
    /// <param name="project">The read-only project snapshot.</param>
    /// <param name="transition">The transition reference.</param>
    /// <returns>A lowercase hexadecimal SHA-256 digest.</returns>
    /// <remarks>
    /// The closure contains the transition, its chain, the referenced groups and state changes, and the full
    /// particle order (authored particles plus the implicit void particle). The preview cache uses this value,
    /// so adding or removing an unreferenced particle also changes the key.
    /// </remarks>
    public static string ComputeTransition(IProjectQuery project, TransitionRef transition)
    {
        ArgumentNullException.ThrowIfNull(project);

        var kineticTransitions = project.KineticTransitions.IsDefault
            ? ImmutableArray<KineticTransition>.Empty
            : project.KineticTransitions;
        var metropolisTransitions = project.MetropolisTransitions.IsDefault
            ? ImmutableArray<MetropolisTransition>.Empty
            : project.MetropolisTransitions;
        var kinetic = transition.IsKinetic
            ? kineticTransitions.Where(item => item.Id.Value == transition.Id).ToImmutableArray()
            : ImmutableArray<KineticTransition>.Empty;
        var metropolis = transition.IsKinetic
            ? ImmutableArray<MetropolisTransition>.Empty
            : metropolisTransitions.Where(item => item.Id.Value == transition.Id).ToImmutableArray();
        var chainIds = kinetic.Select(static item => item.ChainId)
            .Concat(metropolis.Select(static item => item.ChainId))
            .ToHashSet();
        var chains = (project.Chains.IsDefault ? [] : project.Chains)
            .Where(chain => chainIds.Contains(chain.Id))
            .ToImmutableArray();
        var groupIds = chains
            .SelectMany(static chain => chain.GroupIds.IsDefault ? [] : chain.GroupIds)
            .ToHashSet();
        var groups = (project.StateChangeGroups.IsDefault ? [] : project.StateChangeGroups)
            .Where(group => groupIds.Contains(group.Id))
            .ToImmutableArray();
        var stateChangeIds = groups
            .SelectMany(static group => group.StateChangeIds.IsDefault ? [] : group.StateChangeIds)
            .ToHashSet();
        var stateChanges = (project.StateChanges.IsDefault ? [] : project.StateChanges)
            .Where(stateChange => stateChangeIds.Contains(stateChange.Id))
            .ToImmutableArray();
        var particleOrder = (project.Particles.IsDefault ? [] : project.Particles)
            .OrderBy(static particle => particle.Id.Value)
            .Append(project.VoidParticle)
            .ToImmutableArray();

        var closure = new ModelDefinition
        {
            Particles = particleOrder,
            StateChanges = stateChanges,
            StateChangeGroups = groups,
            Chains = chains,
            KineticTransitions = kinetic,
            MetropolisTransitions = metropolis,
            Structure = StructureModel.Default,
        };
        var bytes = ProjectJson.SerializeCanonicalModel(closure);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
