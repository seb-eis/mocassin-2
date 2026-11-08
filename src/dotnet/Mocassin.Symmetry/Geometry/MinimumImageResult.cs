// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;

namespace Mocassin.Symmetry.Geometry;

/// <summary>
/// The shortest periodic image of a target position relative to a center.
/// </summary>
/// <param name="Offset">
/// The relative image encoding. The cell components are added to the target base position; <c>P</c> is the
/// target's canonical P index.
/// </param>
/// <param name="DistanceAngstrom">The image distance in ångström.</param>
/// <param name="DistanceBucketFm">The distance in integer femtometres.</param>
public readonly record struct MinimumImageResult(
    Vector4I Offset,
    double DistanceAngstrom,
    int DistanceBucketFm);
