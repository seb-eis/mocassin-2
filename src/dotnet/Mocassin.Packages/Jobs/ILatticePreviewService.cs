// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// Creates lattice and job-count previews over the same generation path as the job build.
/// </summary>
/// <remarks>
/// The service never compiles and never writes a package. It takes the current project and its compiled model,
/// so it cannot silently recompile stale project data. Unknown dopings, non-finite or out-of-range fractions,
/// invalid cell sizes, and impossible compensation return <see langword="null"/> plus diagnostics; a null project
/// or compiled model is a programmer error and throws. Previews are cached in a bounded, thread-safe LRU keyed by
/// the project model hash, lattice request hash, canonical doping list, and the catalog/symmetry provenance.
/// </remarks>
public interface ILatticePreviewService
{
    /// <summary>
    /// Creates the preview of one requested supercell and doping list.
    /// </summary>
    /// <param name="project">The current project that supplies the authored model and dopings.</param>
    /// <param name="model">The current compiled model that supplies the P1 positions and compiled particles.</param>
    /// <param name="request">The requested supercell size.</param>
    /// <param name="dopings">The requested doping values; an empty array means none.</param>
    /// <param name="diagnostics">The diagnostics of the preview, including harmless build warnings on success.</param>
    /// <returns>The preview, or <see langword="null"/> on invalid user data.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="project"/>, <paramref name="model"/>, or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    public LatticePreview? TryCreatePreview(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings,
        out ImmutableArray<Diagnostic> diagnostics);

    /// <summary>
    /// Creates the simulation-scoped job-count preview of one template.
    /// </summary>
    /// <param name="project">The current project that owns the referenced simulations.</param>
    /// <param name="jobTemplate">The job template to inspect.</param>
    /// <param name="simulationId">The simulation that scopes the default and effective counts.</param>
    /// <param name="diagnostics">The diagnostics of the preview, empty on success.</param>
    /// <returns>The job-count preview, or <see langword="null"/> on an unknown simulation or invalid count.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="project"/> or <paramref name="jobTemplate"/> is <see langword="null"/>.
    /// </exception>
    public JobCountPreview? TryCreateJobCountPreview(
        Project project,
        JobTemplate jobTemplate,
        SimulationId simulationId,
        out ImmutableArray<Diagnostic> diagnostics);
}
