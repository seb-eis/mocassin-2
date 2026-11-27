// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Projects;
using Mocassin.Packages.Projects;

namespace Mocassin.App.Tests.TestSupport;

/// <summary>
/// Loads real project fixtures through the production store.
/// </summary>
internal static class ProjectFixtures
{
    /// <summary>
    /// Loads the ceria sample into a workspace entry.
    /// </summary>
    /// <param name="projectPath">An optional real project path used by deterministic screenshot captures.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The real loaded project entry.</returns>
    public static async Task<ProjectWorkspaceEntry> LoadCeriaAsync(
        string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = projectPath ?? RepositoryPaths.Combine("samples", "ceria", "ceria.mocprj");
        var store = new ProjectStore(
            new ManualTimeProvider(),
            new ProjectStoreOptions { ValidateOnLoad = false });
        var result = await store.LoadAsync(path, cancellationToken);
        if (result.Project is null)
        {
            throw new InvalidOperationException($"The ceria fixture did not load: {result.FailureKind}.");
        }

        return new ProjectWorkspaceEntry(
            result.Project,
            path,
            new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero),
            isReadOnly: false);
    }
}
