// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Symmetry;

/// <summary>
/// The seven crystallographic crystal systems.
/// </summary>
/// <remarks>
/// The crystal system is derived from the space-group catalog. It is Core vocabulary but is never serialized
/// in the project file.
/// </remarks>
public enum CrystalSystem
{
    /// <summary>
    /// Triclinic.
    /// </summary>
    Triclinic = 0,

    /// <summary>
    /// Monoclinic.
    /// </summary>
    Monoclinic = 1,

    /// <summary>
    /// Orthorhombic.
    /// </summary>
    Orthorhombic = 2,

    /// <summary>
    /// Tetragonal.
    /// </summary>
    Tetragonal = 3,

    /// <summary>
    /// Trigonal.
    /// </summary>
    Trigonal = 4,

    /// <summary>
    /// Hexagonal.
    /// </summary>
    Hexagonal = 5,

    /// <summary>
    /// Cubic.
    /// </summary>
    Cubic = 6,
}
