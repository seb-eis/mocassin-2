// SPDX-License-Identifier: MIT

namespace Mocassin.Results.Packages.Internal;

/// <summary>
/// Computes the decompressed-size bound for a lattice payload from its referencing job and model.
/// </summary>
/// <remarks>
/// The writer guards its own lattice payloads with <c>4 × a × b × c × position_count</c>. The reader uses the
/// same shape, bounded by the job's <c>lattice_a/b/c</c> columns and the referenced model payload's
/// <c>position_count</c>, and always capped by <see cref="Packages.PackageVerificationOptions.MaxLatticePayloadBytes"/>.
/// An invalid or overflowing dimension falls back to the cap rather than to an unbounded value.
/// </remarks>
internal static class LatticePayloadBound
{
    /// <summary>The per-position safety factor used by the writer's own lattice guard.</summary>
    public const long BytesPerPositionFactor = 4;

    /// <summary>
    /// Computes an overflow-safe bound, falling back to <paramref name="cap"/> for invalid or overflowing
    /// dimensions.
    /// </summary>
    /// <param name="a">The supercell size along <c>a</c>.</param>
    /// <param name="b">The supercell size along <c>b</c>.</param>
    /// <param name="c">The supercell size along <c>c</c>.</param>
    /// <param name="positionCount">The canonical positions per unit cell.</param>
    /// <param name="cap">The hard cap, normally <see cref="Packages.PackageVerificationOptions.MaxLatticePayloadBytes"/>.</param>
    /// <returns>The bound, at most <paramref name="cap"/>.</returns>
    public static long Compute(long a, long b, long c, long positionCount, long cap)
    {
        if (a < 1 || b < 1 || c < 1 || positionCount < 1)
        {
            return cap;
        }

        try
        {
            var product = checked(a * b);
            product = checked(product * c);
            product = checked(product * positionCount);
            product = checked(product * BytesPerPositionFactor);
            return Math.Min(product, cap);
        }
        catch (OverflowException)
        {
            return cap;
        }
    }
}
