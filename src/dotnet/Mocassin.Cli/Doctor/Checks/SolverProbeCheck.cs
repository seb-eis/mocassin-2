// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Doctor.Checks;

/// <summary>
/// Checks the optional solver through the <see cref="ISolverProbe"/> seam.
/// </summary>
/// <remarks>
/// A missing solver is a warning until the solver ships; a configured or discovered solver that cannot report
/// its version is a failure.
/// </remarks>
internal sealed class SolverProbeCheck : IDoctorCheck
{
    /// <summary>The stable check name.</summary>
    public const string CheckName = "solver";

    private readonly ISolverProbe probe;

    /// <summary>
    /// Initializes the check.
    /// </summary>
    /// <param name="probe">The solver probe.</param>
    public SolverProbeCheck(ISolverProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        this.probe = probe;
    }

    /// <inheritdoc />
    public string Name => CheckName;

    /// <inheritdoc />
    public async Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken)
    {
        var result = await probe.ProbeAsync(cancellationToken).ConfigureAwait(false);
        if (!result.Configured)
        {
            return new DoctorCheckResult(Name, DoctorCheckStatus.Warn, result.Message);
        }

        return result.Version is null
            ? new DoctorCheckResult(Name, DoctorCheckStatus.Fail, result.Message, result.Path)
            : new DoctorCheckResult(Name, DoctorCheckStatus.Ok, result.Message, result.Path);
    }
}
