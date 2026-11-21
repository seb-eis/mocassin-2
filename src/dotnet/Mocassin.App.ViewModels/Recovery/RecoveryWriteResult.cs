// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// Result of writing one recovery snapshot and manifest.
/// </summary>
/// <param name="IsSuccess">Whether both files were written.</param>
/// <param name="SnapshotPath">The snapshot path on success.</param>
/// <param name="Diagnostics">The diagnostics on failure.</param>
internal sealed record RecoveryWriteResult(
    bool IsSuccess,
    string? SnapshotPath,
    ImmutableArray<Diagnostic> Diagnostics);
