// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
[SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "The tests intentionally verify the interface implementations.")]
public sealed class ProjectQueryTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void ProjectImplementsParticleQuery()
    {
        var particle = CreateParticle("O2-", "O", -2.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80");
        var particleSet = CreateSet(
            "Oxygen site",
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83",
            particle.Id);
        var project = CreateProject(particle, particleSet);
        IParticleQuery query = project;

        Assert.Same(particle, Assert.Single(query.Particles));
        Assert.Same(Particle.Void, query.VoidParticle);
        Assert.Same(particle, query.Find(particle.Id));
        Assert.Same(Particle.Void, query.Find(ParticleId.Void));
        Assert.Same(particleSet, query.Find(particleSet.Id));
        Assert.Same(ParticleSet.Empty, query.Find(ParticleSetId.Empty));
        Assert.Null(query.Find(ParticleId.New()));
        Assert.Null(query.Find(ParticleSetId.New()));
    }

    [Fact]
    public void ProjectImplementsStructureQuery()
    {
        var particle = CreateParticle("O2-", "O", -2.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80");
        var particleSet = CreateSet(
            "Oxygen site",
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83",
            particle.Id);
        var site = new CellSite(
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e85")),
            "O",
            new Fractional3(0.25, 0.25, 0.25),
            particleSet.Id,
            SiteStability.Stable);
        var project = CreateProject(particle, particleSet, site);
        IStructureQuery query = project;

        Assert.Same(project.Model.Structure, query.Structure);
        Assert.Same(site, query.Find(site.Id));
        Assert.Null(query.Find(CellSiteId.New()));
    }

    private static ProjectModel CreateProject(
        Particle particle,
        ParticleSet particleSet,
        params CellSite[] sites) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e90"),
                new string('a', 64),
                "Query fixture",
                null,
                createdUtc,
                createdUtc),
            new ModelDefinition
            {
                Particles = [particle],
                ParticleSets = [particleSet],
                Structure = StructureModel.Default with
                {
                    Name = "Ceria",
                    SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
                    Cell = new CellParameters(
                        new LengthAngstrom(5.41),
                        new LengthAngstrom(5.41),
                        new LengthAngstrom(5.41),
                        new AngleDegrees(90.0),
                        new AngleDegrees(90.0),
                        new AngleDegrees(90.0)),
                    Sites = [.. sites],
                },
            },
            [],
            [],
            [],
            ViewSettings.Default);

    private static Particle CreateParticle(string name, string symbol, double charge, string id) =>
        new(
            ParticleId.Create(Guid.Parse(id)),
            name,
            symbol,
            new ChargeElementary(charge),
            IsVacancy: false);

    private static ParticleSet CreateSet(string name, string id, params ParticleId[] members) =>
        new(ParticleSetId.Create(Guid.Parse(id)), name, [.. members]);
}
