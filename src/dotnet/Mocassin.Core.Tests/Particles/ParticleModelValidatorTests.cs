// SPDX-License-Identifier: MIT

using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Particles;

[Trait("Tier", "PR-fast")]
public sealed class ParticleModelValidatorTests
{
    [Fact]
    public void ValidModelProducesNoDiagnostics()
    {
        var oxygen = CreateParticle("O2-", "O", -2.0);
        var cerium = CreateParticle("Ce4+", "Ce", 4.0);
        var model = Model(
            [oxygen, cerium],
            [CreateSet("Oxygen site", oxygen.Id), CreateSet("Cerium site", cerium.Id)]);

        Assert.Empty(ParticleModelValidator.Validate(model));
    }

    [Fact]
    public void EmptyParticleNameProducesPar001()
    {
        var model = Model([CreateParticle(" ", "O", 0.0)], []);

        var diagnostic = Assert.Single(ParticleModelValidator.Validate(model));

        Assert.Equal(ParticleDiagnosticCodes.InvalidName, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("name", diagnostic.Path?.Property);
    }

    [Fact]
    public void DuplicateParticleNameIsCaseInsensitiveAndProducesPar001()
    {
        var model = Model(
            [CreateParticle("Oxygen", "O", 0.0), CreateParticle("oxygen", "O", 0.0)],
            []);

        var diagnostic = Assert.Single(
            ParticleModelValidator.Validate(model).Where(
                static item => item.Code == ParticleDiagnosticCodes.InvalidName));

        Assert.Equal("name", diagnostic.Path?.Property);
    }

    [Fact]
    public void SixtyThreeParticlesAreAccepted()
    {
        var model = Model(CreateParticles(63), []);

        Assert.DoesNotContain(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ParticleLimitExceeded);
    }

    [Fact]
    public void SixtyFourParticlesProducePar002()
    {
        var model = Model(CreateParticles(64), []);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ParticleLimitExceeded);
    }

    [Fact]
    public void OneHundredSetsAreAccepted()
    {
        var model = Model(CreateParticles(7), CreateSets(100));

        Assert.DoesNotContain(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.SetLimitExceeded);
    }

    [Fact]
    public void MoreThanOneHundredSetsProducePar007()
    {
        var model = Model(CreateParticles(7), CreateSets(101));

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.SetLimitExceeded);
    }

    [Fact]
    public void EmptySetProducesPar005()
    {
        var model = Model([], [CreateSet("Empty site")]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidSetMembers);
    }

    [Fact]
    public void SetWithDefaultParticleIdsThroughWithExpressionProducesPar005()
    {
        var particleSet = CreateSet("Empty site") with { ParticleIds = default };
        var model = Model([], [particleSet]);

        Assert.Empty(particleSet.ParticleIds);
        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidSetMembers);
    }

    [Fact]
    public void SetContainingVoidProducesPar005()
    {
        var model = Model([], [CreateSet("Void site", ParticleId.Void)]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidSetMembers);
    }

    [Fact]
    public void SetReferencingUnknownParticleProducesPar006()
    {
        var model = Model([], [CreateSet("Broken site", ParticleId.New())]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.UnknownParticleReference);
    }

    [Fact]
    public void DuplicateOrderedSetsWarnPar009()
    {
        var oxygen = ParticleId.New();
        var vacancy = ParticleId.New();
        var model = Model(
            [CreateParticle("O2-", "O", -2.0, oxygen), CreateParticle("Vacancy", "Vc", 0.0, vacancy)],
            [
                CreateSet("First", oxygen, vacancy),
                CreateSet("Second", oxygen, vacancy),
            ]);

        var diagnostics = ParticleModelValidator.Validate(model);

        var warning = Assert.Single(
            diagnostics.Where(static item => item.Code == ParticleDiagnosticCodes.DuplicateSetContent));
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void SameMembersInDifferentOrderDoNotWarnPar009()
    {
        var oxygen = ParticleId.New();
        var vacancy = ParticleId.New();
        var model = Model(
            [CreateParticle("O2-", "O", -2.0, oxygen), CreateParticle("Vacancy", "Vc", 0.0, vacancy)],
            [
                CreateSet("First", oxygen, vacancy),
                CreateSet("Second", vacancy, oxygen),
            ]);

        Assert.DoesNotContain(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.DuplicateSetContent);
    }

    [Fact]
    public void AuthoredParticleUsingReservedVoidIdProducesPar010()
    {
        var model = Model([CreateParticle("Void", "∅", 0.0, ParticleId.Void)], []);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ReservedIdentifier);
    }

    [Fact]
    public void AuthoredSetUsingReservedEmptyIdProducesPar010()
    {
        var particle = CreateParticle("O2-", "O", -2.0);
        var reserved = new ParticleSet(ParticleSetId.Empty, "Reserved", [particle.Id]);
        var model = Model([particle], [reserved]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ReservedIdentifier);
    }

    [Fact]
    public void EmptySetNameProducesPar011()
    {
        var particle = CreateParticle("O2-", "O", -2.0);
        var model = Model([particle], [CreateSet(" ", particle.Id)]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidSetName);
    }

    [Fact]
    public void DuplicateSetNameIsCaseInsensitiveAndProducesPar011()
    {
        var first = CreateParticle("O2-", "O", -2.0);
        var second = CreateParticle("VO", "VOID", 0.0);
        var model = Model(
            [first, second],
            [CreateSet("Oxygen site", first.Id), CreateSet("oxygen site", second.Id)]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidSetName);
    }

    [Fact]
    public void DuplicateSetMemberProducesPar012()
    {
        var particle = CreateParticle("O2-", "O", -2.0);
        var model = Model([particle], [CreateSet("Oxygen site", particle.Id, particle.Id)]);

        Assert.Contains(
            ParticleModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.DuplicateSetMember);
    }

    [Fact]
    public void NonRecommendedSymbolsWarnPar008()
    {
        var model = Model(
            [
                CreateParticle("Empty symbol", string.Empty, 0.0),
                CreateParticle("Long symbol", "ABCDEFGHIJKL", 0.0),
                CreateParticle("Lower-case symbol", "oxygen", 0.0),
                CreateParticle("Recommended symbol", "O2-", 0.0),
            ],
            []);

        var diagnostics = ParticleModelValidator.Validate(model);

        Assert.Equal(
            3,
            diagnostics.Count(static item => item.Code == ParticleDiagnosticCodes.NonRecommendedSymbol));
        Assert.All(
            diagnostics.Where(static item => item.Code == ParticleDiagnosticCodes.NonRecommendedSymbol),
            static diagnostic => Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity));
    }

    [Property(MaxTest = 100)]
    public bool ValidRandomModelsHaveNoDiagnostics(byte particleCountSeed, byte setCountSeed)
    {
        var particleCount = (particleCountSeed % 20) + 1;
        var setCount = (setCountSeed % particleCount) + 1;
        var particles = CreateParticles(particleCount);
        var sets = particles
            .Take(setCount)
            .Select((particle, index) => CreateSet($"Site {index}", particle.Id))
            .ToArray();

        return ParticleModelValidator.Validate(Model(particles, sets)).IsEmpty;
    }

    private static ParticleModel Model(
        IReadOnlyList<Particle> particles,
        IReadOnlyList<ParticleSet>? sets = null) =>
        new([.. particles], sets is null ? [] : [.. sets]);

    private static Particle[] CreateParticles(int count) =>
        [.. Enumerable.Range(0, count).Select(index => CreateParticle($"P{index}", $"S{index}", 0.0))];

    private static ParticleSet[] CreateSets(int count)
    {
        var particles = CreateParticles(7);
        return
        [
            .. Enumerable.Range(0, count).Select(index =>
            {
                var members = Enumerable.Range(0, particles.Length)
                    .Where(bit => (index & (1 << bit)) != 0)
                    .Select(bit => particles[bit].Id)
                    .ToArray();
                return CreateSet($"Set {index}", members.Length == 0 ? new[] { particles[0].Id } : members);
            }),
        ];
    }

    private static Particle CreateParticle(
        string name,
        string symbol,
        double charge,
        ParticleId? id = null) =>
        new(id ?? ParticleId.New(), name, symbol, new ChargeElementary(charge), IsVacancy: false);

    private static ParticleSet CreateSet(string name, params ParticleId[] particleIds) =>
        new(ParticleSetId.New(), name, [.. particleIds]);
}
