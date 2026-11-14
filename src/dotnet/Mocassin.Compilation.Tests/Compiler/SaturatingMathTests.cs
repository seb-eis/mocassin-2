// SPDX-License-Identifier: MIT

namespace Mocassin.Compilation.Tests.Compiler;

/// <summary>
/// Covers the saturating resource-estimate arithmetic.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class SaturatingMathTests
{
    [Fact]
    public void MultiplySaturatesInsteadOfOverflowing() =>
        Assert.Equal(long.MaxValue, SaturatingMath.Multiply(long.MaxValue / 2 + 1, 2));

    [Fact]
    public void MultiplyByZeroIsZero() => Assert.Equal(0, SaturatingMath.Multiply(0, long.MaxValue));

    [Fact]
    public void AddSaturatesInsteadOfOverflowing() =>
        Assert.Equal(long.MaxValue, SaturatingMath.Add(long.MaxValue, 1));

    [Fact]
    public void NegativeInputIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SaturatingMath.Multiply(-1, 2));
}
