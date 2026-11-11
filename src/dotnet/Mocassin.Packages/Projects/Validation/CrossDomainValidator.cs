// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Energy;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.Structure;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Runs the cross-domain checks that cannot live in one model domain.
/// </summary>
internal sealed class CrossDomainValidator
{
    private readonly IStructurePreviewService structurePreview;
    private readonly ITransitionPreviewService transitionPreview;

    /// <summary>
    /// Initializes the cross-domain validator.
    /// </summary>
    /// <param name="structurePreview">The structure preview service.</param>
    /// <param name="transitionPreview">The transition preview service.</param>
    public CrossDomainValidator(
        IStructurePreviewService structurePreview,
        ITransitionPreviewService transitionPreview)
    {
        this.structurePreview = structurePreview;
        this.transitionPreview = transitionPreview;
    }

    /// <summary>
    /// Validates cross-domain relationships.
    /// </summary>
    /// <param name="project">The project snapshot.</param>
    /// <param name="options">The run options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cross-domain diagnostics.</returns>
    public ImmutableArray<Diagnostic> Validate(
        Project project,
        ValidationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var transitions = project.Model.KineticTransitions;
        if (transitions.IsDefault || transitions.Length == 0)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        var structure = structurePreview.TryCreatePreview(
            project.Model.Structure,
            1,
            1,
            1,
            out _);
        if (structure is null ||
            !CellGeometry.TryCreate(project.Model.Structure.Cell, out var cell, out _) ||
            cell is null)
        {
            return ImmutableArray<Diagnostic>.Empty;
        }

        var geometry = new PeriodicGeometry(cell, structure.Encoder);
        var stableCutoff = (project.Model.StableEnvironment ?? StableEnvironment.Default).Cutoff.Value;
        var unstableCutoffs = new Dictionary<Guid, double>();
        foreach (var environment in project.Model.UnstableEnvironments.IsDefault
                     ? ImmutableArray<UnstableEnvironment>.Empty
                     : project.Model.UnstableEnvironments)
        {
            unstableCutoffs[environment.SiteId.Value] = environment.Cutoff.Value;
        }

        var bag = new DiagnosticBag();
        foreach (var transition in transitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var preview = transitionPreview.TryCreatePreview(
                transition,
                project,
                1,
                1,
                1,
                out _);
            if (preview is null)
            {
                // Path resolution or stability errors explain why no usable path exists.
                continue;
            }

            var path = preview.ResolvedPath;
            for (var index = 0; index + 1 < path.Length; index++)
            {
                var from = path[index];
                var cutoff = from.Stability == SiteStability.Stable
                    ? stableCutoff
                    : unstableCutoffs.TryGetValue(from.SiteId.Value, out var unstableCutoff)
                        ? unstableCutoff
                        : stableCutoff;

                if (!double.IsFinite(cutoff) || cutoff <= 0)
                {
                    // ENM validation already reports the invalid cutoff.
                    continue;
                }

                var image = geometry.GetMinimumImage(from.Encoding, path[index + 1].Encoding);
                if (image.DistanceAngstrom > cutoff)
                {
                    bag.Add(new Diagnostic(
                        EnergyDiagnosticCodes.KmcPathOutsideEnvironment,
                        DiagnosticSeverity.Warning,
                        $"Kinetic path hop {index}->{index + 1} is {image.DistanceAngstrom:0.###} Å, beyond the {cutoff:0.###} Å environment cutoff.",
                        new ModelPath("kineticTransition", transition.Id.Value, $"referencePath[{index}]"),
                        "Increase the environment cutoff or shorten the KMC transition path."));
                    break;
                }
            }
        }

        return bag.Sorted;
    }
}
