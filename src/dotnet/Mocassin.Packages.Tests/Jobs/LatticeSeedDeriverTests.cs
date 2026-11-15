// SPDX-License-Identifier: MIT

using Mocassin.Packages.Jobs;

namespace Mocassin.Packages.Tests.Jobs;

[Trait("Tier", "PR-fast")]
public sealed class LatticeSeedDeriverTests
{
    private const string jobSeed =
        "b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e";

    private const string requestHash =
        "5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295";

    [Fact]
    public void LatticeSeedMatchesDocumentedGoldenVector()
    {
        var seed = LatticeSeedDeriver.DeriveLatticeSeed(
            Convert.FromHexString(jobSeed),
            Convert.FromHexString(requestHash));

        Assert.Equal(
            "a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void ZeroRequestHashMatchesTheSamplerGoldenVectorSeed()
    {
        var seed = LatticeSeedDeriver.DeriveLatticeSeed(
            Convert.FromHexString(
                "884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7"),
            new byte[32]);

        Assert.Equal(
            "7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394",
            Convert.ToHexStringLower(seed.AsSpan()));
    }

    [Fact]
    public void DifferentInputsProduceDifferentSeeds()
    {
        var baseline = LatticeSeedDeriver.DeriveLatticeSeed(
            Convert.FromHexString(jobSeed),
            Convert.FromHexString(requestHash));

        Assert.NotEqual(
            Convert.ToHexStringLower(baseline.AsSpan()),
            Convert.ToHexStringLower(LatticeSeedDeriver.DeriveLatticeSeed(
                Convert.FromHexString(jobSeed),
                new byte[32]).AsSpan()));
        Assert.NotEqual(
            Convert.ToHexStringLower(baseline.AsSpan()),
            Convert.ToHexStringLower(LatticeSeedDeriver.DeriveLatticeSeed(
                new byte[32],
                Convert.FromHexString(requestHash)).AsSpan()));
    }

    [Fact]
    public void DerivationIsDeterministic()
    {
        Assert.Equal<byte>(
            LatticeSeedDeriver.DeriveLatticeSeed(Convert.FromHexString(jobSeed), Convert.FromHexString(requestHash)),
            LatticeSeedDeriver.DeriveLatticeSeed(Convert.FromHexString(jobSeed), Convert.FromHexString(requestHash)));
    }

    [Theory]
    [InlineData(31, 32)]
    [InlineData(32, 31)]
    [InlineData(0, 32)]
    public void InputsMustHaveExactly32Bytes(int jobLength, int hashLength)
    {
        Assert.Throws<ArgumentException>(() => LatticeSeedDeriver.DeriveLatticeSeed(
            new byte[jobLength],
            new byte[hashLength]));
    }
}
