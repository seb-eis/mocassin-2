// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Doctor;

/// <summary>
/// The result of probing the configured or discoverable solver.
/// </summary>
/// <param name="Configured">Whether a solver path was configured or found on <c>PATH</c>.</param>
/// <param name="Path">The resolved solver path, or <see langword="null"/>.</param>
/// <param name="Version">The reported version, or <see langword="null"/> when the probe failed.</param>
/// <param name="Message">The human-readable probe result.</param>
internal sealed record SolverProbeResult(
    bool Configured,
    string? Path,
    string? Version,
    string Message);

/// <summary>
/// The only process boundary in <c>doctor</c>, isolated so tests never launch a process.
/// </summary>
internal interface ISolverProbe
{
    /// <summary>
    /// Probes the solver.
    /// </summary>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The probe result.</returns>
    public Task<SolverProbeResult> ProbeAsync(CancellationToken cancellationToken);
}
