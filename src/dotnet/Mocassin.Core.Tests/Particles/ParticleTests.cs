// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Particles;

[Trait("Tier", "PR-fast")]
public sealed class ParticleTests
{
    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void VoidParticleUsesReservedIdentity()
    {
        var particle = Particle.Void;

        Assert.Equal(ParticleId.Void, particle.Id);
        Assert.Equal("∅", particle.Symbol);
        Assert.Equal(0.0, particle.Charge.Value);
        Assert.False(particle.IsVacancy);
        Assert.True(particle.IsVoid);
    }

    [Fact]
    public void AuthoredParticleIsNotVoid()
    {
        var particle = CreateParticle();

        Assert.False(particle.IsVoid);
    }

    [Fact]
    public void ComputedIsVoidIsNotSerialized()
    {
        var json = JsonSerializer.Serialize(CreateParticle(), serializerOptions);

        Assert.DoesNotContain("isVoid", json, StringComparison.Ordinal);
        Assert.Contains("\"id\"", json, StringComparison.Ordinal);
        Assert.Contains("\"charge\"", json, StringComparison.Ordinal);
    }

    private static Particle CreateParticle() =>
        new(ParticleId.New(), "O2-", "O", new ChargeElementary(-2.0), IsVacancy: false);
}
