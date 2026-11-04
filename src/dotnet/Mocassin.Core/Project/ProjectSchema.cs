// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Project;

/// <summary>
/// Schema constants for the versioned project JSON document.
/// </summary>
public static class ProjectSchema
{
    /// <summary>
    /// The current project schema version written by this implementation.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// The canonical JSON Schema URI embedded in new project files.
    /// </summary>
    public const string SchemaUri = "https://mocassin.dev/schemas/project-v1.json";
}
