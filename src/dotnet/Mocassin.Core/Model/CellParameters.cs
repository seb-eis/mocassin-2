// SPDX-License-Identifier: MIT

using Mocassin.Core.Units;

namespace Mocassin.Core.Model;

/// <summary>
/// The six cell parameters of a crystallographic unit cell.
/// </summary>
/// <param name="A">The a-axis length.</param>
/// <param name="B">The b-axis length.</param>
/// <param name="C">The c-axis length.</param>
/// <param name="Alpha">The angle between the b and c axes.</param>
/// <param name="Beta">The angle between the a and c axes.</param>
/// <param name="Gamma">The angle between the a and b axes.</param>
public sealed record CellParameters(
    LengthAngstrom A,
    LengthAngstrom B,
    LengthAngstrom C,
    AngleDegrees Alpha,
    AngleDegrees Beta,
    AngleDegrees Gamma);
