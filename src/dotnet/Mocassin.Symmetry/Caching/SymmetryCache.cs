// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mocassin.Symmetry.Geometry;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Caching;

/// <summary>
/// A bounded, per-compilation cache for symmetry work.
/// </summary>
/// <remarks>
/// Keys always include the catalog data version and symmetry contract version, plus the setting identity and
/// the operation-specific inputs. The cache is never static; it is released with the compilation that owns it.
/// A capacity breach is a bounded failure, never an unbounded allocation.
/// </remarks>
internal sealed class SymmetryCache
{
    private readonly Dictionary<string, object> entries = new(StringComparer.Ordinal);
    private readonly Lock gate = new();
    private readonly int capacity;

    /// <summary>
    /// Initializes a cache.
    /// </summary>
    /// <param name="catalogDataVersion">The catalog data version.</param>
    /// <param name="contractVersion">The symmetry contract version.</param>
    /// <param name="capacity">The maximum number of entries.</param>
    /// <exception cref="ArgumentException">A version is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is non-positive.</exception>
    public SymmetryCache(string catalogDataVersion, string contractVersion, int capacity = 4096)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogDataVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        CatalogDataVersion = catalogDataVersion;
        ContractVersion = contractVersion;
        this.capacity = capacity;
    }

    /// <summary>
    /// Gets the catalog data version included in every key.
    /// </summary>
    public string CatalogDataVersion { get; }

    /// <summary>
    /// Gets the contract version included in every key.
    /// </summary>
    public string ContractVersion { get; }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    public int Count
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
    /// Gets a value or computes and stores it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory used on a miss.</param>
    /// <returns>The cached or new value.</returns>
    /// <exception cref="InvalidOperationException">The cache capacity would be exceeded.</exception>
    public T GetOrAdd<T>(string key, Func<T> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        lock (gate)
        {
            if (entries.TryGetValue(key, out var existing))
            {
                return (T)existing;
            }

            if (entries.Count >= capacity)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"The symmetry cache exceeded its capacity of {capacity} entries."));
            }

            var value = factory();
            entries.Add(key, value!);
            return value;
        }
    }

    /// <summary>
    /// Determines whether a key is cached.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when present.</returns>
    public bool Contains(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (gate)
        {
            return entries.ContainsKey(key);
        }
    }

    /// <summary>
    /// Creates the cell-geometry key.
    /// </summary>
    /// <param name="cellParametersHash">The SHA-256 cell-parameter hash.</param>
    /// <returns>The cache key.</returns>
    public string CellKey(string cellParametersHash) => BaseKey("cell") + '|' + cellParametersHash;

    /// <summary>
    /// Creates the orbit/stabilizer key.
    /// </summary>
    /// <param name="setting">The setting.</param>
    /// <param name="center">The exact center grid.</param>
    /// <returns>The cache key.</returns>
    public string OrbitKey(SpaceGroupSetting setting, GridFractional3 center) =>
        SettingKey("orbit", setting) + '|' + GridKey(center);

    /// <summary>
    /// Creates the point-group key.
    /// </summary>
    /// <param name="setting">The setting.</param>
    /// <param name="center">The exact center grid.</param>
    /// <param name="geometry">The ordered geometry grids.</param>
    /// <returns>The cache key.</returns>
    public string PointGroupKey(
        SpaceGroupSetting setting,
        GridFractional3 center,
        ImmutableArray<GridFractional3> geometry) =>
        SettingKey("pointGroup", setting)
        + '|'
        + GridKey(center)
        + '|'
        + GridSequenceHash(geometry);

    /// <summary>
    /// Creates the pair-shell key.
    /// </summary>
    /// <param name="setting">The setting.</param>
    /// <param name="centerP">The center P index.</param>
    /// <param name="cutoffBucket">The cutoff bucket in femtometres.</param>
    /// <param name="cellParametersHash">The cell-parameter hash.</param>
    /// <returns>The cache key.</returns>
    public string PairShellKey(
        SpaceGroupSetting setting,
        int centerP,
        int cutoffBucket,
        string cellParametersHash) =>
        SettingKey("pair", setting)
        + string.Create(CultureInfo.InvariantCulture, $"|{centerP}|{cutoffBucket}|")
        + cellParametersHash;

    /// <summary>
    /// Creates the cluster-reduction key.
    /// </summary>
    /// <param name="setting">The setting.</param>
    /// <param name="centerP">The center P index.</param>
    /// <param name="clusterOffsetsHash">The cluster-offset hash.</param>
    /// <param name="allowedOccupationHash">The allowed-occupation hash.</param>
    /// <returns>The cache key.</returns>
    public string ClusterKey(
        SpaceGroupSetting setting,
        int centerP,
        string clusterOffsetsHash,
        string allowedOccupationHash) =>
        SettingKey("cluster", setting)
        + string.Create(CultureInfo.InvariantCulture, $"|{centerP}|")
        + clusterOffsetsHash
        + '|'
        + allowedOccupationHash;

    /// <summary>
    /// Creates the canonical P1-order key.
    /// </summary>
    /// <param name="structureHash">The structure hash.</param>
    /// <returns>The cache key.</returns>
    public string CanonicalP1Key(string structureHash) => BaseKey("p1") + '|' + structureHash;

    /// <summary>
    /// Computes a SHA-256 hash of a grid sequence.
    /// </summary>
    /// <param name="geometry">The exact grids.</param>
    /// <returns>The lowercase hexadecimal hash.</returns>
    internal static string GridSequenceHash(ImmutableArray<GridFractional3> geometry)
    {
        var builder = new StringBuilder();
        foreach (var grid in geometry)
        {
            builder.Append(GridKey(grid)).Append(';');
        }

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private string BaseKey(string kind) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{kind}|{CatalogDataVersion}|{ContractVersion}");

    private string SettingKey(string kind, SpaceGroupSetting setting) =>
        BaseKey(kind)
        + string.Create(
            CultureInfo.InvariantCulture,
            $"|{setting.Number}|{(int)setting.Axis}|{(int)setting.Origin}");

    private static string GridKey(GridFractional3 grid) =>
        string.Create(CultureInfo.InvariantCulture, $"{grid.A},{grid.B},{grid.C}");
}
