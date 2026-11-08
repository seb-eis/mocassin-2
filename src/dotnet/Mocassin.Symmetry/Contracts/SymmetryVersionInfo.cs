// SPDX-License-Identifier: MIT

using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Contracts;

/// <summary>
/// The symmetry versions recorded by the compiled package.
/// </summary>
/// <param name="SymmetryContractVersion">The contract version, for example <c>symmetry-1</c>.</param>
/// <param name="SpaceGroupDataVersion">The catalog data version, for example <c>spglib-2.8.0/catalog-1</c>.</param>
/// <param name="SpaceGroupDataSha256">The SHA-256 of the canonical catalog settings payload.</param>
public sealed record SymmetryVersionInfo(
    string SymmetryContractVersion,
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256)
{
    /// <summary>
    /// Creates the version record from a loaded catalog.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <returns>The version record.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="catalog"/> is <see langword="null"/>.</exception>
    public static SymmetryVersionInfo FromCatalog(SpaceGroupCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return new SymmetryVersionInfo(
            SymmetryContract.Version,
            catalog.DataVersion,
            catalog.DataSha256);
    }
}
