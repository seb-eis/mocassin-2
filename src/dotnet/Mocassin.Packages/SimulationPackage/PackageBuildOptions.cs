// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// Options that control the simulation-package writer.
/// </summary>
public sealed record PackageBuildOptions
{
    /// <summary>
    /// Gets whether an existing package at the target path may be replaced.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// Gets the Zstd compression level used by the payload encoders (1–19).
    /// </summary>
    public int CompressionLevel { get; init; } = 3;

    /// <summary>
    /// Gets whether the writer runs <c>VACUUM</c> before publication.
    /// </summary>
    public bool Vacuum { get; init; }

    /// <summary>
    /// Gets the number of job rows per committed transaction.
    /// </summary>
    public int BatchSize { get; init; } = 500;

    /// <summary>
    /// Gets the advisory package-size limit in bytes; a larger package produces a <c>PKG009</c> warning.
    /// </summary>
    public long AdvisorySizeBytes { get; init; } = 2L * 1024 * 1024 * 1024;
}
