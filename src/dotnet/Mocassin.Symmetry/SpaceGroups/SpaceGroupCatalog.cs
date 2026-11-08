// SPDX-License-Identifier: MIT

using System.Collections.Frozen;
using System.Collections.Immutable;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry;

/// <summary>
/// An immutable, process-wide space-group catalog.
/// </summary>
/// <remarks>
/// Use <see cref="Default"/> for the embedded catalog. Settings are sorted by
/// <c>(number, axis, origin)</c>; lookups are content-immutable and thread-safe.
/// </remarks>
public sealed class SpaceGroupCatalog
{
    private static readonly Lazy<SpaceGroupCatalog> defaultCatalog =
        new(SpaceGroupCatalogLoader.LoadEmbedded, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly FrozenDictionary<(int Number, AxisSetting Axis, OriginChoice Origin), SpaceGroupSetting> byKey;
    private readonly FrozenDictionary<string, SpaceGroupSetting> byHallSymbol;
    private readonly FrozenDictionary<string, ImmutableArray<SpaceGroupSetting>> byHermannMauguin;

    internal SpaceGroupCatalog(
        string dataVersion,
        string dataSha256,
        ImmutableArray<SpaceGroupSetting> settings)
    {
        DataVersion = dataVersion;
        DataSha256 = dataSha256;
        Settings = settings;
        byKey = settings.ToFrozenDictionary(
            static setting => (setting.Number, setting.Axis, setting.Origin));
        byHallSymbol = settings.ToFrozenDictionary(
            static setting => setting.HallSymbol,
            StringComparer.Ordinal);
        byHermannMauguin = settings
            .GroupBy(static setting => setting.HermannMauguin, StringComparer.Ordinal)
            .ToFrozenDictionary(
                static group => group.Key,
                static group => group.ToImmutableArray(),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the embedded catalog, loaded and validated once per process.
    /// </summary>
    public static SpaceGroupCatalog Default => defaultCatalog.Value;

    /// <summary>
    /// Gets the catalog data version, for example <c>spglib-2.8.0/catalog-1</c>.
    /// </summary>
    public string DataVersion { get; }

    /// <summary>
    /// Gets the SHA-256 checksum of the canonical settings payload.
    /// </summary>
    public string DataSha256 { get; }

    /// <summary>
    /// Gets every setting in canonical <c>(number, axis, origin)</c> order.
    /// </summary>
    public ImmutableArray<SpaceGroupSetting> Settings { get; }

    /// <summary>
    /// Resolves a structured space-group reference.
    /// </summary>
    /// <param name="number">The international group number.</param>
    /// <param name="axis">The axis setting.</param>
    /// <param name="origin">The origin choice.</param>
    /// <returns>The matching setting, or <see langword="null"/> when no setting matches.</returns>
    public SpaceGroupSetting? GetSetting(int number, AxisSetting axis, OriginChoice origin) =>
        byKey.TryGetValue((number, axis, origin), out var setting) ? setting : null;

    /// <summary>
    /// Resolves a setting by its exact stored Hall symbol.
    /// </summary>
    /// <param name="hallSymbol">The Hall symbol.</param>
    /// <returns>The matching setting, or <see langword="null"/> when no setting matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hallSymbol"/> is <see langword="null"/>.</exception>
    public SpaceGroupSetting? GetByHallSymbol(string hallSymbol)
    {
        ArgumentNullException.ThrowIfNull(hallSymbol);
        return byHallSymbol.TryGetValue(hallSymbol, out var setting) ? setting : null;
    }

    /// <summary>
    /// Resolves every setting sharing an exact stored Hermann–Mauguin symbol.
    /// </summary>
    /// <param name="hermannMauguin">The stored Hermann–Mauguin symbol.</param>
    /// <returns>
    /// All matching settings in canonical order. Callers disambiguate with axis and origin when more than one
    /// setting is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="hermannMauguin"/> is <see langword="null"/>.
    /// </exception>
    public ImmutableArray<SpaceGroupSetting> GetByHermannMauguin(string hermannMauguin)
    {
        ArgumentNullException.ThrowIfNull(hermannMauguin);
        return byHermannMauguin.TryGetValue(hermannMauguin, out var settings)
            ? settings
            : ImmutableArray<SpaceGroupSetting>.Empty;
    }
}
