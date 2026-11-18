// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Packages.SimulationPackage;

namespace Mocassin.Packages.Build;

/// <summary>
/// The report identities of one build run.
/// </summary>
/// <remarks>
/// Each value stays <see langword="null"/> until its stage has run, so an early failure or cancellation still
/// produces a complete report. The CLI and Step 26 never recompute a hash or signature.
/// </remarks>
/// <param name="ProjectId">The authored project identity.</param>
/// <param name="ModelHash">The Step 01 canonical model hash.</param>
/// <param name="DocumentHash">The Step 01 canonical document hash.</param>
/// <param name="ModelSignature">The Step 08 compiled-model signature.</param>
/// <param name="ParameterizationSignature">The Step 09 parameterization signature.</param>
public sealed record BuildIdentity(
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    string? ModelSignature,
    string? ParameterizationSignature)
{
    /// <summary>Gets an identity whose values are all unknown.</summary>
    public static BuildIdentity Empty { get; } = new(null, null, null, null, null);
}

/// <summary>
/// The wall time of one completed stage.
/// </summary>
/// <param name="Stage">The stage.</param>
/// <param name="Elapsed">The stage wall time.</param>
public sealed record BuildStageTiming(BuildStage Stage, TimeSpan Elapsed);

/// <summary>
/// The authoritative outcome of one build run.
/// </summary>
/// <remarks>
/// <see cref="Diagnostics"/> is deduplicated, deterministically sorted, and authoritative. A preview run carries
/// <see cref="Preview"/>; a full build carries <see cref="Package"/> after publication. <see cref="Timings"/>
/// lists pipeline stages only; the preview timing is <see cref="BuildPreview.Elapsed"/> and the post-publication
/// verification timing is caller-composed.
/// </remarks>
/// <param name="Status">The overall status.</param>
/// <param name="Failure">The documented failure kind.</param>
/// <param name="Identity">The report identities reached by the run.</param>
/// <param name="Diagnostics">The deduplicated diagnostics.</param>
/// <param name="Selection">The effective selection, or <see langword="null"/> before selection.</param>
/// <param name="JobSelection">The normalized job selection, or <see langword="null"/> before expansion.</param>
/// <param name="Preview">The typed preview payload in preview mode, otherwise <see langword="null"/>.</param>
/// <param name="Package">The published package, or <see langword="null"/>.</param>
/// <param name="Timings">The completed pipeline stages in order.</param>
/// <param name="Elapsed">The total wall time.</param>
public sealed record BuildResult(
    BuildStatus Status,
    BuildFailureKind Failure,
    BuildIdentity Identity,
    ImmutableArray<Diagnostic> Diagnostics,
    BuildSelection? Selection,
    JobSelectionResult? JobSelection,
    BuildPreview? Preview,
    PackageWriteResult? Package,
    ImmutableArray<BuildStageTiming> Timings,
    TimeSpan Elapsed);
