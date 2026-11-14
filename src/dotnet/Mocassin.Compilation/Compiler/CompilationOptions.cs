// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Caching;

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Controls one model compilation run.
/// </summary>
/// <remarks>
/// The limits are compilation guardrails, not physics constants. Hard limits fail with <c>CMP006</c> before an
/// allocation; advisory limits produce a <c>CMP010</c> warning and do not stop the pipeline. Step 09 and
/// Step 11 may tune the limits through the parameterization and job layers.
/// </remarks>
public sealed record CompilationOptions
{
    /// <summary>
    /// Gets the maximum degree of parallelism. A value of <c>1</c> disables parallelism.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets a value indicating whether stage results are cached for the duration of the run. When
    /// <see langword="false"/>, <see cref="Cache"/> is ignored.
    /// </summary>
    public bool EnableStageCache { get; init; } = true;

    /// <summary>
    /// Gets the maximum number of entries in the per-run stage cache.
    /// </summary>
    public int StageCacheCapacity { get; init; } = 256;

    /// <summary>
    /// Gets the hard limit for generated rules.
    /// </summary>
    public long MaxRuleCount { get; init; } = 1_000_000;

    /// <summary>
    /// Gets the advisory limit for generated rules. Exceeding it emits <c>CMP010</c> but does not fail.
    /// </summary>
    public long AdvisoryRuleCount { get; init; } = 100_000;

    /// <summary>
    /// Gets the hard limit for the estimated energy-table size in bytes.
    /// </summary>
    public long MaxEnergyTableBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Gets the advisory limit for the estimated energy-table size in bytes. Exceeding it emits <c>CMP010</c>
    /// but does not fail.
    /// </summary>
    public long AdvisoryEnergyTableBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>
    /// Gets the hard limit for enumerated cluster tuples. A lower value is enforced by a pre-check; the value
    /// cannot raise the symmetry reducer's fixed ceiling.
    /// </summary>
    public int MaxClusterTupleCount { get; init; } = 1_000_000;

    /// <summary>
    /// Gets a value indicating whether transitions that no simulation references emit a <c>CMP009</c> warning
    /// when they are not compiled.
    /// </summary>
    public bool EmitUnusedTransitionWarnings { get; init; } = true;

    /// <summary>
    /// Gets an optional caller-owned, thread-safe stage cache. <see langword="null"/> uses the per-run cache.
    /// </summary>
    public ICompilationCache? Cache { get; init; }

    /// <summary>
    /// Gets the default compilation options.
    /// </summary>
    public static CompilationOptions Default { get; } = new();

    /// <summary>
    /// Validates caller-supplied options and returns the effective value.
    /// </summary>
    /// <param name="options">The optional caller value.</param>
    /// <returns>The options to use for the run.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A documented option range is violated.</exception>
    internal static CompilationOptions Validate(CompilationOptions? options)
    {
        var effective = options ?? Default;

        if (effective.MaxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.MaxDegreeOfParallelism,
                "MaxDegreeOfParallelism must be at least 1.");
        }

        if (effective.StageCacheCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.StageCacheCapacity,
                "StageCacheCapacity must be at least 1.");
        }

        if (effective.MaxRuleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.MaxRuleCount,
                "MaxRuleCount must not be negative.");
        }

        if (effective.AdvisoryRuleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.AdvisoryRuleCount,
                "AdvisoryRuleCount must not be negative.");
        }

        if (effective.MaxEnergyTableBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.MaxEnergyTableBytes,
                "MaxEnergyTableBytes must not be negative.");
        }

        if (effective.AdvisoryEnergyTableBytes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.AdvisoryEnergyTableBytes,
                "AdvisoryEnergyTableBytes must not be negative.");
        }

        if (effective.MaxClusterTupleCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                effective.MaxClusterTupleCount,
                "MaxClusterTupleCount must not be negative.");
        }

        return effective;
    }
}
