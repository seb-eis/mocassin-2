// SPDX-License-Identifier: MIT

using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.Tests.LayoutSpike.Data;

/// <summary>
/// Resolves spike rows back to model objects without re-implementing the project graph.
/// </summary>
internal static class SpikeModelLookup
{
    /// <summary>
    /// Finds a particle, including the implicit void particle.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The particle identifier.</param>
    /// <returns>The particle, or <see langword="null"/>.</returns>
    public static Particle? FindParticle(ProjectModel project, Guid id) =>
        id == Particle.Void.Id.Value ? Particle.Void : project.Particles.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a particle set.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The set identifier.</param>
    /// <returns>The set, or <see langword="null"/>.</returns>
    public static ParticleSet? FindSet(ProjectModel project, Guid id) =>
        project.Model.ParticleSets.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a cell site.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The site identifier.</param>
    /// <returns>The site, or <see langword="null"/>.</returns>
    public static CellSite? FindSite(ProjectModel project, Guid id) =>
        project.Structure.Sites.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a state change.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The state-change identifier.</param>
    /// <returns>The state change, or <see langword="null"/>.</returns>
    public static StateChange? FindStateChange(ProjectModel project, Guid id) =>
        project.StateChanges.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a transition chain.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The chain identifier.</param>
    /// <returns>The chain, or <see langword="null"/>.</returns>
    public static TransitionChain? FindChain(ProjectModel project, Guid id) =>
        project.Chains.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a kinetic transition.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The transition identifier.</param>
    /// <returns>The transition, or <see langword="null"/>.</returns>
    public static KineticTransition? FindKinetic(ProjectModel project, Guid id) =>
        project.KineticTransitions.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds a Metropolis transition.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The transition identifier.</param>
    /// <returns>The transition, or <see langword="null"/>.</returns>
    public static MetropolisTransition? FindMetropolis(ProjectModel project, Guid id) =>
        project.MetropolisTransitions.FirstOrDefault(item => item.Id.Value == id);

    /// <summary>
    /// Finds the chain referenced by either transition kind.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="id">The transition identifier.</param>
    /// <returns>The chain, or <see langword="null"/>.</returns>
    public static TransitionChain? FindTransitionChain(ProjectModel project, Guid id)
    {
        var kinetic = FindKinetic(project, id);
        if (kinetic is not null)
        {
            return FindChain(project, kinetic.ChainId.Value);
        }

        var metropolis = FindMetropolis(project, id);
        return metropolis is null ? null : FindChain(project, metropolis.ChainId.Value);
    }
}
