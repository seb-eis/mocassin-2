// SPDX-License-Identifier: MIT

using Mocassin.Contracts.Solver;
using Mocassin.Core.Model.Simulations;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class SolverJobFlagsMapperTests
{
    [Fact]
    public void BitTableMatchesTheDocumentedContract()
    {
        Assert.Equal(0u, (uint)SolverJobFlags.None);
        Assert.Equal(1u, (uint)SolverJobFlags.NoSaving);
        Assert.Equal(2u, (uint)SolverJobFlags.NoJumpLogging);
        Assert.Equal(4u, (uint)SolverJobFlags.UseFastExp);
        Assert.Equal(8u, (uint)SolverJobFlags.Deterministic);
        Assert.Equal(16u, (uint)SolverJobFlags.UseDualDofCorrection);
        Assert.Equal(32u, (uint)SolverJobFlags.UsePrerun);
        Assert.Equal(64u, (uint)SolverJobFlags.KmcSimulation);
        Assert.Equal(128u, (uint)SolverJobFlags.MmcSimulation);
    }

    [Fact]
    public void FromAuthoredMapsOnlyAuthoredBits()
    {
        var flags = SolverJobFlagsMapper.FromAuthored(
            ExecutionFlags.NoSaving |
            ExecutionFlags.NoJumpLogging |
            ExecutionFlags.UseFastExp |
            ExecutionFlags.Deterministic);

        Assert.Equal(
            SolverJobFlags.NoSaving |
            SolverJobFlags.NoJumpLogging |
            SolverJobFlags.UseFastExp |
            SolverJobFlags.Deterministic,
            flags);
        Assert.Equal(15u, (uint)flags);
    }

    [Fact]
    public void NoneMapsToNoBits()
    {
        Assert.Equal(SolverJobFlags.None, SolverJobFlagsMapper.FromAuthored(ExecutionFlags.None));
        Assert.Equal(ExecutionFlags.None, SolverJobFlagsMapper.ToAuthored(SolverJobFlags.None));
    }

    [Fact]
    public void ComposeAddsKindPrerunAndDualDofBitsForKmc()
    {
        var flags = SolverJobFlagsMapper.Compose(
            ExecutionFlags.NoJumpLogging,
            SimulationKind.Kmc,
            preRunMcsp: 100,
            useDualDofCorrection: true);

        Assert.Equal(
            SolverJobFlags.NoJumpLogging |
            SolverJobFlags.UsePrerun |
            SolverJobFlags.UseDualDofCorrection |
            SolverJobFlags.KmcSimulation,
            flags);
    }

    [Fact]
    public void ComposeOmitsPrerunWhenPreRunIsZero()
    {
        var flags = SolverJobFlagsMapper.Compose(
            ExecutionFlags.None,
            SimulationKind.Kmc,
            preRunMcsp: 0,
            useDualDofCorrection: false);

        Assert.Equal(SolverJobFlags.KmcSimulation, flags);
    }

    [Fact]
    public void ComposeAddsMmcKindAndIgnoresKmcDerivedIntent()
    {
        var flags = SolverJobFlagsMapper.Compose(
            ExecutionFlags.UseFastExp,
            SimulationKind.Mmc,
            preRunMcsp: 100,
            useDualDofCorrection: true);

        Assert.Equal(SolverJobFlags.UseFastExp | SolverJobFlags.MmcSimulation, flags);
    }

    [Fact]
    public void NegativePreRunIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SolverJobFlagsMapper.Compose(ExecutionFlags.None, SimulationKind.Kmc, -1, false));
    }

    [Fact]
    public void UnknownKindIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SolverJobFlagsMapper.Compose(ExecutionFlags.None, (SimulationKind)99, 0, false));
    }

    [Fact]
    public void ToAuthoredIgnoresDerivedBits()
    {
        var flags =
            SolverJobFlags.NoSaving |
            SolverJobFlags.Deterministic |
            SolverJobFlags.UseDualDofCorrection |
            SolverJobFlags.UsePrerun |
            SolverJobFlags.KmcSimulation;

        Assert.Equal(
            ExecutionFlags.NoSaving | ExecutionFlags.Deterministic,
            SolverJobFlagsMapper.ToAuthored(flags));
    }

    [Theory]
    [InlineData(0L, false)]
    [InlineData(100L, true)]
    [InlineData(1000L, true)]
    public void AuthoredProjectionRoundTrips(long preRun, bool useDualDof)
    {
        var authored = ExecutionFlags.NoSaving | ExecutionFlags.UseFastExp;
        var composed = SolverJobFlagsMapper.Compose(
            authored,
            SimulationKind.Kmc,
            preRun,
            useDualDof);

        Assert.Equal(authored, SolverJobFlagsMapper.ToAuthored(composed));
    }
}
