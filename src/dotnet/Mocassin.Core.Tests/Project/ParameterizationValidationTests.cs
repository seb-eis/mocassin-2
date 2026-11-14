// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Project;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationValidationTests
{
    [Fact]
    public void ValidatorAcceptsThePopulatedTemplate()
    {
        Assert.Empty(Validate(ParameterizationTestModels.CreatePopulatedTemplate()));
    }

    [Fact]
    public void ValidatorRejectsOutOfRangeEnergy()
    {
        var template = WithPairEntry(101.0);

        var diagnostics = Validate(template);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidEnergy &&
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Path?.Domain == "parameterization" &&
                diagnostic.Path.Property?.EndsWith("energyEv", StringComparison.Ordinal) == true);
    }

    [Theory]
    [InlineData(-100.0)]
    [InlineData(100.0)]
    public void ValidatorAcceptsInclusiveEnergyLimits(double energy)
    {
        Assert.DoesNotContain(
            Validate(WithPairEntry(energy)),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidEnergy);
    }

    [Fact]
    public void ValidatorRejectsZeroFrequency()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    ParameterizationTestModels.TransitionAIdValue,
                    [new RuleFrequencyEntry(ParameterizationTestModels.RuleGeometryIdA, new FrequencyHz(0.0))]),
            ],
        };

        var diagnostics = Validate(template);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidFrequency &&
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Path?.Property?.EndsWith("frequencyHz", StringComparison.Ordinal) == true);
    }

    [Theory]
    [InlineData("not-a-stable-id")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ValidatorRejectsMalformedStableId(string energyId)
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    energyId,
                    chiral: false,
                    [ParameterizationTestModels.Entry(
                        ParameterizationTestModels.ParticleA,
                        ParameterizationTestModels.ParticleB,
                        0.0)]),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsDuplicatePairValueKey()
    {
        var entry = ParameterizationTestModels.Entry(
            ParameterizationTestModels.ParticleA,
            ParameterizationTestModels.ParticleB,
            0.0);
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    [entry, entry]),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsDuplicatePairSetIdAcrossStableAndUnstableLists()
    {
        var set = ParameterizationTestModels.CreatePairSet(
            ParameterizationTestModels.PairEnergyIdA,
            chiral: false,
            []);
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets = [set],
            UnstablePairSets = [set],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsConflictingMirroredChiralValues()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: true,
                    [
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleA,
                            ParameterizationTestModels.ParticleB,
                            0.1),
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleB,
                            ParameterizationTestModels.ParticleA,
                            0.2),
                    ]),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey &&
                diagnostic.Message.Contains("chiral", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorAcceptsMirroredChiralValuesWithTheSameEnergy()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: true,
                    [
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleA,
                            ParameterizationTestModels.ParticleB,
                            0.1),
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleB,
                            ParameterizationTestModels.ParticleA,
                            0.1),
                    ]),
            ],
        };

        Assert.DoesNotContain(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorAcceptsRepeatedParticlesInAnOccupationTuple()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            GroupSets =
            [
                new GroupEnergySet(
                    ParameterizationTestModels.ClusterEnergyIdA,
                    ClusterId.Create(ParameterizationTestModels.ClusterIdValue),
                    ParameterizationTestModels.SiteA,
                    [
                        new GroupEnergyEntry(
                            [
                                ParameterizationTestModels.ParticleA,
                                ParameterizationTestModels.ParticleA,
                                ParameterizationTestModels.ParticleB,
                            ],
                            new EnergyEv(0.1)),
                    ]),
            ],
        };

        Assert.Empty(Validate(template));
    }

    [Fact]
    public void ValidatorRejectsDuplicateGroupValueKey()
    {
        var group = new GroupEnergySet(
            ParameterizationTestModels.ClusterEnergyIdA,
            ClusterId.Create(ParameterizationTestModels.ClusterIdValue),
            ParameterizationTestModels.SiteA,
            [
                new GroupEnergyEntry(
                    [ParameterizationTestModels.ParticleA, ParameterizationTestModels.ParticleB],
                    new EnergyEv(0.1)),
                new GroupEnergyEntry(
                    [ParameterizationTestModels.ParticleA, ParameterizationTestModels.ParticleB],
                    new EnergyEv(0.2)),
            ]);
        var template = ParameterizationTestModels.CreateEmptyTemplate() with { GroupSets = [group] };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsDuplicateGroupSetHeader()
    {
        var group = new GroupEnergySet(
            ParameterizationTestModels.ClusterEnergyIdA,
            ClusterId.Create(ParameterizationTestModels.ClusterIdValue),
            ParameterizationTestModels.SiteA,
            []);
        var template = ParameterizationTestModels.CreateEmptyTemplate() with { GroupSets = [group, group] };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey &&
                diagnostic.Message.Contains("group set", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidatorRejectsDuplicateSiteEnergyOverride()
    {
        var entry = new SiteEnergyOverride(
            ParameterizationTestModels.SiteA,
            ParameterizationTestModels.ParticleA,
            new EnergyEv(0.1));
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            SiteEnergyOverrides = [entry, entry],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsDuplicateRuleKey()
    {
        var rule = new RuleFrequencyEntry(
            ParameterizationTestModels.RuleGeometryIdA,
            new FrequencyHz(1.0e13));
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    ParameterizationTestModels.TransitionAIdValue,
                    [rule, rule]),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsUnknownTransitionKind()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    (TransitionKind)99,
                    ParameterizationTestModels.TransitionAIdValue,
                    []),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsEmptyTransitionId()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            TransitionRuleSets =
            [
                new TransitionRuleSet(TransitionKind.Kinetic, Guid.Empty, []),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey);
    }

    [Fact]
    public void ValidatorRejectsDuplicateTemplateNameCaseInsensitively()
    {
        var project = ParameterizationTestModels.CreateProject(
        [
            ParameterizationTestModels.CreateEmptyTemplate(name: "Ceria parameters"),
            ParameterizationTestModels.CreateEmptyTemplate(
                ParameterizationId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82")),
                "ceria PARAMETERS"),
        ]);

        var diagnostics = ProjectValidator.Validate(project);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.DuplicateName &&
                diagnostic.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ValidatorRejectsEmptyTemplateId()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate(ParameterizationId.Create(Guid.Empty));

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey &&
                diagnostic.Path?.Property == "id");
    }

    [Fact]
    public void ValidatorRejectsOverlongTemplateName()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate(
            name: new string('a', ParameterizationLimits.TemplateNameLength + 1));

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.LimitExceeded &&
                diagnostic.Path?.Property == "name");
    }

    [Fact]
    public void ValidatorRejectsTooManyTemplates()
    {
        var templates = Enumerable.Range(0, ParameterizationLimits.TemplatesPerProject + 1)
            .Select(index => ParameterizationTestModels.CreateEmptyTemplate(
                ParameterizationId.New(),
                $"Template {index}"))
            .ToImmutableArray();

        var diagnostics = ProjectValidator.Validate(ParameterizationTestModels.CreateProject(templates));

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.LimitExceeded &&
                diagnostic.Path?.Domain == "parameterization");
    }

    [Fact]
    public void ValidatorRejectsTooManyPairEntriesBeforeAllocating()
    {
        var entries = Enumerable.Range(0, ParameterizationLimits.PairEntriesPerSet + 1)
            .Select(_ => new PairEnergyEntry(
                ParticleId.New(),
                ParticleId.New(),
                new EnergyEv(0.0)))
            .ToImmutableArray();
        var template = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    entries),
            ],
        };

        Assert.Contains(
            Validate(template),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.LimitExceeded &&
                diagnostic.Path?.Property?.EndsWith("entries", StringComparison.Ordinal) == true);
    }

    private static ParameterizationTemplate WithPairEntry(double energy) =>
        ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    [ParameterizationTestModels.Entry(
                        ParameterizationTestModels.ParticleA,
                        ParameterizationTestModels.ParticleB,
                        energy)]),
            ],
        };

    private static ImmutableArray<Diagnostic> Validate(ParameterizationTemplate template) =>
        ProjectValidator.Validate(ParameterizationTestModels.CreateProject(template));
}
