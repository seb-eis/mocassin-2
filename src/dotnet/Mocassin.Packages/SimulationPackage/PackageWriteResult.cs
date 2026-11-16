// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Packages.SimulationPackage;

/// <summary>
/// The result of a published simulation package.
/// </summary>
/// <param name="Path">The final package path.</param>
/// <param name="Sha256">The lowercase SHA-256 of the exact published bytes.</param>
/// <param name="JobCount">The number of job rows.</param>
/// <param name="ModelCount">The number of model rows (one in v1).</param>
/// <param name="LatticeCount">The number of distinct lattice rows.</param>
/// <param name="SizeBytes">The published file size in bytes.</param>
/// <param name="Warnings">Advisory build warnings such as <c>PKG009</c>.</param>
public sealed record PackageWriteResult(
    string Path,
    string Sha256,
    int JobCount,
    int ModelCount,
    int LatticeCount,
    long SizeBytes,
    ImmutableArray<Diagnostic> Warnings)
{
    /// <summary>
    /// Gets the lattice reuse factor <c>JobCount / LatticeCount</c>; 1.0 means every lattice is unique and a
    /// higher value means deduplication.
    /// </summary>
    public double LatticeReuseFactor => LatticeCount == 0 ? 0.0 : (double)JobCount / LatticeCount;
}
