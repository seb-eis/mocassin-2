// SPDX-License-Identifier: MIT

using Mocassin.Core.Editing;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// Compares a recovery snapshot with the saved file without hashing in the ViewModels layer.
/// </summary>
internal static class RecoveryComparison
{
    private static readonly DateTimeOffset normalizedSentinel = DateTimeOffset.UnixEpoch;

    /// <summary>
    /// Determines whether two project values differ in content, ignoring only the modified timestamp.
    /// </summary>
    /// <param name="snapshot">The candidate snapshot.</param>
    /// <param name="savedFile">The saved project value.</param>
    /// <returns><see langword="true"/> when content differs.</returns>
    public static bool HasContentDifference(ProjectModel snapshot, ProjectModel savedFile)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(savedFile);
        var normalizedSnapshot = snapshot with
        {
            Metadata = snapshot.Metadata with { ModifiedUtc = normalizedSentinel },
        };
        var normalizedSaved = savedFile with
        {
            Metadata = savedFile.Metadata with { ModifiedUtc = normalizedSentinel },
        };
        return ProjectDocument.CreateRecovered(normalizedSnapshot, normalizedSaved).IsDirty;
    }
}
