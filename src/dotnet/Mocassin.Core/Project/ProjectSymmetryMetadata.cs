// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Project;

/// <summary>
/// Optional advisory project metadata describing the symmetry catalog and contract versions used to author the
/// project.
/// </summary>
/// <param name="SpaceGroupDataVersion">The embedded space-group catalog data version.</param>
/// <param name="SpaceGroupDataSha256">The SHA-256 of the canonical catalog settings payload.</param>
/// <param name="SymmetryContractVersion">The symmetry contract version.</param>
/// <remarks>
/// Missing metadata is accepted. A present but partial object compares only the fields it contains.
/// <c>Mocassin.Core</c> stores and serializes the values but never reads the embedded catalog; the
/// composition-layer validator performs the comparison.
/// </remarks>
public sealed record ProjectSymmetryMetadata(
    string? SpaceGroupDataVersion,
    string? SpaceGroupDataSha256,
    string? SymmetryContractVersion);
