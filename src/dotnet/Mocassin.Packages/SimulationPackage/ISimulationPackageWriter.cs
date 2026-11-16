// SPDX-License-Identifier: MIT

using Mocassin.Packages.Parameterization;

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// Streams a compiled model and generated jobs into an immutable <c>.msl</c> package.
/// </summary>
/// <remarks>
/// <see cref="CompleteAsync"/> checkpoints, closes, hashes, fsyncs, and atomically renames the temporary build
/// file over the target. Disposing without completing deletes the temporary file and leaves an existing package
/// untouched.
/// </remarks>
public interface ISimulationPackageWriter : IAsyncDisposable
{
    /// <summary>
    /// Writes the one parameterized model of the package.
    /// </summary>
    /// <param name="model">The compiled and parameterized model.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the model row is committed.</returns>
    public Task WriteModelAsync(ParameterizedModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes one expanded job and its initial lattice.
    /// </summary>
    /// <param name="job">The descriptor/lattice pair.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes when the job row is inserted or buffered in the current batch.</returns>
    public Task WriteJobAsync(JobPackage job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes and publishes the package.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation before the publication barrier.</param>
    /// <returns>The published path, byte hash, counts, size, and advisory warnings.</returns>
    public Task<PackageWriteResult> CompleteAsync(CancellationToken cancellationToken = default);
}
