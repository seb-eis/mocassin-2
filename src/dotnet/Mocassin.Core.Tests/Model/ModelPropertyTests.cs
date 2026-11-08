// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using FsCheck.Xunit;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Model;

[Trait("Tier", "PR-fast")]
public sealed class ModelPropertyTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        System.Globalization.CultureInfo.InvariantCulture);

    [Property(MaxTest = 100)]
    public bool RandomParticleAndStructureModelsRoundTripCanonically(int seed)
    {
        var state = (uint)seed;
        uint Next()
        {
            state = (state * 1664525u) + 1013904223u;
            return state;
        }

        var particleCount = 1 + (int)(Next() % 3);
        var particles = ImmutableArray.CreateBuilder<Particle>(particleCount);
        for (var index = 0; index < particleCount; index++)
        {
            particles.Add(new Particle(
                ParticleId.Create(CreateGuid(seed, index)),
                $"P{index}",
                $"S{index}",
                new ChargeElementary((int)(Next() % 9) - 4),
                false));
        }

        var particleSetCount = 1 + (int)(Next() % 2);
        var particleSets = ImmutableArray.CreateBuilder<ParticleSet>(particleSetCount);
        for (var index = 0; index < particleSetCount; index++)
        {
            var members = particles
                .Select(static particle => particle.Id)
                .Skip(index % particleCount)
                .Concat(particles.Select(static particle => particle.Id).Take(index % particleCount));
            particleSets.Add(new ParticleSet(
                ParticleSetId.Create(CreateGuid(seed, 100 + index)),
                $"Set{index}",
                [.. members]));
        }

        var siteCount = 1 + (int)(Next() % 4);
        var sites = ImmutableArray.CreateBuilder<CellSite>(siteCount);
        for (var index = 0; index < siteCount; index++)
        {
            sites.Add(new CellSite(
                CellSiteId.Create(CreateGuid(seed, 200 + index)),
                $"Site{index}",
                new Fractional3(
                    (Next() % 1000) / 1000.0,
                    (Next() % 1000) / 1000.0,
                    (Next() % 1000) / 1000.0),
                particleSets[index % particleSetCount].Id,
                Next() % 2 == 0 ? SiteStability.Stable : SiteStability.Unstable));
        }

        var dummyCount = (int)(Next() % 3);
        var dummies = ImmutableArray.CreateBuilder<CellDummyPosition>(dummyCount);
        for (var index = 0; index < dummyCount; index++)
        {
            dummies.Add(new CellDummyPosition(
                CellDummyPositionId.Create(CreateGuid(seed, 300 + index)),
                $"Dummy{index}",
                new Fractional3(
                    (Next() % 1000) / 1000.0,
                    (Next() % 1000) / 1000.0,
                    (Next() % 1000) / 1000.0)));
        }

        var model = new ModelDefinition
        {
            Particles = particles.ToImmutable(),
            ParticleSets = particleSets.ToImmutable(),
            Structure = StructureModel.Default with
            {
                Name = "Random structure",
                SpaceGroup = new SpaceGroupRef(1, AxisSetting.None, OriginChoice.None),
                Cell = CubicCell(5.0),
                Sites = sites.ToImmutable(),
                DummyPositions = dummies.ToImmutable(),
            },
        };
        var project = new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                CreateGuid(seed, 400),
                new string('a', 64),
                "Random model",
                null,
                createdUtc,
                createdUtc),
            model,
            [],
            [],
            [],
            ViewSettings.Default);

        var canonical = ProjectCanonicalizer.ToCanonicalJson(project);
        var loaded = ProjectJson.Deserialize(ProjectCanonicalizer.ToCanonicalUtf8(project));
        var reloaded = ProjectCanonicalizer.ToCanonicalJson(loaded);

        return string.Equals(canonical, reloaded, StringComparison.Ordinal)
            && ProjectHasher.ComputeStructure(project.Model.Structure)
                == ProjectHasher.ComputeStructure(loaded.Model.Structure);
    }

    private static Guid CreateGuid(int seed, int slot) =>
        Guid.Parse($"018f4c9a-7f6b-7c3a-9d2e-{(uint)((seed * 31) + slot):x8}{(uint)slot:x4}");

    private static CellParameters CubicCell(double length) =>
        new(
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0));
}
