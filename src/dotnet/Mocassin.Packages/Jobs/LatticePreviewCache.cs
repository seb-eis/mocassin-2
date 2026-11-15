// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// One cached lattice preview with its diagnostics and estimated payload size.
/// </summary>
/// <param name="Key">The cache key that produced the entry.</param>
/// <param name="Preview">The cached preview.</param>
/// <param name="Diagnostics">The diagnostics produced by the preview.</param>
/// <param name="EstimatedBytes">The estimated payload of the preview in bytes.</param>
internal sealed record LatticePreviewCacheEntry(
    string Key,
    LatticePreview Preview,
    ImmutableArray<Diagnostic> Diagnostics,
    long EstimatedBytes);

/// <summary>
/// A bounded, thread-safe LRU cache for lattice previews.
/// </summary>
/// <remarks>
/// The cache holds at most <see cref="DefaultEntryCapacity"/> entries and <see cref="DefaultByteCapacity"/>
/// estimated payload bytes. Entries are evicted least-recently-used first. A preview that alone exceeds the byte
/// budget is not cached, but is still returned to the caller. The cache is never static; it is released with the
/// owning preview service.
/// </remarks>
internal sealed class LatticePreviewCache
{
    /// <summary>
    /// The default maximum number of cached previews.
    /// </summary>
    internal const int DefaultEntryCapacity = 8;

    /// <summary>
    /// The default maximum estimated cached payload in bytes (256 MiB).
    /// </summary>
    internal const long DefaultByteCapacity = 256L * 1024 * 1024;

    private readonly Dictionary<string, LinkedListNode<LatticePreviewCacheEntry>> entries =
        new(StringComparer.Ordinal);

    private readonly LinkedList<LatticePreviewCacheEntry> order = new();
    private readonly Lock gate = new();
    private readonly int entryCapacity;
    private readonly long byteCapacity;
    private long sizeBytes;

    /// <summary>
    /// Initializes a preview cache.
    /// </summary>
    /// <param name="entryCapacity">The maximum number of cached entries.</param>
    /// <param name="byteCapacity">The maximum estimated payload in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">A capacity is non-positive.</exception>
    internal LatticePreviewCache(int entryCapacity, long byteCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entryCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(byteCapacity);
        this.entryCapacity = entryCapacity;
        this.byteCapacity = byteCapacity;
    }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (gate)
            {
                return entries.Count;
            }
        }
    }

    /// <summary>
    /// Gets the estimated cached payload in bytes.
    /// </summary>
    internal long SizeBytes
    {
        get
        {
            lock (gate)
            {
                return sizeBytes;
            }
        }
    }

    /// <summary>
    /// Determines whether a key is cached without changing the recency order.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns><see langword="true"/> when the key is cached.</returns>
    internal bool Contains(string key)
    {
        lock (gate)
        {
            return entries.ContainsKey(key);
        }
    }

    /// <summary>
    /// Tries to read a cached entry and marks it as most recently used.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="entry">The cached entry when present.</param>
    /// <returns><see langword="true"/> when the key is cached.</returns>
    internal bool TryGet(string key, out LatticePreviewCacheEntry? entry)
    {
        lock (gate)
        {
            if (!entries.TryGetValue(key, out var node))
            {
                entry = null;
                return false;
            }

            order.Remove(node);
            order.AddFirst(node);
            entry = node.Value;
            return true;
        }
    }

    /// <summary>
    /// Adds or replaces a cache entry and evicts the least-recently-used entries to respect both bounds.
    /// </summary>
    /// <param name="entry">The entry to store.</param>
    internal void Add(LatticePreviewCacheEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (gate)
        {
            if (entries.TryGetValue(entry.Key, out var existing))
            {
                entries.Remove(entry.Key);
                order.Remove(existing);
                sizeBytes -= existing.Value.EstimatedBytes;
            }

            if (entry.EstimatedBytes > byteCapacity)
            {
                return;
            }

            while (entries.Count >= entryCapacity || sizeBytes + entry.EstimatedBytes > byteCapacity)
            {
                var last = order.Last;
                if (last is null)
                {
                    break;
                }

                order.RemoveLast();
                entries.Remove(last.Value.Key);
                sizeBytes -= last.Value.EstimatedBytes;
            }

            var node = new LinkedListNode<LatticePreviewCacheEntry>(entry);
            order.AddFirst(node);
            entries.Add(entry.Key, node);
            sizeBytes += entry.EstimatedBytes;
        }
    }
}
