// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Options for one validation pipeline run.
/// </summary>
public sealed record ValidationOptions
{
    /// <summary>
    /// Gets a value indicating whether model statistics are computed.
    /// </summary>
    public bool IncludeStatistics { get; init; }

    /// <summary>
    /// Gets a value indicating whether buildability warnings are treated as errors for the validity result.
    /// </summary>
    public bool RequireBuildable { get; init; }

    /// <summary>
    /// Gets a value indicating whether every warning is treated as an error for the validity result.
    /// </summary>
    public bool WarningsAsErrors { get; init; }

    /// <summary>
    /// Gets a value indicating whether the additional <see cref="ValidationStage.Build"/> stages run.
    /// </summary>
    public bool RunBuildStage { get; init; }

    /// <summary>
    /// Gets the requested supercell count along a.
    /// </summary>
    public int CellCountA { get; init; } = 1;

    /// <summary>
    /// Gets the requested supercell count along b.
    /// </summary>
    public int CellCountB { get; init; } = 1;

    /// <summary>
    /// Gets the requested supercell count along c.
    /// </summary>
    public int CellCountC { get; init; } = 1;

    /// <summary>
    /// Gets the advisory maximum estimated compiled rule count.
    /// </summary>
    public long MaxEstimatedRuleCount { get; init; } = 1_000_000;

    /// <summary>
    /// Gets the advisory maximum estimated energy-table memory in bytes.
    /// </summary>
    public long MaxEstimatedMemoryBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Gets the default options for interactive and CLI validation.
    /// </summary>
    public static ValidationOptions Default { get; } = new();
}
