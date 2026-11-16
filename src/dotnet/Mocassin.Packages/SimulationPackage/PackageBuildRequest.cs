// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// The deterministic build-time description of one simulation package.
/// </summary>
/// <remarks>
/// <see cref="CreatedUtc"/> is supplied by the caller and never read from the clock, so a rebuild can be
/// compared deterministically and the canonical package summary can normalize it.
/// </remarks>
/// <param name="TargetPath">The final <c>.msl</c> path. The writer builds in a temp file beside it.</param>
/// <param name="ProjectId">The authored project identifier.</param>
/// <param name="JobSelection">The non-empty job-selection expression, for example <c>all</c> or <c>1-100,250</c>.</param>
/// <param name="CreatedUtc">The build timestamp written to <c>schema_info.created_utc</c>.</param>
public sealed record PackageBuildRequest(
    string TargetPath,
    Guid ProjectId,
    string JobSelection,
    DateTimeOffset CreatedUtc);
