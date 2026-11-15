// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.Jobs;

/// <summary>
/// The outcome of generating one base supercell.
/// </summary>
/// <remarks>
/// Invalid user data returns diagnostics and a <see langword="null"/> <see cref="Lattice"/>; generation never
/// exposes a partial lattice. <see cref="LatticeRequestHash"/> and <see cref="LatticeSeed"/> are build-time
/// provenance used by later doping and preview features, and are <see langword="null"/>/empty on failure.
/// </remarks>
/// <param name="Lattice">The generated lattice, or <see langword="null"/> on failure.</param>
/// <param name="LatticeRequestHash">The lowercase hexadecimal lattice-request hash, or <see langword="null"/>.</param>
/// <param name="LatticeSeed">The 32-byte lattice seed, or an empty array on failure.</param>
/// <param name="Diagnostics">The diagnostics in stable severity-first order.</param>
public sealed record SupercellGenerationResult(
    GeneratedLattice? Lattice,
    string? LatticeRequestHash,
    ImmutableArray<byte> LatticeSeed,
    ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether generation produced a lattice without any error diagnostic.
    /// </summary>
    public bool IsSuccess =>
        Lattice is not null &&
        !Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
