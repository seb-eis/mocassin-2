// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects.Templates;

/// <summary>
/// Loads the embedded project starters and creates fresh projects from them.
/// </summary>
/// <remarks>
/// The assets are embedded <c>.mocprj</c> resources, so creation needs no file system, network, or template
/// directory. The ceria starter is the model-definition sample without a job template; a freshness test asserts
/// that its canonical model hash equals <c>samples/ceria/ceria.mocprj</c>.
/// </remarks>
public static class ProjectTemplateFactory
{
    private const string resourcePrefix = "Mocassin.Packages.Projects.Templates.Assets.";

    private static readonly Lazy<ImmutableArray<ProjectTemplate>> templates = new(LoadAll);

    /// <summary>
    /// Gets every embedded starter, ordered by <see cref="ProjectTemplateKind"/>.
    /// </summary>
    public static ImmutableArray<ProjectTemplate> All => templates.Value;

    /// <summary>
    /// Gets one embedded starter.
    /// </summary>
    /// <param name="kind">The starter kind.</param>
    /// <returns>The template.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not defined.</exception>
    public static ProjectTemplate Get(ProjectTemplateKind kind) =>
        All.FirstOrDefault(template => template.Kind == kind)
        ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown project template kind.");

    /// <summary>
    /// Creates a fresh project from an embedded starter.
    /// </summary>
    /// <param name="kind">The starter kind.</param>
    /// <param name="name">The new project display name.</param>
    /// <param name="createdUtc">The creation timestamp.</param>
    /// <returns>The fresh project value.</returns>
    public static Project CreateProject(ProjectTemplateKind kind, string name, DateTimeOffset createdUtc) =>
        Get(kind).CreateProject(name, createdUtc);

    private static ImmutableArray<ProjectTemplate> LoadAll() =>
    [
        Load(ProjectTemplateKind.Blank, "blank.mocprj", "Blank"),
        Load(ProjectTemplateKind.Ceria, "ceria.mocprj", "Ceria model starter"),
    ];

    private static ProjectTemplate Load(ProjectTemplateKind kind, string fileName, string displayName)
    {
        var assembly = typeof(ProjectTemplateFactory).Assembly;
        var resourceName = resourcePrefix + fileName;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded project template '{resourceName}' is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var project = ProjectJson.Deserialize(reader.ReadToEnd());
        return new ProjectTemplate(kind, displayName, project);
    }
}
