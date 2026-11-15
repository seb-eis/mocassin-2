// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
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

    [Property(MaxTest = 200)]
    public bool RandomRequestsNeverExceedTheCandidates(int[]? values, int count)
    {
        if (values is null)
        {
            return true;
        }

        var candidates = values.Distinct().Take(64).ToArray();
        if (candidates.Length == 0)
        {
            return true;
        }

        var sampleSize = (int)((uint)count % (uint)(candidates.Length + 1));
        var selected = new LatticeSampler(Convert.FromHexString(samplerSeed))
            .SampleWithoutReplacement(candidates, sampleSize);

        return selected.Length == sampleSize &&
               selected.Distinct().Count() == sampleSize &&
               selected.All(candidates.Contains);
    }

    [Fact]
    public void SingleDrawsAreApproximatelyUniformAcrossTheCandidateSet()
    {
        const int candidateCount = 12;
        const int drawCount = 12_000;
        var counts = new int[candidateCount];
        var sampler = new LatticeSampler(Convert.FromHexString(samplerSeed));
        int[] candidates = [.. Enumerable.Range(0, candidateCount)];

        for (var draw = 0; draw < drawCount; draw++)
        {
            counts[sampler.SampleWithoutReplacement(candidates, 1)[0]]++;
        }

        // Expected count is 1,000 with a standard deviation of about 30; the bounds are roughly five sigma
        // and keep the test deterministic (fixed seed and draw count).
        Assert.All(counts, count => Assert.InRange(count, 850, 1_150));
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
