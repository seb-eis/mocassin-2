// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Model;

namespace Mocassin.Core.Project;

/// <summary>
/// Immutable root value of one <c>.mocprj</c> project file.
/// </summary>
#pragma warning disable CA1724 // The type intentionally names the project domain and the namespace follows the plan.
public sealed record Project
{
#pragma warning restore CA1724
    /// <summary>
    /// Initializes a project value.
    /// </summary>
    /// <param name="schema">The optional JSON Schema URI.</param>
    /// <param name="schemaVersion">The project schema version.</param>
    /// <param name="metadata">The project metadata.</param>
    /// <param name="model">The scientific model.</param>
    /// <param name="parameterizations">The parameterization templates.</param>
    /// <param name="jobTemplates">The job templates.</param>
    /// <param name="buildTemplates">The build templates.</param>
    /// <param name="viewSettings">The persisted view state.</param>
    public Project(
        string? schema,
        int schemaVersion,
        ProjectMetadata metadata,
        ModelDefinition model,
        ImmutableArray<ParameterizationTemplate> parameterizations,
        ImmutableArray<JobTemplate> jobTemplates,
        ImmutableArray<BuildTemplate> buildTemplates,
        ViewSettings viewSettings)
        : this(
            schema,
            schemaVersion,
            metadata,
            model,
            parameterizations,
            jobTemplates,
            buildTemplates,
            viewSettings,
            symmetry: null)
    {
    }

    /// <summary>
    /// Initializes a project value with optional symmetry metadata.
    /// </summary>
    /// <param name="schema">The optional JSON Schema URI.</param>
    /// <param name="schemaVersion">The project schema version.</param>
    /// <param name="metadata">The project metadata.</param>
    /// <param name="model">The scientific model.</param>
    /// <param name="parameterizations">The parameterization templates.</param>
    /// <param name="jobTemplates">The job templates.</param>
    /// <param name="buildTemplates">The build templates.</param>
    /// <param name="viewSettings">The persisted view state.</param>
    /// <param name="symmetry">The optional symmetry metadata.</param>
    public Project(
        string? schema,
        int schemaVersion,
        ProjectMetadata metadata,
        ModelDefinition model,
        ImmutableArray<ParameterizationTemplate> parameterizations,
        ImmutableArray<JobTemplate> jobTemplates,
        ImmutableArray<BuildTemplate> buildTemplates,
        ViewSettings viewSettings,
        ProjectSymmetryMetadata? symmetry)
    {
        Schema = schema;
        SchemaVersion = schemaVersion;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Parameterizations = parameterizations.IsDefault ? [] : parameterizations;
        JobTemplates = jobTemplates.IsDefault ? [] : jobTemplates;
        BuildTemplates = buildTemplates.IsDefault ? [] : buildTemplates;
        ViewSettings = viewSettings ?? Mocassin.Core.Model.ViewSettings.Default;
        Symmetry = symmetry;
    }

    /// <summary>
    /// Gets the optional JSON Schema URI.
    /// </summary>
    public string? Schema { get; init; }

    /// <summary>
    /// Gets the project schema version.
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// Gets the project metadata.
    /// </summary>
    public ProjectMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the scientific model.
    /// </summary>
    public ModelDefinition Model { get; init; }

    /// <summary>
    /// Gets the optional symmetry metadata.
    /// </summary>
    public ProjectSymmetryMetadata? Symmetry { get; init; }

    /// <summary>
    /// Gets the parameterization templates.
    /// </summary>
    public ImmutableArray<ParameterizationTemplate> Parameterizations { get; init; }

    /// <summary>
    /// Gets the job templates.
    /// </summary>
    public ImmutableArray<JobTemplate> JobTemplates { get; init; }

    /// <summary>
    /// Gets the build templates.
    /// </summary>
    public ImmutableArray<BuildTemplate> BuildTemplates { get; init; }

    /// <summary>
    /// Gets the persisted view state.
    /// </summary>
    public ViewSettings ViewSettings { get; init; }

    /// <summary>
    /// Returns a copy with a new modified timestamp.
    /// </summary>
    /// <param name="modifiedUtc">The new UTC timestamp.</param>
    /// <returns>The updated project.</returns>
    public Project WithModifiedUtc(DateTimeOffset modifiedUtc) => this with
    {
        Metadata = Metadata.WithModifiedUtc(modifiedUtc),
    };
}
