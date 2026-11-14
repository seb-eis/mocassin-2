// SPDX-License-Identifier: MIT

using Mocassin.Core.Units;

namespace Mocassin.Packages.Parameterization;

/// <summary>
/// Controls an apply operation, its completeness diagnostics, and its resource bounds.
/// </summary>
/// <remarks>
/// The defaults match the Step 09 design: completeness warnings are enabled, the plausible frequency band is
/// <c>1e-3..1e18 Hz</c>, and the dense applied values may use at most 2 GiB. The implicit default template
/// suppresses completeness warnings regardless of <see cref="EmitCompletenessWarnings"/> because there is no
/// authored template to warn about.
/// </remarks>
public sealed record ParameterizationOptions
{
    /// <summary>
    /// Gets a value indicating whether completeness warnings are emitted for an explicit template.
    /// </summary>
    public bool EmitCompletenessWarnings { get; init; } = true;

    /// <summary>
    /// Gets the quality thresholds used by completeness and plausibility checks.
    /// </summary>
    public ParameterizationQualityOptions Quality { get; init; } = new();

    /// <summary>
    /// Gets the maximum size in bytes of the dense applied-value arrays.
    /// </summary>
    public long MaxAppliedValueBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static ParameterizationOptions Default { get; } = new();
}

/// <summary>
/// Defines the plausibility and uniformity thresholds used by parameterization quality diagnostics.
/// </summary>
public sealed record ParameterizationQualityOptions
{
    /// <summary>
    /// Gets the smallest plausibly entered attempt frequency.
    /// </summary>
    public FrequencyHz MinimumPlausibleFrequency { get; init; } = new(1e-3);

    /// <summary>
    /// Gets the largest plausibly entered attempt frequency.
    /// </summary>
    public FrequencyHz MaximumPlausibleFrequency { get; init; } = new(1e18);

    /// <summary>
    /// Gets the minimum number of explicit pair or group values before the identical-value share is checked.
    /// </summary>
    public int IdenticalValueMinimumCount { get; init; } = 20;

    /// <summary>
    /// Gets the identical-value share, in <c>0..1</c>, that is treated as suspiciously uniform.
    /// </summary>
    public double IdenticalValueShareThreshold { get; init; } = 0.90;

    /// <summary>
    /// Gets the default quality options.
    /// </summary>
    public static ParameterizationQualityOptions Default { get; } = new();
}
