// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Reduction;

/// <summary>
/// The exact point-group decomposition for a center site and an ordered geometry sequence.
/// </summary>
/// <param name="CenterStabilizer"><c>G0</c>: operations that fix the center modulo lattice translation.</param>
/// <param name="SetwiseStabilizer"><c>Gset</c>: operations in <c>G0</c> that map the unordered site set onto itself.</param>
/// <param name="PointwiseStabilizer"><c>Gseq</c>: operations in <c>G0</c> that fix every site individually.</param>
/// <param name="InducedPermutations"><c>P ≅ Gset / Gseq</c>: distinct induced site permutations.</param>
public sealed record PointGroup(
    ImmutableArray<SymmetryOperation> CenterStabilizer,
    ImmutableArray<SymmetryOperation> SetwiseStabilizer,
    ImmutableArray<SymmetryOperation> PointwiseStabilizer,
    ImmutableArray<SitePermutation> InducedPermutations)
{
    /// <summary>
    /// Gets the ordered-orbit size <c>|G0| / |Gseq|</c>.
    /// </summary>
    public int OrderedOrbitSize =>
        PointwiseStabilizer.Length == 0 ? 0 : CenterStabilizer.Length / PointwiseStabilizer.Length;

    /// <summary>
    /// Gets the set-orbit size <c>|G0| / |Gset|</c>.
    /// </summary>
    public int SetOrbitSize =>
        SetwiseStabilizer.Length == 0 ? 0 : CenterStabilizer.Length / SetwiseStabilizer.Length;
}
