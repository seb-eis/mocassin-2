// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Units;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.TestSupport;

internal static class ParameterizationTestModels
{
    public static readonly Guid TemplateIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80");
    public static readonly Guid ProjectIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e61");
    public static readonly Guid SiteAIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f05");
    public static readonly Guid SiteBIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f06");
    public static readonly Guid ClusterIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f70");
    public static readonly Guid TransitionAIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f51");
    public static readonly Guid TransitionBIdValue = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d8f52");
    public static readonly DateTimeOffset CreatedUtc = DateTimeOffset.Parse(
        "2026-11-14T12:00:00Z",
        CultureInfo.InvariantCulture);

    public const string PairEnergyIdA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public const string PairEnergyIdB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    public const string PairEnergyIdC = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    public const string PairEnergyIdD = "1111111111111111111111111111111111111111111111111111111111111111";
    public const string ClusterEnergyIdA = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    public const string RuleGeometryIdA = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    public const string RuleGeometryIdB = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

    public static readonly ParticleId ParticleA = ParticleId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f01"));

    public static readonly ParticleId ParticleB = ParticleId.Create(
        Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f02"));

    public static readonly CellSiteId SiteA = CellSiteId.Create(SiteAIdValue);
    public static readonly CellSiteId SiteB = CellSiteId.Create(SiteBIdValue);

    public static ParameterizationTemplate CreateEmptyTemplate(
        ParameterizationId? id = null,
        string name = "Ceria parameters")
    {
        return new ParameterizationTemplate(
            id ?? ParameterizationId.Create(TemplateIdValue),
            name,
            "3cee47582fabf954a4b2d176d87ed25a79905f96a21a430fef6d9b6ccd6e6003",
            CreatedUtc);
    }

    public static ParameterizationTemplate CreatePopulatedTemplate()
    {
        var empty = CreateEmptyTemplate();
        return empty with
        {
            StablePairSets =
            [
                CreatePairSet(PairEnergyIdB, chiral: false, [Entry(ParticleB, ParticleB, 0.0)]),
                CreatePairSet(
                    PairEnergyIdA,
                    chiral: false,
                    [
                        Entry(ParticleB, ParticleA, 0.25),
                        Entry(ParticleA, ParticleB, -0.5),
                    ]),
                CreatePairSet(PairEnergyIdC, chiral: true, [Entry(ParticleA, ParticleB, 0.3)]),
            ],
            UnstablePairSets =
            [
                CreatePairSet(PairEnergyIdD, chiral: false, [Entry(ParticleA, ParticleA, 1.5)]),
            ],
            GroupSets =
            [
                new GroupEnergySet(
                    ClusterEnergyIdA,
                    ClusterId.Create(ClusterIdValue),
                    SiteA,
                    [
                        new GroupEnergyEntry([ParticleB, ParticleA, ParticleA], new EnergyEv(-0.12)),
                        new GroupEnergyEntry([ParticleA, ParticleA, ParticleB], new EnergyEv(0.48)),
                    ]),
            ],
            SiteEnergyOverrides =
            [
                new SiteEnergyOverride(SiteB, ParticleB, new EnergyEv(-0.2)),
                new SiteEnergyOverride(SiteA, ParticleA, new EnergyEv(0.1)),
            ],
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Metropolis,
                    TransitionBIdValue,
                    [new RuleFrequencyEntry(RuleGeometryIdB, new FrequencyHz(2.0e13))]),
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    TransitionAIdValue,
                    [
                        new RuleFrequencyEntry(RuleGeometryIdB, new FrequencyHz(2.0e13)),
                        new RuleFrequencyEntry(RuleGeometryIdA, new FrequencyHz(1.0e13)),
                    ]),
            ],
        };
    }

    public static PairEnergySet CreatePairSet(
        string energyId,
        bool chiral,
        ImmutableArray<PairEnergyEntry> entries) =>
        new(energyId, SiteA, SiteB, 248, chiral, entries);

    public static PairEnergyEntry Entry(ParticleId center, ParticleId partner, double energy) =>
        new(center, partner, new EnergyEv(energy));

    public static ProjectModel CreateProject(ParameterizationTemplate template) =>
        CreateProject([template]);

    public static ProjectModel CreateProject(ImmutableArray<ParameterizationTemplate> templates) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                ProjectIdValue,
                new string('a', 64),
                "Parameterization test project",
                null,
                CreatedUtc,
                CreatedUtc),
            new ModelDefinition(),
            templates,
            [],
            [],
            ViewSettings.Default);
}
