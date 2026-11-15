// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.SimulationPackage.Internal;

/// <summary>
/// The contract-typed lattice row handed to the writer before the payload encoder of Step 11 feature 5 exists.
/// </summary>
/// <param name="Signature">The Step 11 lattice signature and <c>lattices.signature</c> key.</param>
/// <param name="FormatVersion">The encoded lattice-payload format version.</param>
/// <param name="Payload">The encoded Zstd+MessagePack lattice payload.</param>
internal sealed record PackageLatticeRow(
    string Signature,
    int FormatVersion,
    ReadOnlyMemory<byte> Payload);
