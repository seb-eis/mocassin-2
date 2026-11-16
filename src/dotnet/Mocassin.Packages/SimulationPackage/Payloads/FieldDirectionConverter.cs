// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Geometry;

namespace Mocassin.Packages.SimulationPackage.Payloads;

/// <summary>
/// Converts the authored fractional electric-field direction to the normalized Cartesian vector stored in the
/// model and job payloads.
/// </summary>
internal static class FieldDirectionConverter
{
    /// <summary>
    /// Converts and normalizes the authored fractional direction with the compiled cell vectors.
    /// </summary>
    /// <param name="direction">The authored fractional direction.</param>
    /// <param name="cellVectors">The compiled <c>a</c>, <c>b</c>, and <c>c</c> vectors in ångström.</param>
    /// <returns>
    /// The normalized Cartesian unit vector, or <c>(1, 0, 0)</c> when the authored vector is zero.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The direction is not finite, the cell vectors are not exactly three finite vectors, or the conversion
    /// produces a non-finite value.
    /// </exception>
    public static ImmutableArray<double> ToCartesianUnit(
        Fractional3 direction,
        ImmutableArray<Cartesian3> cellVectors)
    {
        if (!direction.IsFinite)
        {
            throw new ArgumentException("The field direction must be finite.", nameof(direction));
        }

        if (cellVectors.IsDefault || cellVectors.Length != 3)
        {
            throw new ArgumentException("Exactly three cell vectors are required.", nameof(cellVectors));
        }

        if (!cellVectors.All(static vector => vector.IsFinite))
        {
            throw new ArgumentException("The cell vectors must be finite.", nameof(cellVectors));
        }

        var cartesian =
            (cellVectors[0] * direction.A) +
            (cellVectors[1] * direction.B) +
            (cellVectors[2] * direction.C);
        if (!cartesian.IsFinite)
        {
            throw new ArgumentException("The converted field direction is not finite.", nameof(direction));
        }

        var length = cartesian.Length;
        if (length == 0.0)
        {
            return [1.0, 0.0, 0.0];
        }

        var unit = cartesian / length;
        return [unit.X, unit.Y, unit.Z];
    }
}
