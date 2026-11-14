// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Caching;

/// <summary>
/// Reports stage-cache activity. Statistics are reported but never part of the model signature.
/// </summary>
/// <param name="Hits">The number of cache hits.</param>
/// <param name="Misses">The number of cache misses.</param>
/// <param name="Capacity">The configured cache capacity.</param>
public sealed record CompilationCacheStatistics(int Hits, int Misses, int Capacity)
{
    /// <summary>
    /// Gets the empty statistics used when caching is disabled or no stage consulted the cache.
    /// </summary>
    public static CompilationCacheStatistics Empty { get; } = new(0, 0, 0);
}
