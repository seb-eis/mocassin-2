// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Particles;

[Trait("Tier", "PR-fast")]
public sealed class ParticleModelTests
{
    [Fact]
    public void DefaultArraysNormalizeToEmpty()
    {
        var model = new ParticleModel(default(ImmutableArray<Particle>), default(ImmutableArray<ParticleSet>));

        Assert.Empty(model.Particles);
        Assert.Empty(model.ParticleSets);
    }

    [Fact]
    public void FindReturnsImplicitVoidParticle()
    {
        var model = new ParticleModel([CreateParticle()], []);

        Assert.Same(Particle.Void, model.Find(ParticleId.Void));
        Assert.Same(Particle.Void, model.VoidParticle);
    }

    [Fact]
    public void FindReturnsImplicitEmptySet()
    {
        var model = new ParticleModel([], [CreateSet()]);

        Assert.Same(ParticleSet.Empty, model.Find(ParticleSetId.Empty));
        Assert.Same(ParticleSet.Empty, model.EmptyParticleSet);
    }

    [Fact]
    public void FindReturnsAuthoredObjects()
    {
        var particle = CreateParticle();
        var particleSet = CreateSet(particle.Id);
        var model = new ParticleModel([particle], [particleSet]);

        Assert.Same(particle, model.Find(particle.Id));
        Assert.Same(particleSet, model.Find(particleSet.Id));
    }

    [Fact]
    public void FindReturnsNullForUnknownIdentifiers()
    {
        var model = new ParticleModel([CreateParticle()], [CreateSet()]);

        Assert.Null(model.Find(ParticleId.New()));
        Assert.Null(model.Find(ParticleSetId.New()));
    }

    [Fact]
    public void ImplicitObjectsAreNotPartOfTheAuthoredArrays()
    {
        var model = new ParticleModel([CreateParticle()], [CreateSet()]);

        Assert.DoesNotContain(model.Particles, particle => particle.IsVoid);
        Assert.DoesNotContain(model.ParticleSets, particleSet => particleSet.Id == ParticleSetId.Empty);
    }

    [Fact]
    public void DocumentedLimitsMatchTheModelConstants()
    {
        Assert.Equal(63, ParticleModel.MaxCustomParticles);
        Assert.Equal(100, ParticleModel.MaxParticleSets);
    }

    private static Particle CreateParticle() =>
        new(ParticleId.New(), "O2-", "O", new ChargeElementary(-2.0), IsVacancy: false);

    private static ParticleSet CreateSet(params ParticleId[] particleIds) =>
        new(ParticleSetId.New(), "Oxygen site", [.. particleIds]);
}
