// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The contract-typed model row handed to the writer before the payload encoder of Step 11 feature 4 exists.
/// </summary>
/// <param name="Signature">The Step 09 parameterization signature and <c>models.signature</c> key.</param>
/// <param name="FormatVersion">The encoded model-payload format version.</param>
/// <param name="Payload">The encoded Zstd+MessagePack model payload.</param>
/// <param name="SymmetryContractVersion">The compiled symmetry contract version.</param>
/// <param name="SpaceGroupDataVersion">The compiled space-group catalog data version.</param>
/// <param name="SpaceGroupDataSha256">The compiled space-group catalog SHA-256.</param>
internal sealed record PackageModelRow(
    string Signature,
    int FormatVersion,
    ReadOnlyMemory<byte> Payload,
    string SymmetryContractVersion,
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256);
