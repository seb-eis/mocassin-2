// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;

namespace Mocassin.Symmetry.Transitions;

/// <summary>
/// Creates advisory transition previews from a same-snapshot project query.
/// </summary>
public interface ITransitionPreviewService
{
    /// <summary>
    /// Tries to create a kinetic-transition preview.
    /// </summary>
    /// <param name="transition">The transition to preview.</param>
    /// <param name="project">The same-snapshot project query.</param>
    /// <param name="cellCountA">The requested supercell count along a.</param>
    /// <param name="cellCountB">The requested supercell count along b.</param>
    /// <param name="cellCountC">The requested supercell count along c.</param>
    /// <param name="diagnostics">The diagnostics when the preview is rejected.</param>
    /// <returns>The preview, or <see langword="null"/> when the transition is invalid.</returns>
    public TransitionPreview? TryCreatePreview(
        KineticTransition transition,
        IProjectQuery project,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);

    /// <summary>
    /// Tries to create a Metropolis-transition preview.
    /// </summary>
    /// <param name="transition">The transition to preview.</param>
    /// <param name="project">The same-snapshot project query.</param>
    /// <param name="cellCountA">The requested supercell count along a.</param>
    /// <param name="cellCountB">The requested supercell count along b.</param>
    /// <param name="cellCountC">The requested supercell count along c.</param>
    /// <param name="diagnostics">The diagnostics when the preview is rejected.</param>
    /// <returns>The preview, or <see langword="null"/> when the transition is invalid.</returns>
    public TransitionPreview? TryCreatePreview(
        MetropolisTransition transition,
        IProjectQuery project,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}
