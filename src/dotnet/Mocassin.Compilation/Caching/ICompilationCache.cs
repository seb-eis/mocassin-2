// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Caching;

/// <summary>
/// A caller-owned, thread-safe cache for immutable compilation stage results.
/// </summary>
/// <remarks>
/// The compiler never keeps a static or process-wide cache. When no cache is supplied it uses a bounded cache
/// for one <c>Compile</c> call and releases it on return. Entries are accepted only after the integrity checks
/// defined by the indexing and caching feature.
/// </remarks>
public interface ICompilationCache
{
    /// <summary>
    /// Tries to read a typed stage result.
    /// </summary>
    /// <typeparam name="T">The immutable stage-result type.</typeparam>
    /// <param name="key">The content-addressed cache key.</param>
    /// <param name="value">The cached value when found.</param>
    /// <returns><see langword="true"/> when a value was found.</returns>
    public bool TryGet<T>(string key, out T? value)
        where T : class;

    /// <summary>
    /// Returns a cached value or creates, stores, and returns a new one.
    /// </summary>
    /// <typeparam name="T">The immutable stage-result type.</typeparam>
    /// <param name="key">The content-addressed cache key.</param>
    /// <param name="factory">The factory used on a miss.</param>
    /// <returns>The cached or newly created value.</returns>
    public T GetOrAdd<T>(string key, Func<T> factory)
        where T : class;

    /// <summary>
    /// Gets a snapshot of the cache statistics.
    /// </summary>
    public CompilationCacheStatistics Statistics { get; }
}
