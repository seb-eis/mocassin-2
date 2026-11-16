// SPDX-License-Identifier: MIT

using System.Buffers;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using Mocassin.Contracts.Packages;

namespace Mocassin.Contracts.Tests.Packages;

/// <summary>
/// Verifies the canonical five-field lattice projection and its domain-separated signature.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class LatticeSignatureV1Tests
{
    private const string modelSignature =
        "812a7a77a8b78993cf8c475061fbf071fea27082569f154737ed12eb3d64a314";

    [Fact]
    public void ProjectionHasFiveFieldsInManifestOrder()
    {
        var payload = Payload();
        var projection = LatticeSignatureV1.ComputeProjection(modelSignature, payload);
        var reader = new MessagePackReader(projection);

        Assert.Equal(5, reader.ReadArrayHeader());
        Assert.Equal(LatticeSignatureV1.FormatVersion, reader.ReadUInt64());
        Assert.Equal(modelSignature, reader.ReadString());
        Assert.Equal(3, reader.ReadArrayHeader());
        Assert.Equal(2UL, reader.ReadUInt64());
        Assert.Equal(1UL, reader.ReadUInt64());
        Assert.Equal(3UL, reader.ReadUInt64());
        Assert.Equal(2UL, reader.ReadUInt64());
        var particles = reader.ReadBytes();
        Assert.NotNull(particles);
        Assert.Equal(payload.Lattice.AsSpan().ToArray(), particles.Value.ToArray());
        Assert.Equal(projection.Length, reader.Consumed);
    }

    [Fact]
    public void ComputeMatchesTheDomainSeparatedProjectionHash()
    {
        var payload = Payload();
        var projection = LatticeSignatureV1.ComputeProjection(modelSignature, payload);
        var domain = Encoding.UTF8.GetBytes(LatticeSignatureV1.Domain);
        var input = new byte[domain.Length + 1 + projection.Length];
        domain.CopyTo(input, 0);
        input[domain.Length] = 0;
        projection.CopyTo(input, domain.Length + 1);

        Assert.Equal(
            Convert.ToHexStringLower(SHA256.HashData(input)),
            LatticeSignatureV1.Compute(modelSignature, payload));
    }

    [Fact]
    public void ComputeIsDeterministicAndBindsModelIdentityAndParticles()
    {
        var baseline = LatticeSignatureV1.Compute(modelSignature, Payload());

        Assert.Equal(baseline, LatticeSignatureV1.Compute(modelSignature, Payload()));
        Assert.NotEqual(baseline, LatticeSignatureV1.Compute(new string('a', 64), Payload()));
        Assert.NotEqual(
            baseline,
            LatticeSignatureV1.Compute(
                modelSignature,
                Payload(particles: ImmutableArray.Create<byte>(1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1))));
        Assert.NotEqual(
            baseline,
            LatticeSignatureV1.Compute(modelSignature, Payload(latticeSize: ImmutableArray.Create(3u, 1u, 2u))));
    }

    [Fact]
    public void EmptymodelSignatureIsRejected()
    {
        Assert.Throws<ArgumentException>(() => LatticeSignatureV1.ComputeProjection(" ", Payload()));
        Assert.Throws<ArgumentException>(() => LatticeSignatureV1.Compute(string.Empty, Payload()));
    }

    [Theory]
    [InlineData(2u, 1u, 2u, 2u, 12)]
    [InlineData(2u, 1u, 3u, 1u, 12)]
    [InlineData(0u, 1u, 3u, 2u, 0)]
    [InlineData(2u, 1u, 3u, 0u, 0)]
    public void InvalidShapeIsRejected(uint a, uint b, uint c, uint positionCount, int length)
    {
        var payload = new LatticePayload(
            1,
            ImmutableArray.Create(a, b, c),
            positionCount,
            ImmutableArray.CreateRange(Enumerable.Repeat((byte)0, length)));

        Assert.Throws<ArgumentException>(() => LatticeSignatureV1.ComputeProjection(modelSignature, payload));
    }

    [Fact]
    public void UnsupportedPayloadVersionIsRejected()
    {
        var payload = Payload() with { FormatVersion = 2 };

        Assert.Throws<ArgumentException>(() => LatticeSignatureV1.ComputeProjection(modelSignature, payload));
    }

    [Fact]
    public void MissingLatticeSizeOrParticlesIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => LatticeSignatureV1.ComputeProjection(modelSignature, Payload() with { LatticeSize = default }));
        Assert.Throws<ArgumentException>(
            () => LatticeSignatureV1.ComputeProjection(modelSignature, Payload() with { Lattice = default }));
    }

    [Fact]
    public void ProjectionThatExceedsTheIndexLimitIsRejected()
    {
        var payload = new LatticePayload(
            1,
            ImmutableArray.Create(256u, 256u, 256u),
            256,
            ImmutableArray.Create<byte>(0, 1));

        Assert.Throws<ArgumentException>(() => LatticeSignatureV1.ComputeProjection(modelSignature, payload));
    }

    private static LatticePayload Payload(
        ImmutableArray<uint>? latticeSize = null,
        ImmutableArray<byte>? particles = null) =>
        new(
            PackageFormatConstants.LatticePayloadFormatVersion,
            latticeSize ?? ImmutableArray.Create(2u, 1u, 3u),
            2,
            particles ?? ImmutableArray.Create<byte>(0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1));
}
