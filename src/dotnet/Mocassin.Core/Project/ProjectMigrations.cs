// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Core.Project;

/// <summary>
/// A pure, ordered transformation of one project schema version to the next.
/// </summary>
public interface IProjectMigration
{
    /// <summary>
    /// Gets the schema version this migration reads.
    /// </summary>
    public int FromVersion { get; }

    /// <summary>
    /// Gets the schema version this migration writes.
    /// </summary>
    public int ToVersion { get; }

    /// <summary>
    /// Applies the migration to a mutable JSON document.
    /// </summary>
    /// <param name="document">The project JSON document.</param>
    /// <returns>The migrated document.</returns>
    public JsonNode Apply(JsonNode document);
}

/// <summary>
/// Result of running the project migration pipeline.
/// </summary>
/// <param name="Document">The migrated JSON document.</param>
/// <param name="Version">The resulting schema version.</param>
/// <param name="Diagnostics">Diagnostics produced by the pipeline.</param>
public sealed record ProjectMigrationResult(
    JsonNode Document,
    int Version,
    ImmutableArray<Diagnostic> Diagnostics);

/// <summary>
/// Applies project migrations in order until the current schema version is reached.
/// </summary>
public static class ProjectMigrations
{
    /// <summary>
    /// Gets the current project schema version.
    /// </summary>
    public const int CurrentVersion = ProjectSchema.CurrentVersion;

    /// <summary>
    /// Gets the registered migrations. The initial v1 schema has no predecessor, so the list is empty.
    /// </summary>
    public static ImmutableArray<IProjectMigration> All { get; } = [];

    /// <summary>
    /// Applies the migration chain from an explicit source version.
    /// </summary>
    /// <param name="document">The project JSON document.</param>
    /// <param name="fromVersion">The schema version declared by the document.</param>
    /// <returns>The migration result.</returns>
    public static ProjectMigrationResult Apply(JsonNode document, int fromVersion)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (fromVersion > CurrentVersion)
        {
            return new ProjectMigrationResult(
                document,
                fromVersion,
                [
                    new Diagnostic(
                        ProjectDiagnosticCodes.FutureSchemaVersion,
                        DiagnosticSeverity.Error,
                        $"Project schema version {fromVersion} is newer than the supported version {CurrentVersion}. " +
                        "Open the project with a newer MOCASSIN release.",
                        new ModelPath("project", Guid.Empty, "schemaVersion")),
                ]);
        }

        var current = document;
        var version = fromVersion;
        while (version < CurrentVersion)
        {
            var migration = All.FirstOrDefault(candidate => candidate.FromVersion == version);
            if (migration is null)
            {
                return new ProjectMigrationResult(
                    current,
                    version,
                    [
                        new Diagnostic(
                            ProjectDiagnosticCodes.InvalidDocument,
                            DiagnosticSeverity.Error,
                            $"No project migration is registered from schema version {version}.",
                            new ModelPath("project", Guid.Empty, "schemaVersion")),
                    ]);
            }

            current = migration.Apply(current);
            version = migration.ToVersion;
        }

        return new ProjectMigrationResult(current, version, []);
    }
}
