// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Symmetry;

namespace Mocassin.Symmetry.SpaceGroups;

/// <summary>
/// One resolved space-group setting from the embedded catalog.
/// </summary>
/// <param name="Number">The international space-group number in <c>1..230</c>.</param>
/// <param name="HermannMauguin">The Hermann–Mauguin symbol stored by the catalog.</param>
/// <param name="HallSymbol">The Hall symbol that uniquely identifies the setting.</param>
/// <param name="CrystalSystem">The crystal system derived from the group number.</param>
/// <param name="Axis">The axis setting.</param>
/// <param name="Origin">The origin choice.</param>
/// <param name="Operations">The canonically ordered symmetry operations.</param>
public sealed record SpaceGroupSetting(
    int Number,
    string HermannMauguin,
    string HallSymbol,
    CrystalSystem CrystalSystem,
    AxisSetting Axis,
    OriginChoice Origin,
    ImmutableArray<SymmetryOperation> Operations);
