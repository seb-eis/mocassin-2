// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;

namespace Mocassin.Symmetry.Energy;

/// <summary>
/// Samples pair images and builds unique directed pair geometries without allocating energy tables.
/// </summary>
/// <remarks>
/// This is the shared kernel used by the energy preview and by Step 08 compilation. Invalid user data returns
/// <see langword="false"/> with an empty pair array and diagnostics; the sampler never throws for user data.
/// </remarks>
public interface IEnergyGeometrySampler
{
    /// <summary>
    /// Samples the stable-stable and unstable-stable pair geometries for a model.
    /// </summary>
    /// <param name="model">The model to sample.</param>
    /// <param name="cellCountA">The supercell size along a.</param>
    /// <param name="cellCountB">The supercell size along b.</param>
    /// <param name="cellCountC">The supercell size along c.</param>
    /// <param name="pairs">The sampled directed pair geometries on success.</param>
    /// <param name="diagnostics">The structured diagnostics on failure.</param>
    /// <returns><see langword="true"/> when the model could be sampled.</returns>
    public bool TrySamplePairs(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<PairInteractionPreview> pairs,
        out ImmutableArray<Diagnostic> diagnostics);
}
