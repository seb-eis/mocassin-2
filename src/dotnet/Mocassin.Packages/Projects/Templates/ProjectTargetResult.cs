// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Result of validating a project target path.
/// </summary>
/// <param name="IsSuccess">Whether the target is valid.</param>
/// <param name="FullPath">The full target path.</param>
/// <param name="Failure">The failure kind.</param>
/// <param name="Message">The user-facing message on failure.</param>
public readonly record struct ProjectTargetResult(
    bool IsSuccess,
    string FullPath,
    ProjectPathFailure Failure,
    string? Message);
