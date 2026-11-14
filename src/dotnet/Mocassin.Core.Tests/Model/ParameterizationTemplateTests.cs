// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FsCheck.Xunit;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using Mocassin.Core.Units;

namespace Mocassin.Core.Tests.Model;

[Trait("Tier", "PR-fast")]
public sealed class ParameterizationTemplateTests
{
    [Fact]
    public void ConstructorUsesEmptyArraysAndDefaults()
    {
        var template = ParameterizationTestModels.CreateEmptyTemplate();

        Assert.Empty(template.StablePairSets);
        Assert.Empty(template.UnstablePairSets);
        Assert.Empty(template.GroupSets);
        Assert.Empty(template.SiteEnergyOverrides);
        Assert.Empty(template.TransitionRuleSets);
        Assert.Empty(new PairEnergySet(ParameterizationTestModels.PairEnergyIdA, default, default, 0, false).Entries);
        Assert.Empty(new GroupEnergySet(
            ParameterizationTestModels.ClusterEnergyIdA,
            default,
            default).Entries);
        Assert.Empty(new GroupEnergyEntry().OccupationParticleIds);
        Assert.Empty(new TransitionRuleSet(TransitionKind.Kinetic, Guid.Empty).Rules);
    }

    [Fact]
    public void CanonicalJsonUsesThePinnedFieldNamesAndOrder()
    {
        var template = TemplateJson(ParameterizationTestModels.CreatePopulatedTemplate());

        Assert.Equal(
            [
                "id",
                "name",
                "modelSignature",
                "createdUtc",
                "stablePairSets",
                "unstablePairSets",
                "groupSets",
                "siteEnergyOverrides",
                "transitionRuleSets",
            ],
            template.EnumerateObject().Select(static property => property.Name));

        var pairSet = template.GetProperty("stablePairSets")[0];
        Assert.Equal(
            ["energyId", "centerSiteId", "partnerSiteId", "distanceBucketFm", "isChiral", "entries"],
            pairSet.EnumerateObject().Select(static property => property.Name));
        Assert.Equal(
            ["centerParticleId", "partnerParticleId", "energyEv"],
            pairSet.GetProperty("entries")[0].EnumerateObject().Select(static property => property.Name));

        var groupSet = template.GetProperty("groupSets")[0];
        Assert.Equal(
            ["clusterEnergyId", "clusterId", "centerSiteId", "entries"],
            groupSet.EnumerateObject().Select(static property => property.Name));
        Assert.Equal(
            ["occupationParticleIds", "energyEv"],
            groupSet.GetProperty("entries")[0].EnumerateObject().Select(static property => property.Name));

        Assert.Equal(
            ["siteId", "particleId", "energyEv"],
            template.GetProperty("siteEnergyOverrides")[0].EnumerateObject().Select(static property => property.Name));

        var ruleSet = template.GetProperty("transitionRuleSets")[0];
        Assert.Equal(
            ["transitionKind", "transitionId", "rules"],
            ruleSet.EnumerateObject().Select(static property => property.Name));
        Assert.Equal("kinetic", ruleSet.GetProperty("transitionKind").GetString());
        Assert.Equal(
            ["ruleGeometryId", "frequencyHz"],
            ruleSet.GetProperty("rules")[0].EnumerateObject().Select(static property => property.Name));
    }

    [Fact]
    public void CanonicalJsonSortsEveryCollectionByItsStableKey()
    {
        var template = TemplateJson(ParameterizationTestModels.CreatePopulatedTemplate());

        Assert.Equal(
            [
                ParameterizationTestModels.PairEnergyIdA,
                ParameterizationTestModels.PairEnergyIdB,
                ParameterizationTestModels.PairEnergyIdC,
            ],
            template.GetProperty("stablePairSets")
                .EnumerateArray()
                .Select(static set => set.GetProperty("energyId").GetString()));
        Assert.Equal(
            [
                ParameterizationTestModels.ParticleA.Value.ToString("D"),
                ParameterizationTestModels.ParticleB.Value.ToString("D"),
            ],
            template.GetProperty("stablePairSets")[0]
                .GetProperty("entries")
                .EnumerateArray()
                .Select(static entry => entry.GetProperty("centerParticleId").GetString()));
        Assert.Equal(
            [
                ParameterizationTestModels.ParticleA.Value.ToString("D"),
                ParameterizationTestModels.ParticleA.Value.ToString("D"),
                ParameterizationTestModels.ParticleB.Value.ToString("D"),
            ],
            template.GetProperty("groupSets")[0].GetProperty("entries")[0]
                .GetProperty("occupationParticleIds")
                .EnumerateArray()
                .Select(static id => id.GetString()));
        Assert.Equal(
            [
                ParameterizationTestModels.SiteA.Value.ToString("D"),
                ParameterizationTestModels.SiteB.Value.ToString("D"),
            ],
            template.GetProperty("siteEnergyOverrides")
                .EnumerateArray()
                .Select(static entry => entry.GetProperty("siteId").GetString()));
        Assert.Equal(
            ["kinetic", "metropolis"],
            template.GetProperty("transitionRuleSets")
                .EnumerateArray()
                .Select(static set => set.GetProperty("transitionKind").GetString()));
        Assert.Equal(
            [
                ParameterizationTestModels.RuleGeometryIdA,
                ParameterizationTestModels.RuleGeometryIdB,
            ],
            template.GetProperty("transitionRuleSets")[0]
                .GetProperty("rules")
                .EnumerateArray()
                .Select(static rule => rule.GetProperty("ruleGeometryId").GetString()));
    }

    [Fact]
    public void CanonicalDocumentIgnoresAuthoringOrder()
    {
        var template = ParameterizationTestModels.CreatePopulatedTemplate();
        var reversed = template with
        {
            StablePairSets = [.. template.StablePairSets.Reverse()],
            UnstablePairSets = [.. template.UnstablePairSets.Reverse()],
            GroupSets = [.. template.GroupSets.Reverse()],
            SiteEnergyOverrides = [.. template.SiteEnergyOverrides.Reverse()],
            TransitionRuleSets = [.. template.TransitionRuleSets.Reverse()],
        };

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(template)),
            ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(reversed)));
        Assert.Equal(
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(template)),
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(reversed)));
    }

    [Fact]
    public void CanonicalJsonFoldsMirroredChiralDuplicatesWithTheSameValue()
    {
        var mirrored = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: true,
                    [
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleB,
                            ParameterizationTestModels.ParticleA,
                            0.1),
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleA,
                            ParameterizationTestModels.ParticleB,
                            0.1),
                    ]),
            ],
        };
        var folded = mirrored with
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
                    ]),
            ],
        };

        var entries = TemplateJson(mirrored)
            .GetProperty("stablePairSets")[0]
            .GetProperty("entries");
        Assert.Single(entries.EnumerateArray());
        Assert.Equal(
            ParameterizationTestModels.ParticleA.Value.ToString("D"),
            entries[0].GetProperty("centerParticleId").GetString());
        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(folded)),
            ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(mirrored)));
        Assert.Equal(
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(folded)),
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(mirrored)));
    }

    [Fact]
    public void CanonicalJsonKeepsConflictingMirroredChiralValuesForValidation()
    {
        var conflicting = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: true,
                    [
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleB,
                            ParameterizationTestModels.ParticleA,
                            0.1),
                        ParameterizationTestModels.Entry(
                            ParameterizationTestModels.ParticleA,
                            ParameterizationTestModels.ParticleB,
                            0.2),
                    ]),
            ],
        };

        var entries = TemplateJson(conflicting)
            .GetProperty("stablePairSets")[0]
            .GetProperty("entries");
        Assert.Equal(2, entries.GetArrayLength());
        Assert.Contains(
            ProjectValidator.Validate(ParameterizationTestModels.CreateProject(conflicting)),
            static diagnostic => diagnostic.Code == ParameterizationDiagnosticCodes.InvalidKey &&
                diagnostic.Message.Contains("chiral", StringComparison.Ordinal));

        var reloaded = ProjectJson.Deserialize(
            ProjectCanonicalizer.ToCanonicalUtf8(ParameterizationTestModels.CreateProject(conflicting)));
        Assert.Equal(2, reloaded.Parameterizations.Single().StablePairSets[0].Entries.Length);
    }

    [Property(MaxTest = 50)]
    public bool CanonicalDocumentIsInvariantUnderRandomEntryOrder(int seed)
    {
        var random = new Random(seed & 0x7FFFFFFF);
        var template = ParameterizationTestModels.CreatePopulatedTemplate();
        var shuffled = template with
        {
            StablePairSets =
            [
                .. template.StablePairSets
                    .OrderBy(_ => random.Next())
                    .Select(set => set with { Entries = [.. set.Entries.OrderBy(_ => random.Next())] }),
            ],
            UnstablePairSets = [.. template.UnstablePairSets.OrderBy(_ => random.Next())],
            GroupSets =
            [
                .. template.GroupSets
                    .OrderBy(_ => random.Next())
                    .Select(set => set with { Entries = [.. set.Entries.OrderBy(_ => random.Next())] }),
            ],
            SiteEnergyOverrides = [.. template.SiteEnergyOverrides.OrderBy(_ => random.Next())],
            TransitionRuleSets =
            [
                .. template.TransitionRuleSets
                    .OrderBy(_ => random.Next())
                    .Select(set => set with { Rules = [.. set.Rules.OrderBy(_ => random.Next())] }),
            ],
        };

        return string.Equals(
                ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(template)),
                ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(shuffled)),
                StringComparison.Ordinal) &&
            string.Equals(
                ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(template)),
                ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(shuffled)),
                StringComparison.Ordinal);
    }

    [Property(MaxTest = 50)]
    public bool CanonicalRoundTripIsIdempotentForShuffledTemplates(int seed)
    {
        var random = new Random(seed & 0x7FFFFFFF);
        var original = ParameterizationTestModels.CreatePopulatedTemplate();
        var template = original with
        {
            StablePairSets = [.. original.StablePairSets.OrderBy(_ => random.Next())],
            UnstablePairSets = [.. original.UnstablePairSets.OrderBy(_ => random.Next())],
            GroupSets =
            [
                .. original.GroupSets
                    .OrderBy(_ => random.Next())
                    .Select(set => set with { Entries = [.. set.Entries.OrderBy(_ => random.Next())] }),
            ],
            SiteEnergyOverrides = [.. original.SiteEnergyOverrides.OrderBy(_ => random.Next())],
            TransitionRuleSets =
            [
                .. original.TransitionRuleSets
                    .OrderBy(_ => random.Next())
                    .Select(set => set with { Rules = [.. set.Rules.OrderBy(_ => random.Next())] }),
            ],
        };
        var first = ProjectCanonicalizer.ToCanonicalUtf8(
            ParameterizationTestModels.CreateProject(template));
        var second = ProjectCanonicalizer.ToCanonicalUtf8(
            ProjectJson.Deserialize(first));

        return first.AsSpan().SequenceEqual(second);
    }

    [Fact]
    public void NegativeZeroIsNormalizedAtWriteTime()
    {
        var negative = ParameterizationTestModels.CreateEmptyTemplate() with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    [ParameterizationTestModels.Entry(
                        ParameterizationTestModels.ParticleA,
                        ParameterizationTestModels.ParticleB,
                        -0.0)]),
            ],
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    ParameterizationTestModels.TransitionAIdValue,
                    [new RuleFrequencyEntry(ParameterizationTestModels.RuleGeometryIdA, new FrequencyHz(-0.0))]),
            ],
        };
        var positive = negative with
        {
            StablePairSets =
            [
                ParameterizationTestModels.CreatePairSet(
                    ParameterizationTestModels.PairEnergyIdA,
                    chiral: false,
                    [ParameterizationTestModels.Entry(
                        ParameterizationTestModels.ParticleA,
                        ParameterizationTestModels.ParticleB,
                        0.0)]),
            ],
            TransitionRuleSets =
            [
                new TransitionRuleSet(
                    TransitionKind.Kinetic,
                    ParameterizationTestModels.TransitionAIdValue,
                    [new RuleFrequencyEntry(ParameterizationTestModels.RuleGeometryIdA, new FrequencyHz(0.0))]),
            ],
        };

        var negativeJson = ProjectCanonicalizer.ToCanonicalJson(
            ParameterizationTestModels.CreateProject(negative));
        var positiveJson = ProjectCanonicalizer.ToCanonicalJson(
            ParameterizationTestModels.CreateProject(positive));

        Assert.Equal(positiveJson, negativeJson);
        Assert.DoesNotContain("-0", negativeJson, StringComparison.Ordinal);
        Assert.Equal(
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(negative)),
            ProjectHasher.ComputeDocument(ParameterizationTestModels.CreateProject(positive)));
    }

    [Fact]
    public void RoundTripPreservesTemplateValuesAndCanonicalBytes()
    {
        var project = ParameterizationTestModels.CreateProject(
            ParameterizationTestModels.CreatePopulatedTemplate());
        var canonical = ProjectCanonicalizer.ToCanonicalUtf8(project);
        var loaded = ProjectJson.Deserialize(canonical);
        var template = loaded.Parameterizations.Single();

        Assert.Equal(ProjectCanonicalizer.ToCanonicalJson(project), ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(3, template.StablePairSets.Length);
        Assert.Single(template.UnstablePairSets);
        Assert.Single(template.GroupSets);
        Assert.Equal(2, template.SiteEnergyOverrides.Length);
        Assert.Equal(2, template.TransitionRuleSets.Length);
        Assert.Equal(
            new EnergyEv(-0.5),
            template.StablePairSets.Single(static set =>
                set.EnergyId == ParameterizationTestModels.PairEnergyIdA).Entries[0].Energy);
        Assert.Equal(
            new EnergyEv(0.48),
            template.GroupSets[0].Entries[0].Energy);
        Assert.Equal(
            new FrequencyHz(1.0e13),
            template.TransitionRuleSets[0].Rules[0].Frequency);
        Assert.Equal(
            new FrequencyHz(2.0e13),
            template.TransitionRuleSets[0].Rules.Single(static rule =>
                rule.RuleGeometryId == ParameterizationTestModels.RuleGeometryIdB).Frequency);
    }

    [Fact]
    public void MissingValueArraysLoadAsEmpty()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(
            ParameterizationTestModels.CreateProject(
                ParameterizationTestModels.CreatePopulatedTemplate())))!.AsObject();
        var template = root["parameterizations"]![0]!.AsObject();
        template.Remove("stablePairSets");
        template.Remove("unstablePairSets");
        template.Remove("groupSets");
        template.Remove("siteEnergyOverrides");
        template.Remove("transitionRuleSets");

        var loaded = ProjectJson.Deserialize(root.ToJsonString());
        var loadedTemplate = loaded.Parameterizations.Single();

        Assert.Empty(loadedTemplate.StablePairSets);
        Assert.Empty(loadedTemplate.UnstablePairSets);
        Assert.Empty(loadedTemplate.GroupSets);
        Assert.Empty(loadedTemplate.SiteEnergyOverrides);
        Assert.Empty(loadedTemplate.TransitionRuleSets);
        Assert.DoesNotContain(
            ProjectValidator.Validate(loaded),
            static diagnostic => diagnostic.Code is "PRM013" or "PRM014");
    }

    [Fact]
    public void UnknownNestedPropertyIsRejected()
    {
        AssertRejected(root => root["parameterizations"]![0]!["unexpected"] = true);
        AssertRejected(root => root["parameterizations"]![0]!["stablePairSets"]![0]!["unexpected"] = true);
        AssertRejected(root => root["parameterizations"]![0]!["stablePairSets"]![0]!["entries"]![0]!["unexpected"] = true);
        AssertRejected(root => root["parameterizations"]![0]!["groupSets"]![0]!["entries"]![0]!["unexpected"] = true);
        AssertRejected(root => root["parameterizations"]![0]!["siteEnergyOverrides"]![0]!["unexpected"] = true);
        AssertRejected(root => root["parameterizations"]![0]!["transitionRuleSets"]![0]!["rules"]![0]!["unexpected"] = true);
    }

    [Fact]
    public void CanonicalJsonIsCultureIndependent()
    {
        var project = ParameterizationTestModels.CreateProject(
            ParameterizationTestModels.CreatePopulatedTemplate());
        var invariant = ProjectCanonicalizer.ToCanonicalJson(project);
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var german = ProjectCanonicalizer.ToCanonicalJson(project);

            Assert.Equal(invariant, german);
            Assert.Contains("\"energyEv\":-0.5", german, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static void AssertRejected(Action<JsonObject> mutate)
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(
            ParameterizationTestModels.CreateProject(
                ParameterizationTestModels.CreatePopulatedTemplate())))!.AsObject();
        mutate(root);
        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    private static JsonElement TemplateJson(ParameterizationTemplate template)
    {
        using var document = JsonDocument.Parse(
            ProjectCanonicalizer.ToCanonicalJson(ParameterizationTestModels.CreateProject(template)));
        return document.RootElement.GetProperty("parameterizations")[0].Clone();
    }
}
