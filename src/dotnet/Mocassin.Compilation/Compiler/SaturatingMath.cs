// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Compiler;

/// <summary>
/// Provides saturating non-negative <see cref="long"/> arithmetic for resource estimates.
/// </summary>
internal static class SaturatingMath
{
    /// <summary>
    /// Returns the saturating product of two non-negative values.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>The product, or <see cref="long.MaxValue"/> on overflow.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A value is negative.</exception>
    public static long Multiply(long left, long right)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(right);
        if (left == 0 || right == 0)
        {
            return 0;
        }

        return left > long.MaxValue / right ? long.MaxValue : left * right;
    }

    /// <summary>
    /// Returns the saturating sum of two non-negative values.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns>The sum, or <see cref="long.MaxValue"/> on overflow.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A value is negative.</exception>
    public static long Add(long left, long right)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(left);
        ArgumentOutOfRangeException.ThrowIfNegative(right);
        return right > long.MaxValue - left ? long.MaxValue : left + right;
    }
}
