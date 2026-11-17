// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Identifies one embedded project starter.
/// </summary>
public enum ProjectTemplateKind
{
    /// <summary>An empty, valid project with no model content.</summary>
    Blank = 0,

    /// <summary>The ceria model-definition starter, without a job template.</summary>
    Ceria = 1,
}

/// <summary>
/// One embedded, immutable project starter.
/// </summary>
/// <remarks>
/// The template project is a read-only asset. <see cref="CreateProject"/> returns a copy with a fresh UUIDv7
/// identity, a fresh master seed, the supplied name, and fresh timestamps; the scientific model is unchanged.
/// The CLI and Step 23's creation UI use the same factory, so there is no second starter definition.
/// </remarks>
/// <param name="Kind">The starter kind.</param>
/// <param name="DisplayName">The user-visible starter name.</param>
/// <param name="Project">The embedded template project.</param>
public sealed record ProjectTemplate(ProjectTemplateKind Kind, string DisplayName, Project Project)
{
    /// <summary>
    /// Creates a fresh project from this template.
    /// </summary>
    /// <param name="name">The new project display name.</param>
    /// <param name="createdUtc">The creation timestamp; it also seeds the UUIDv7 identity.</param>
    /// <returns>The fresh project value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public Project CreateProject(string name, DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(name);
        var timestamp = createdUtc.ToUniversalTime();
        return Project with
        {
            Metadata = Project.Metadata with
            {
                ProjectId = Uuid7.NewGuid(timestamp),
                MasterSeed = ProjectMetadata.CreateMasterSeed(),
                Name = name.Trim(),
                CreatedUtc = timestamp,
                ModifiedUtc = timestamp,
            },
        };
    }
}
