// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;

namespace Mocassin.Core.Tests.Particles;

[Trait("Tier", "PR-fast")]
public sealed class ParticleSetTests
{
    [Fact]
    public void EmptySetUsesReservedIdentity()
    {
        var particleSet = ParticleSet.Empty;

        Assert.Equal(ParticleSetId.Empty, particleSet.Id);
        Assert.Empty(particleSet.ParticleIds);
        Assert.False(string.IsNullOrWhiteSpace(particleSet.Name));
    }

    [Fact]
    public void DefaultParticleIdsNormalizeToEmpty()
    {
        var particleSet = new ParticleSet(ParticleSetId.New(), "Site", default(ImmutableArray<ParticleId>));

        Assert.Empty(particleSet.ParticleIds);
    }

    [Fact]
    public void DefaultParticleIdsThroughWithExpressionNormalizeToEmpty()
    {
        var particleSet = new ParticleSet(ParticleSetId.New(), "Site", [ParticleId.New()]) with
        {
            ParticleIds = default,
        };

        Assert.Empty(particleSet.ParticleIds);
    }

    [Fact]
    public void ParticleIdsKeepAuthoredOrder()
    {
        var first = ParticleId.New();
        var second = ParticleId.New();

        var particleSet = new ParticleSet(ParticleSetId.New(), "Site", [second, first]);

        Assert.Equal(new[] { second, first }, particleSet.ParticleIds);
    }
}
