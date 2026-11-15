// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class JobSeedDeriverTests
{
    private const string masterHex =
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";

    [Fact]
    public void JobSeedMatchesDocumentedGoldenVector()
    {
        var seed = JobSeedDeriver.DeriveJobSeed(Convert.FromHexString(masterHex), 0, 0, 0);

        Assert.Equal(
            "884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void SeedTextMatchesDocumentedGoldenVector()
    {
        var seed = JobSeedDeriver.DeriveJobSeed("ceria-seed-1", 0, 0, 0);

        Assert.Equal(
            "b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void IndicesUseFixedLittleEndianFields()
    {
        var seed = JobSeedDeriver.DeriveJobSeed(Convert.FromHexString(masterHex), 1, 2, 3);

        Assert.Equal(
            "172da959820807561e234261342a48e26076c59e1af34a01487919d16a7daccf",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void CanonicalHexSeedTextIsDecodedRatherThanHashed()
    {
        var seed = JobSeedDeriver.DeriveJobSeed(masterHex, 0, 0, 0);

        Assert.Equal(
            "884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void UppercaseHexSeedTextIsHashedAsText()
    {
        var master = JobSeedDeriver.DeriveMasterSeed(masterHex.ToUpperInvariant());

        Assert.Equal(
            "45cdb16cee351105503cabc303fd62b5f035c95b261cea914df8a37ed52c77d5",
            Convert.ToHexStringLower(master.AsSpan()));
    }

    [Fact]
    public void CollectionSeedOverridesSimulationAndProjectSeed()
    {
        Assert.Equal("collection", JobSeedDeriver.ResolveSeedText("collection", "simulation", new string('a', 64)));
    }

    [Fact]
    public void SimulationSeedOverridesProjectSeed()
    {
        Assert.Equal("simulation", JobSeedDeriver.ResolveSeedText(null, "simulation", new string('a', 64)));
    }

    [Fact]
    public void ProjectSeedIsTheFallback()
    {
        var project = new string('a', 64);
        Assert.Equal(project, JobSeedDeriver.ResolveSeedText(null, null, project));
    }

    [Fact]
    public void EmptySeedLayersReturnNull()
    {
        Assert.Null(JobSeedDeriver.ResolveSeedText(null, null, null));
        Assert.Null(JobSeedDeriver.ResolveSeedText(string.Empty, string.Empty, string.Empty));
    }

    [Fact]
    public void DifferentIndicesProduceDifferentSeeds()
    {
        var master = Convert.FromHexString(masterHex);
        var seeds = new[]
        {
            JobSeedDeriver.DeriveJobSeed(master, 0, 0, 0),
            JobSeedDeriver.DeriveJobSeed(master, 1, 0, 0),
            JobSeedDeriver.DeriveJobSeed(master, 0, 1, 0),
            JobSeedDeriver.DeriveJobSeed(master, 0, 0, 1),
        };

        Assert.Equal(4, seeds.Select(static seed => Convert.ToHexStringLower(seed.AsSpan())).Distinct().Count());
    }

    [Fact]
    public void DerivationIsDeterministic()
    {
        Assert.Equal<byte>(
            JobSeedDeriver.DeriveJobSeed("ceria-seed-1", 4, 5, 6),
            JobSeedDeriver.DeriveJobSeed("ceria-seed-1", 4, 5, 6));
    }

    [Fact]
    public void MasterSeedMustHaveExactly32Bytes()
    {
        Assert.Throws<ArgumentException>(() => JobSeedDeriver.DeriveJobSeed(new byte[31], 0, 0, 0));
    }

    [Fact]
    public void NegativeIndexIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => JobSeedDeriver.DeriveJobSeed(Convert.FromHexString(masterHex), -1, 0, 0));
    }

    [Fact]
    public void SeedTextHashingIsDomainSeparatedFromPlainSha256()
    {
        var derived = JobSeedDeriver.DeriveMasterSeed("ceria-seed-1");
        var plain = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("ceria-seed-1")));

        Assert.NotEqual(plain, Convert.ToHexStringLower(derived.AsSpan()));
    }
}
