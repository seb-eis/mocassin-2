// SPDX-License-Identifier: MIT

using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-medium")]
public sealed class LatticeSamplerTests
{
    private const string samplerSeed =
        "7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394";

    [Fact]
    public void SampleWithoutReplacementMatchesDocumentedGoldenVector()
    {
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));

        var selected = sampler.SampleWithoutReplacement([.. Enumerable.Range(0, 10)], 3);

        Assert.Equal([1, 7, 2], selected.ToArray());
    }

    [Fact]
    public void CandidateOrderDoesNotChangeTheSample()
    {
        var candidates = Enumerable.Range(0, 10).ToArray();
        var forward = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement(candidates, 3);
        var reversed = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement([.. candidates.Reverse()], 3);

        Assert.Equal(forward.ToArray(), reversed.ToArray());
    }

    [Fact]
    public void SamplingIsDeterministicPerSeed()
    {
        var first = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement([.. Enumerable.Range(0, 100)], 10);
        var second = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement([.. Enumerable.Range(0, 100)], 10);

        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(10, first.Distinct().Count());
    }

    [Fact]
    public void DifferentSeedProducesADifferentStreamAndSample()
    {
        var first = new LatticeSampler(Convert.FromHexString(samplerSeed));
        var second = new LatticeSampler(Convert.FromHexString(new string('0', 64)));

        Assert.NotEqual(first.NextUInt64(), second.NextUInt64());
        Assert.NotEqual(
            new LatticeSampler(Convert.FromHexString(samplerSeed))
                .SampleWithoutReplacement([.. Enumerable.Range(0, 100)], 10)
                .ToArray(),
            new LatticeSampler(Convert.FromHexString(new string('1', 64)))
                .SampleWithoutReplacement([.. Enumerable.Range(0, 100)], 10)
                .ToArray());
    }

    [Fact]
    public void BoundedValuesStayBelowTheUpperBound()
    {
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));
        for (ulong upper = 1; upper < 1000; upper++)
        {
            for (var draw = 0; draw < 5; draw++)
            {
                Assert.InRange(sampler.NextBounded(upper), 0UL, upper - 1);
            }
        }
    }

    [Fact]
    public void ZeroUpperBoundIsRejected()
    {
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));

        Assert.Throws<ArgumentOutOfRangeException>(() => sampler.NextBounded(0));
    }

    [Fact]
    public void SampleContainsOnlyCandidateValues()
    {
        var candidates = new[] { 10, 30, 20, 50, 40, 80, 70, 60, 90, 0 };

        var selected = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement(candidates, 5);

        Assert.Equal(5, selected.Length);
        Assert.Equal(5, selected.Distinct().Count());
        Assert.All(selected, value => Assert.Contains(value, candidates));
    }

    [Fact]
    public void EmptySampleDoesNotConsumeTheStream()
    {
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));
        var reference = new LatticeSampler(Convert.FromHexString(samplerSeed));

        Assert.Empty(sampler.SampleWithoutReplacement([1, 2, 3], 0));
        Assert.Equal(reference.NextUInt64(), sampler.NextUInt64());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void OutOfRangeSampleSizeIsRejected(int count)
    {
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => sampler.SampleWithoutReplacement([1, 2, 3], count));
    }

    [Fact]
    public void SeedMustHaveExactly32Bytes()
    {
        Assert.Throws<ArgumentException>(() => new LatticeSampler(new byte[31]));
    }
}
