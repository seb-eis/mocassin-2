// SPDX-License-Identifier: MIT

using System.Text.Json;

namespace Mocassin.Results.Packages;

/// <summary>
/// Reads the structural content of a published <c>.msl</c> package without modifying it.
/// </summary>
/// <remarks>
/// Inspection works on a package that fails some verification checks: it reports what it can read. It never
/// opens the package writable and never requires <c>-wal</c>/<c>-shm</c> sidecars.
/// </remarks>
public interface IMslPackageInspector
{
    /// <summary>Inspects a package.</summary>
    /// <param name="path">The <c>.msl</c> path.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The inspection projection.</returns>
    /// <exception cref="InvalidDataException">The package cannot be read or its required schema is missing.</exception>
    public Task<PackageInspection> InspectAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Builds the <c>package-summary-v1</c> canonical JSON projection.</summary>
    /// <param name="path">The <c>.msl</c> path.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The canonical summary document.</returns>
    /// <exception cref="InvalidDataException">The package or one of its payloads cannot be decoded.</exception>
    public Task<JsonDocument> BuildCanonicalSummaryAsync(string path, CancellationToken cancellationToken = default);
}
