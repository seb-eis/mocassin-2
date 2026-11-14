// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace Mocassin.Compilation.Caching;

/// <summary>
/// A bounded, thread-safe, single-flight stage cache.
/// </summary>
internal sealed class StageCache : ICompilationCache
{
    private readonly int capacity;
    private readonly ConcurrentDictionary<string, Lazy<object>> entries = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> insertionOrder = new();
    private int hits;
    private int misses;

    /// <summary>
    /// Initializes a stage cache.
    /// </summary>
    /// <param name="capacity">The maximum number of entries.</param>
    public StageCache(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        this.capacity = capacity;
    }

    /// <inheritdoc />
    public CompilationCacheStatistics Statistics =>
        new(Volatile.Read(ref hits), Volatile.Read(ref misses), capacity);

    /// <inheritdoc />
    public bool TryGet<T>(string key, out T? value)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (entries.TryGetValue(key, out var lazy) && lazy.IsValueCreated)
        {
            value = (T)lazy.Value;
            Interlocked.Increment(ref hits);
            return true;
        }

        value = null;
        Interlocked.Increment(ref misses);
        return false;
    }

    /// <inheritdoc />
    public T GetOrAdd<T>(string key, Func<T> factory)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);
        var added = false;
        var lazy = entries.GetOrAdd(key, _ =>
        {
            added = true;
            return new Lazy<object>(() => factory(), LazyThreadSafetyMode.ExecutionAndPublication);
        });
        if (added)
        {
            Interlocked.Increment(ref misses);
            insertionOrder.Enqueue(key);
            Trim();
        }
        else
        {
            Interlocked.Increment(ref hits);
        }

        return (T)lazy.Value;
    }

    private void Trim()
    {
        while (entries.Count > capacity && insertionOrder.TryDequeue(out var key))
        {
            entries.TryRemove(key, out _);
        }
    }
}
