// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// One recoverable snapshot for a project.
/// </summary>
/// <param name="ProjectId">The project identity.</param>
/// <param name="ProjectPath">The main project path.</param>
/// <param name="SnapshotPath">The snapshot file path.</param>
/// <param name="SavedUtc">The UTC time the snapshot was written.</param>
/// <param name="Source">The snapshot source.</param>
internal sealed record RecoveryCandidate(
    Guid ProjectId,
    string ProjectPath,
    string SnapshotPath,
    DateTimeOffset SavedUtc,
    RecoverySource Source);
