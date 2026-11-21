// SPDX-License-Identifier: MIT

namespace Mocassin.App.Recovery;

/// <summary>
/// The application-local recovery manifest stored next to each snapshot.
/// </summary>
/// <param name="Format">The manifest format marker.</param>
/// <param name="Version">The manifest schema version.</param>
/// <param name="ProjectId">The project identity.</param>
/// <param name="ProjectPath">The main project path.</param>
/// <param name="SavedUtc">The UTC snapshot time.</param>
/// <param name="Source">The recovery source name.</param>
internal sealed record RecoveryManifest(
    string Format,
    int Version,
    Guid ProjectId,
    string ProjectPath,
    DateTimeOffset SavedUtc,
    string Source);
