// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.Build;

/// <summary>
/// Computes the display-only ETA from a rolling window of progress samples.
/// </summary>
/// <remarks>
/// The window keeps at least the last eight and at most the last 64 samples. The estimate never enters a
/// report field that affects reproducibility; only the advisory JSON-Lines <c>etaS</c> field and the human
/// progress line use it.
/// </remarks>
internal sealed class BuildEtaEstimator
{
    private const int maximumSamples = 64;
    private const int minimumSamples = 8;

    private readonly List<Sample> samples = [];
    private double? lastEstimate;

    /// <summary>
    /// Adds a progress sample and returns the current estimate.
    /// </summary>
    /// <param name="elapsed">The elapsed stage time.</param>
    /// <param name="completed">The completed job count.</param>
    /// <param name="total">The selected job count.</param>
    /// <returns>The estimated remaining seconds, or <see langword="null"/> until enough samples exist.</returns>
    public double? Observe(TimeSpan elapsed, int completed, int total)
    {
        if (total <= 0 || completed <= 0)
        {
            return null;
        }

        if (completed >= total)
        {
            lastEstimate = 0;
            return 0;
        }

        if (samples.Count > 0 && completed <= samples[^1].Completed)
        {
            return lastEstimate;
        }

        samples.Add(new Sample(elapsed, completed));
        if (samples.Count > maximumSamples)
        {
            samples.RemoveAt(0);
        }

        if (samples.Count < minimumSamples)
        {
            return null;
        }

        var first = samples[0];
        var last = samples[^1];
        var jobs = last.Completed - first.Completed;
        if (jobs <= 0)
        {
            return lastEstimate;
        }

        var secondsPerJob = (last.Elapsed - first.Elapsed).TotalSeconds / jobs;
        lastEstimate = Math.Max(0, (total - completed) * secondsPerJob);
        return lastEstimate;
    }

    private sealed record Sample(TimeSpan Elapsed, int Completed);
}
