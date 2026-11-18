// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Doctor;

/// <summary>
/// The severity of one doctor check.
/// </summary>
internal enum DoctorCheckStatus
{
    /// <summary>The check passed.</summary>
    Ok = 0,

    /// <summary>The check found a non-failing issue, for example a missing optional solver.</summary>
    Warn = 1,

    /// <summary>The check failed and makes the environment unhealthy.</summary>
    Fail = 2,
}

/// <summary>
/// The outcome of one doctor check.
/// </summary>
/// <param name="Name">The stable check name.</param>
/// <param name="Status">The check status.</param>
/// <param name="Message">The short human-readable result.</param>
/// <param name="Detail">Optional detail, for example a version, path, or exception type.</param>
internal sealed record DoctorCheckResult(
    string Name,
    DoctorCheckStatus Status,
    string Message,
    string? Detail = null);
