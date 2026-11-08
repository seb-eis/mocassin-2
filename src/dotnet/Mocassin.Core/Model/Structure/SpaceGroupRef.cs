// SPDX-License-Identifier: MIT

using Mocassin.Core.Symmetry;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// Persisted reference to a space-group setting in the embedded catalog.
/// </summary>
/// <param name="Number">The International Tables space-group number in the range 1..230.</param>
/// <param name="Axis">The unique-axis or alternative-axis setting.</param>
/// <param name="Origin">The origin-choice setting.</param>
/// <remarks>
/// Only the identifying tuple is persisted. The Hermann–Mauguin symbol, Hall symbol, crystal system,
/// operations, and catalog data version are derived from the embedded catalog by Symmetry services.
/// </remarks>
public sealed record SpaceGroupRef(
    int Number,
    AxisSetting Axis,
    OriginChoice Origin);
