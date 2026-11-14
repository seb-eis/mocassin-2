// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Caching;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Summarizes one compilation run for diagnostics, tests, the GUI, and the CLI.
/// </summary>
/// <remarks>
/// The report is evidence, not part of the model signature. Timings, cache statistics, and the human-readable
/// summary never affect the compiled geometry, rules, encoding, or signature.
/// </remarks>
public sealed record CompilationReport
{
    /// <summary>
    /// Initializes a compilation report.
    /// </summary>
    /// <param name="compilerVersion">The stable compiler implementation identity.</param>
    /// <param name="modelSignature">The model signature, or <see langword="null"/> when signing did not run.</param>
    /// <param name="symmetryContractVersion">The symmetry contract version.</param>
    /// <param name="spaceGroupDataVersion">The catalog data version, or <see langword="null"/> before resolution.</param>
    /// <param name="spaceGroupDataSha256">The catalog SHA-256, or <see langword="null"/> before resolution.</param>
    /// <param name="stages">The ordered stage results.</param>
    /// <param name="cache">The stage-cache statistics snapshot.</param>
    /// <param name="summary">The human-readable run summary.</param>
    /// <exception cref="ArgumentException">A required string is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="cache"/> is <see langword="null"/>.</exception>
    public CompilationReport(
        string compilerVersion,
        string? modelSignature,
        string symmetryContractVersion,
        string? spaceGroupDataVersion,
        string? spaceGroupDataSha256,
        ImmutableArray<CompilationStageResult> stages,
        CompilationCacheStatistics cache,
        string summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compilerVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(symmetryContractVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        ArgumentNullException.ThrowIfNull(cache);

        CompilerVersion = compilerVersion;
        ModelSignature = modelSignature;
        SymmetryContractVersion = symmetryContractVersion;
        SpaceGroupDataVersion = spaceGroupDataVersion;
        SpaceGroupDataSha256 = spaceGroupDataSha256;
        Stages = stages.IsDefault ? ImmutableArray<CompilationStageResult>.Empty : stages;
        Cache = cache;
        Summary = summary;
    }

    /// <summary>
    /// Gets the stable compiler implementation identity.
    /// </summary>
    public string CompilerVersion { get; }

    /// <summary>
    /// Gets the model signature, or <see langword="null"/> when signing did not run.
    /// </summary>
    public string? ModelSignature { get; }

    /// <summary>
    /// Gets the symmetry contract version.
    /// </summary>
    public string SymmetryContractVersion { get; }

    /// <summary>
    /// Gets the catalog data version, or <see langword="null"/> before resolution.
    /// </summary>
    public string? SpaceGroupDataVersion { get; }

    /// <summary>
    /// Gets the catalog SHA-256, or <see langword="null"/> before resolution.
    /// </summary>
    public string? SpaceGroupDataSha256 { get; }

    /// <summary>
    /// Gets the ordered stage results, including skipped stages and the terminal report stage.
    /// </summary>
    public ImmutableArray<CompilationStageResult> Stages { get; }

    /// <summary>
    /// Gets the stage-cache statistics snapshot.
    /// </summary>
    public CompilationCacheStatistics Cache { get; }

    /// <summary>
    /// Gets the authoritative compiled-content counts.
    /// </summary>
    public CompilationCounts Counts { get; init; } = CompilationCounts.Empty;

    /// <summary>
    /// Gets the compilation input hashes.
    /// </summary>
    public CompilationInputHashes InputHashes { get; init; } = CompilationInputHashes.Empty;

    /// <summary>
    /// Gets the human-readable run summary.
    /// </summary>
    public string Summary { get; }
}
