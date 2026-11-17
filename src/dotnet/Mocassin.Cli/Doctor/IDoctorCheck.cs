// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Doctor;

/// <summary>
/// One independent environment check used by <c>mocassin doctor</c>.
/// </summary>
/// <remarks>
/// Tests inject a check list to exercise healthy, warning-only, and failing reports without spoofing the
/// operating system. The production command uses the seven real checks.
/// </remarks>
internal interface IDoctorCheck
{
    /// <summary>Gets the stable check name.</summary>
    public string Name { get; }

    /// <summary>
    /// Runs the check.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The check result.</returns>
    public Task<DoctorCheckResult> RunAsync(CancellationToken cancellationToken);
}
