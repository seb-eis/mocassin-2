// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Contracts;
using Mocassin.Symmetry.SpaceGroups;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Packages.Projects;

/// <summary>
/// Composes Core, Symmetry, and project-envelope validation into one stable diagnostic list.
/// </summary>
/// <remarks>
/// This is the single composition point for project validation. <c>ProjectEditor</c> keeps the Core-only
/// default; the UI and CLI pass <see cref="Validate(Project)"/> explicitly when catalog-dependent validation
/// is required.
/// </remarks>
public static class ProjectValidationService
{
    /// <summary>
    /// Validates a project with the embedded catalog and symmetry contract.
    /// </summary>
    /// <param name="project">The project to validate.</param>
    /// <returns>The composed diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(Project project)
    {
        var catalog = SpaceGroupCatalog.Default;
        return Validate(
            project,
            new StructureValidator(catalog),
            SymmetryVersionInfo.FromCatalog(catalog),
            new TransitionPathValidator(catalog));
    }

    /// <summary>
    /// Validates a project with explicit structure-validation and version dependencies.
    /// </summary>
    /// <param name="project">The project to validate.</param>
    /// <param name="structureValidator">The Symmetry-side structure validator.</param>
    /// <param name="symmetryVersions">The expected catalog and contract versions.</param>
    /// <returns>The composed diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(
        Project project,
        IStructureValidator structureValidator,
        SymmetryVersionInfo symmetryVersions) =>
        Validate(project, structureValidator, symmetryVersions, new TransitionPathValidator());

    /// <summary>
    /// Validates a project with explicit Core, structure, transition-path, and version dependencies.
    /// </summary>
    /// <param name="project">The project to validate.</param>
    /// <param name="structureValidator">The Symmetry-side structure validator.</param>
    /// <param name="symmetryVersions">The expected catalog and contract versions.</param>
    /// <param name="transitionPathValidator">The Symmetry-side transition-path validator.</param>
    /// <returns>The composed diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(
        Project project,
        IStructureValidator structureValidator,
        SymmetryVersionInfo symmetryVersions,
        TransitionPathValidator transitionPathValidator)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(structureValidator);
        ArgumentNullException.ThrowIfNull(symmetryVersions);
        ArgumentNullException.ThrowIfNull(transitionPathValidator);

        var bag = new DiagnosticBag();
        bag.AddRange(ProjectValidator.Validate(project));
        bag.AddRange(structureValidator.Validate(project.Model.Structure));
        bag.AddRange(transitionPathValidator.Validate(project.Model));
        ValidateSymmetryMetadata(project, symmetryVersions, bag);
        ValidateParticleSetCharges(project, bag);
        return bag.Sorted;
    }

    private static void ValidateSymmetryMetadata(
        Project project,
        SymmetryVersionInfo expected,
        DiagnosticBag bag)
    {
        var metadata = project.Symmetry;
        if (metadata is null)
        {
            return;
        }

        var projectId = project.Metadata.ProjectId;
        CompareMetadataValue(
            metadata.SpaceGroupDataVersion,
            expected.SpaceGroupDataVersion,
            "spaceGroupDataVersion",
            projectId,
            bag);
        CompareMetadataValue(
            metadata.SpaceGroupDataSha256,
            expected.SpaceGroupDataSha256,
            "spaceGroupDataSha256",
            projectId,
            bag);
        CompareMetadataValue(
            metadata.SymmetryContractVersion,
            expected.SymmetryContractVersion,
            "symmetryContractVersion",
            projectId,
            bag);
    }

    private static void CompareMetadataValue(
        string? actual,
        string expected,
        string fieldName,
        Guid projectId,
        DiagnosticBag bag)
    {
        if (actual is null || string.Equals(actual, expected, StringComparison.Ordinal))
        {
            return;
        }

        bag.Add(new Diagnostic(
            ProjectDiagnosticCodes.SymmetryMetadataMismatch,
            DiagnosticSeverity.Warning,
            $"Project symmetry metadata '{fieldName}' is '{actual}', but the embedded value is '{expected}'.",
            new ModelPath("project", projectId, $"symmetry.{fieldName}")));
    }

    private static void ValidateParticleSetCharges(Project project, DiagnosticBag bag)
    {
        var particles = project.Model.Particles;
        var particleSets = project.Model.ParticleSets;
        if (particles.IsDefault || particleSets.IsDefault)
        {
            return;
        }

        var byId = new Dictionary<ParticleId, Particle>();
        foreach (var particle in particles)
        {
            byId[particle.Id] = particle;
        }

        foreach (var particleSet in particleSets)
        {
            if (particleSet.ParticleIds.IsDefault || particleSet.ParticleIds.Length == 0)
            {
                continue;
            }

            if (!byId.TryGetValue(particleSet.ParticleIds[0], out var defaultParticle)
                || defaultParticle.Charge.Value == 0.0)
            {
                continue;
            }

            foreach (var memberId in particleSet.ParticleIds)
            {
                if (!byId.TryGetValue(memberId, out var member)
                    || !member.IsVacancy
                    || member.Charge.Value == 0.0)
                {
                    continue;
                }

                bag.Add(new Diagnostic(
                    ParticleDiagnosticCodes.ChargedVacancy,
                    DiagnosticSeverity.Warning,
                    $"Particle set '{particleSet.Name}' contains charged vacancy '{member.Name}' while " +
                    $"its default occupation '{defaultParticle.Name}' is also charged; set one of them to charge 0.",
                    new ModelPath("particleSet", particleSet.Id.Value, "particleIds")));
                break;
            }
        }
    }
}
