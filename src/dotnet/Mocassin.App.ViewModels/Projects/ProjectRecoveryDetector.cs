// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Recovery;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Detects the newest content-different sidecar or manifest recovery candidate for one session.
/// </summary>
internal sealed class ProjectRecoveryDetector
{
    private readonly ProjectStore store;
    private readonly IRecoveryStore recovery;
    private readonly IProjectFileProbe probe;
    private readonly TimeProvider timeProvider;

    /// <summary>
    /// Initializes the detector.
    /// </summary>
    /// <param name="store">The project store.</param>
    /// <param name="recovery">The recovery store.</param>
    /// <param name="probe">The project file probe.</param>
    /// <param name="timeProvider">The clock.</param>
    public ProjectRecoveryDetector(
        ProjectStore store,
        IRecoveryStore recovery,
        IProjectFileProbe probe,
        TimeProvider timeProvider)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        this.probe = probe ?? throw new ArgumentNullException(nameof(probe));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Detects the newest valid candidate for a loaded session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="savedProject">The saved file value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newest candidate, or <see langword="null"/>.</returns>
    public async Task<RecoveryCandidate?> DetectAsync(
        ProjectSession session,
        ProjectModel savedProject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(savedProject);
        RecoveryCandidate? sidecar = null;
        var sidecarPath = ProjectStore.GetAutosavePath(session.Path);
        if (probe.GetStamp(sidecarPath).Exists)
        {
            var sidecarLoad = await store.LoadAutosaveAsync(session.Path, cancellationToken).ConfigureAwait(false);
            if (sidecarLoad.Project is { } sidecarProject
                && sidecarProject.Metadata.ProjectId == session.Id
                && RecoveryComparison.HasContentDifference(sidecarProject, savedProject))
            {
                sidecar = new RecoveryCandidate(
                    session.Id,
                    session.Path,
                    sidecarPath,
                    probe.GetStamp(sidecarPath).LastWriteUtc,
                    RecoverySource.Sidecar);
            }
        }

        var now = timeProvider.GetUtcNow();
        var candidates = new List<RecoveryCandidate>();
        foreach (var candidate in await recovery.ListAsync(cancellationToken).ConfigureAwait(false))
        {
            if (candidate.ProjectId != session.Id
                || RecoveryRetentionPolicy.IsExpired(candidate.SavedUtc, now))
            {
                continue;
            }

            var snapshot = await store.LoadAsync(candidate.SnapshotPath, cancellationToken).ConfigureAwait(false);
            if (snapshot.Project is null
                || snapshot.Project.Metadata.ProjectId != session.Id
                || !RecoveryComparison.HasContentDifference(snapshot.Project, savedProject))
            {
                continue;
            }

            candidates.Add(candidate);
        }

        if (sidecar is not null)
        {
            candidates.Add(sidecar);
        }

        return candidates.OrderByDescending(candidate => candidate.SavedUtc).FirstOrDefault();
    }
}
