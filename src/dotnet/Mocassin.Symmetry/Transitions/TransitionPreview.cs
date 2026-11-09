// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Transitions;

namespace Mocassin.Symmetry.Transitions;

/// <summary>
/// Self-describing, non-persisted preview of one kinetic or Metropolis transition.
/// </summary>
/// <param name="Transition">The transition reference.</param>
/// <param name="Chain">The abstract chain.</param>
/// <param name="ParticleOrder">The canonical particle order used by the matrices.</param>
/// <param name="ReferencePath">The authored reference path, or the two Metropolis site positions.</param>
/// <param name="ResolvedPath">The resolved reference positions.</param>
/// <param name="ExpandedPaths">The symmetry-expanded paths or Metropolis site pairs.</param>
/// <param name="StateChangeMatrices">Dense per-position donor/acceptor matrices.</param>
/// <param name="ExpectedRuleCount">The expected rule count before conservation filtering.</param>
/// <param name="CellCountA">The requested supercell count along a.</param>
/// <param name="CellCountB">The requested supercell count along b.</param>
/// <param name="CellCountC">The requested supercell count along c.</param>
/// <remarks>
/// The preview is advisory and is never written to the project. The compiled model remains authoritative.
/// </remarks>
public sealed record TransitionPreview(
    TransitionRef Transition,
    TransitionChain Chain,
    ImmutableArray<ParticleId> ParticleOrder,
    ImmutableArray<Fractional3> ReferencePath,
    ImmutableArray<ResolvedPathPosition> ResolvedPath,
    ImmutableArray<ImmutableArray<Fractional3>> ExpandedPaths,
    ImmutableArray<ImmutableArray<int>> StateChangeMatrices,
    int ExpectedRuleCount,
    int CellCountA,
    int CellCountB,
    int CellCountC);
