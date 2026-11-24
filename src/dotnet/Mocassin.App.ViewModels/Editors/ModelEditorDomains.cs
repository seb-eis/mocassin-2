// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Documents;

namespace Mocassin.App.ViewModels.Editors;

/// <summary>
/// The five 24A model-editor domains and their descriptors. The names are the model-path domains diagnostics
/// already use.
/// </summary>
internal static class ModelEditorDomains
{
    /// <summary>
    /// The particle domain.
    /// </summary>
    public const string Particle = "particle";

    /// <summary>
    /// The particle-set domain.
    /// </summary>
    public const string ParticleSet = "particleSet";

    /// <summary>
    /// The structure domain. Its diagnostics carry <see cref="Guid.Empty"/> plus a property.
    /// </summary>
    public const string Structure = "structure";

    /// <summary>
    /// The reference-site domain.
    /// </summary>
    public const string CellSite = "cellSite";

    /// <summary>
    /// The visual-only dummy-position domain.
    /// </summary>
    public const string CellDummyPosition = "cellDummyPosition";

    /// <summary>
    /// Gets the particle editor descriptor.
    /// </summary>
    public static ModelEditorDescriptor Particles { get; } = new(
        Particle,
        DocumentKinds.ModelParticles,
        "Particles",
        "No particles yet — add a particle or open a sample project.",
        "particle");

    /// <summary>
    /// Gets the particle-set editor descriptor.
    /// </summary>
    public static ModelEditorDescriptor ParticleSets { get; } = new(
        ParticleSet,
        DocumentKinds.ModelParticleSets,
        "Particle sets",
        "No particle sets yet — add a set and choose its ordered members.",
        "particle set");

    /// <summary>
    /// Gets the structure editor descriptor.
    /// </summary>
    public static ModelEditorDescriptor StructureEditor { get; } = new(
        Structure,
        DocumentKinds.ModelStructure,
        "Structure",
        "Choose a space group and cell parameters to describe the structure.",
        "structure");

    /// <summary>
    /// Gets the site editor descriptor.
    /// </summary>
    public static ModelEditorDescriptor Sites { get; } = new(
        CellSite,
        DocumentKinds.ModelSites,
        "Sites",
        "No sites yet — add a reference site with its particle set and position.",
        "site");

    /// <summary>
    /// Gets the dummy-position editor descriptor.
    /// </summary>
    public static ModelEditorDescriptor DummyPositions { get; } = new(
        CellDummyPosition,
        DocumentKinds.ModelDummyPositions,
        "Dummy positions",
        "No dummy positions yet — add a visual-only marker.",
        "dummy position");

    /// <summary>
    /// Gets every 24A descriptor in explorer order.
    /// </summary>
    public static IReadOnlyList<ModelEditorDescriptor> All { get; } =
    [
        Particles,
        ParticleSets,
        StructureEditor,
        Sites,
        DummyPositions,
    ];

    /// <summary>
    /// Finds a descriptor by model-path domain.
    /// </summary>
    /// <param name="domain">The model-path domain.</param>
    /// <returns>The descriptor, or <see langword="null"/>.</returns>
    public static ModelEditorDescriptor? Find(string domain) =>
        All.FirstOrDefault(candidate => string.Equals(candidate.Domain, domain, StringComparison.Ordinal));
}
