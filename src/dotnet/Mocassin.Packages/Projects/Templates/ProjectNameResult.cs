// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Result of normalizing a project name.
/// </summary>
/// <param name="IsSuccess">Whether the name is valid.</param>
/// <param name="Name">The normalized name, or the trimmed input on failure.</param>
/// <param name="Failure">The failure kind.</param>
/// <param name="Message">The user-facing message on failure.</param>
public readonly record struct ProjectNameResult(
    bool IsSuccess,
    string Name,
    ProjectPathFailure Failure,
    string? Message);
