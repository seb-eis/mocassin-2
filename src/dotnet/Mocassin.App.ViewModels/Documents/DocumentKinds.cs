// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Documents;

/// <summary>
/// Stable document kind strings. Later steps add model document kinds without changing the key shape.
/// </summary>
internal static class DocumentKinds
{
    /// <summary>
    /// The welcome document.
    /// </summary>
    public const string Welcome = "welcome";

    /// <summary>
    /// The read-only project overview.
    /// </summary>
    public const string ProjectOverview = "project.overview";

    /// <summary>
    /// The read-only canonical project JSON.
    /// </summary>
    public const string ProjectJson = "project.json";

    /// <summary>
    /// The particle editor document.
    /// </summary>
    public const string ModelParticles = "model.particles";

    /// <summary>
    /// The particle-set editor document.
    /// </summary>
    public const string ModelParticleSets = "model.particleSets";

    /// <summary>
    /// The structure form document.
    /// </summary>
    public const string ModelStructure = "model.structure";

    /// <summary>
    /// The reference-site editor document.
    /// </summary>
    public const string ModelSites = "model.sites";

    /// <summary>
    /// The dummy-position editor document.
    /// </summary>
    public const string ModelDummyPositions = "model.dummyPositions";
}
