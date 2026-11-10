// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.TestSupport;

internal static class LatticeTestModels
{
    public static readonly DateTimeOffset CreatedUtc =
        DateTimeOffset.Parse("2026-11-10T12:00:00Z", CultureInfo.InvariantCulture);

    public static readonly Guid ProjectId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a00");

    public static Particle Ce4Plus { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a10")),
        "Ce4+",
        "Ce",
        new ChargeElementary(4.0),
        IsVacancy: false);

    public static Particle Ce3Plus { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a11")),
        "Ce3+",
        "Ce",
        new ChargeElementary(3.0),
        IsVacancy: false);

    public static Particle Zr4Plus { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a12")),
        "Zr4+",
        "Zr",
        new ChargeElementary(4.0),
        IsVacancy: false);

    public static Particle O2Minus { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a13")),
        "O2-",
        "O",
        new ChargeElementary(-2.0),
        IsVacancy: false);

    public static Particle OxygenVacancy { get; } = new(
        ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a14")),
        "Oxygen vacancy",
        "Vc",
        new ChargeElementary(0.0),
        IsVacancy: true);

    public static ParticleSet CeSiteSet { get; } = new(
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a20")),
        "Ce site",
        [Ce4Plus.Id, Ce3Plus.Id, Zr4Plus.Id]);

    public static ParticleSet OSiteSet { get; } = new(
        ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a21")),
        "O site",
        [O2Minus.Id, OxygenVacancy.Id]);

    public static CellSite CeSite { get; } = new(
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a30")),
        "Ce site",
        new Fractional3(0.0, 0.0, 0.0),
        CeSiteSet.Id,
        SiteStability.Stable);

    public static CellSite OSite { get; } = new(
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a31")),
        "O site",
        new Fractional3(0.5, 0.5, 0.5),
        OSiteSet.Id,
        SiteStability.Stable);

    public static CellSite UnstableSite { get; } = new(
        CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a32")),
        "O saddle",
        new Fractional3(0.25, 0.25, 0.25),
        OSiteSet.Id,
        SiteStability.Unstable);

    public static OccupationExchange CeReduction { get; } = new(
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a40")),
        "Ce4+ -> Ce3+",
        CeSite.Id,
        Ce4Plus.Id,
        Ce3Plus.Id);

    public static OccupationExchange OxygenVacancyFormation { get; } = new(
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a41")),
        "O2- -> VO",
        OSite.Id,
        O2Minus.Id,
        OxygenVacancy.Id);

    public static OccupationExchange CeIsovalent { get; } = new(
        OccupationExchangeId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a42")),
        "Ce4+ -> Zr4+",
        CeSite.Id,
        Ce4Plus.Id,
        Zr4Plus.Id);

    public static Doping CeriaReductionDoping { get; } = new(
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a50")),
        "Reduce ceria",
        0,
        CeReduction.Id,
        OxygenVacancyFormation.Id,
        ChargeCompensation: true);

    public static Doping IsovalentDoping { get; } = new(
        DopingId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d6a51")),
        "Zirconium substitution",
        1,
        CeIsovalent.Id,
        DependentExchangeId: null,
        ChargeCompensation: false);

    public static ModelDefinition CreateEmptyModel() => new();

    public static ModelDefinition CreateLatticeModel() => new()
    {
        Particles = [Ce4Plus, Ce3Plus, Zr4Plus, O2Minus, OxygenVacancy],
        ParticleSets = [CeSiteSet, OSiteSet],
        Structure = StructureModel.Default with
        {
            Name = "Ceria",
            Sites = [CeSite, OSite, UnstableSite],
        },
        OccupationExchanges = [CeReduction, OxygenVacancyFormation, CeIsovalent],
        Dopings = [CeriaReductionDoping, IsovalentDoping],
    };

    public static ModelDefinition CreateLatticeOnlyModel() => new()
    {
        Particles = [Ce4Plus, Ce3Plus, Zr4Plus, O2Minus, OxygenVacancy],
        ParticleSets = [CeSiteSet, OSiteSet],
        Structure = StructureModel.Default with
        {
            Name = "Ceria",
            Sites = [CeSite, OSite, UnstableSite],
        },
        OccupationExchanges = [CeReduction, OxygenVacancyFormation],
        Dopings = [CeriaReductionDoping],
    };

    public static ProjectModel CreateProject(ModelDefinition? model = null) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                ProjectId,
                new string('b', 64),
                "Lattice model",
                null,
                CreatedUtc,
                CreatedUtc),
            model ?? CreateEmptyModel(),
            [],
            [],
            [],
            ViewSettings.Default);
}
