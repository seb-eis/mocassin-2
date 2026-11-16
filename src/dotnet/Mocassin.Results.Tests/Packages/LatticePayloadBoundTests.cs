// SPDX-License-Identifier: MIT

using Mocassin.Results.Packages.Internal;

namespace Mocassin.Results.Tests.Packages;

/// <summary>
/// Exercises the overflow-safe lattice payload bound.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class LatticePayloadBoundTests
{
    private const long cap = 512L * 1024 * 1024;

    [Fact]
    public void SmallCeriaLatticeUsesTheWriterFactor()
    {
        Assert.Equal(4 * 6 * 6 * 6 * 4, LatticePayloadBound.Compute(6, 6, 6, 4, cap));
    }

    [Fact]
    public void LargeButValidLatticeIsBoundedByTheDerivedValue()
    {
        Assert.Equal(4 * 40 * 40 * 40 * 4, LatticePayloadBound.Compute(40, 40, 40, 4, cap));
    }

    [Fact]
    public void BoundIsCappedByTheConfiguredMaximum()
    {
        Assert.Equal(4096, LatticePayloadBound.Compute(40, 40, 40, 4, 4096));
    }

    [Fact]
    public void OverflowAndInvalidDimensionsFallBackToTheCap()
    {
        Assert.Equal(cap, LatticePayloadBound.Compute(long.MaxValue, long.MaxValue, 2, 4, cap));
        Assert.Equal(cap, LatticePayloadBound.Compute(0, 6, 6, 4, cap));
        Assert.Equal(cap, LatticePayloadBound.Compute(6, -1, 6, 4, cap));
        Assert.Equal(cap, LatticePayloadBound.Compute(6, 6, 6, 0, cap));
    }
}
