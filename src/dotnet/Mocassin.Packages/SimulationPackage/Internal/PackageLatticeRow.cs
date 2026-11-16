// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The contract-typed lattice row inserted by the writer after the payload is encoded and checked.
/// </summary>
/// <param name="Signature">The Step 11 lattice signature and <c>lattices.signature</c> key.</param>
/// <param name="FormatVersion">The encoded lattice-payload format version.</param>
/// <param name="Payload">The encoded Zstd+MessagePack lattice payload.</param>
internal sealed record PackageLatticeRow(
    string Signature,
    int FormatVersion,
    ReadOnlyMemory<byte> Payload);
