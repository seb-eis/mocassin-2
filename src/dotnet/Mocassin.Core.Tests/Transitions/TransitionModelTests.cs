// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionModelTests
{
    [Fact]
    public void EmptyModelHasEmptyTransitionSections()
    {
        var model = new ModelDefinition();

        Assert.Empty(model.StateChanges);
        Assert.Empty(model.StateChangeGroups);
        Assert.Empty(model.Chains);
        Assert.Empty(model.KineticTransitions);
        Assert.Empty(model.MetropolisTransitions);
        Assert.Empty(ModelDefinition.Empty.StateChanges);
        Assert.Empty(ModelDefinition.Empty.StateChangeGroups);
        Assert.Empty(ModelDefinition.Empty.Chains);
        Assert.Empty(ModelDefinition.Empty.KineticTransitions);
        Assert.Empty(ModelDefinition.Empty.MetropolisTransitions);
    }

    [Fact]
    public void EmptyModelObjectLoadsWithEmptyTransitionSections()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(TransitionTestModels.CreateProject()))!.AsObject();
        root["model"] = new JsonObject();

        var loaded = ProjectJson.Deserialize(root.ToJsonString());

        Assert.Empty(loaded.Model.StateChanges);
        Assert.Empty(loaded.Model.StateChangeGroups);
        Assert.Empty(loaded.Model.Chains);
        Assert.Empty(loaded.Model.KineticTransitions);
        Assert.Empty(loaded.Model.MetropolisTransitions);
        Assert.Equal(StructureModel.Default.Name, loaded.Model.Structure.Name);
    }

    [Fact]
    public void EmptyModelRoundTripsThroughProjectJson()
    {
        AssertCanonicalRoundTrip(TransitionTestModels.CreateProject(TransitionTestModels.CreateEmptyModel()));
    }

    [Fact]
    public void TransitionOnlyModelRoundTripsThroughProjectJson()
    {
        AssertCanonicalRoundTrip(TransitionTestModels.CreateProject(TransitionTestModels.CreateTransitionOnlyModel()));
    }

    [Fact]
    public void CombinedModelRoundTripsThroughProjectJson()
    {
        AssertCanonicalRoundTrip(TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel()));
    }

    [Fact]
    public void EmptyTransitionSectionsAreOmittedFromCanonicalModel()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            TransitionTestModels.CreateProject(TransitionTestModels.CreateEmptyModel()));
        using var document = JsonDocument.Parse(json);
        var model = document.RootElement.GetProperty("model");

        Assert.Equal(
            ["particles", "particleSets", "structure"],
            model.EnumerateObject().Select(static property => property.Name));
    }

    [Fact]
    public void CanonicalModelSortsTransitionCollectionsById()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var shuffled = model with
        {
            StateChanges = [.. model.StateChanges.Reverse()],
            StateChangeGroups = [.. model.StateChangeGroups.Reverse()],
            Chains = [.. model.Chains.Reverse()],
            KineticTransitions = [.. model.KineticTransitions.Reverse()],
            MetropolisTransitions = [.. model.MetropolisTransitions.Reverse()],
        };

        Assert.Equal(
            ProjectHasher.Compute(TransitionTestModels.CreateProject(model)),
            ProjectHasher.Compute(TransitionTestModels.CreateProject(shuffled)));

        var json = ProjectCanonicalizer.ToCanonicalJson(TransitionTestModels.CreateProject(shuffled));
        using var document = JsonDocument.Parse(json);
        var modelElement = document.RootElement.GetProperty("model");

        AssertSortedById(modelElement, "stateChanges");
        AssertSortedById(modelElement, "stateChangeGroups");
        AssertSortedById(modelElement, "chains");
        AssertSortedById(modelElement, "kineticTransitions");
        AssertSortedById(modelElement, "metropolisTransitions");
    }

    [Fact]
    public void ReorderingSemanticTransitionArraysChangesCanonicalOutput()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var canonical = ProjectCanonicalizer.ToCanonicalJson(
            TransitionTestModels.CreateProject(model));

        var reorderedGroup = model.StateChangeGroups[0] with
        {
            StateChangeIds = [TransitionTestModels.ForwardStateChange.Id, TransitionTestModels.ReverseStateChange.Id],
        };
        var reorderedReferencePath = model.KineticTransitions[0] with
        {
            ReferencePath = [.. model.KineticTransitions[0].ReferencePath.Reverse()],
        };
        var reorderedChain = model.Chains[0] with
        {
            GroupIds = [.. model.Chains[0].GroupIds.Reverse()],
        };

        var reorderedGroups = model with
        {
            StateChangeGroups = [reorderedGroup, .. model.StateChangeGroups.Skip(1)],
        };
        var reorderedPaths = model with { KineticTransitions = [reorderedReferencePath] };
        var reorderedChains = model with { Chains = [reorderedChain, .. model.Chains.Skip(1)] };

        Assert.NotEqual(
            canonical,
            ProjectCanonicalizer.ToCanonicalJson(TransitionTestModels.CreateProject(reorderedGroups)));
        Assert.NotEqual(
            canonical,
            ProjectCanonicalizer.ToCanonicalJson(TransitionTestModels.CreateProject(reorderedPaths)));
        Assert.NotEqual(
            canonical,
            ProjectCanonicalizer.ToCanonicalJson(TransitionTestModels.CreateProject(reorderedChains)));
    }

    [Fact]
    public void TransitionEnumsUseCamelCaseStrings()
    {
        Assert.Equal("\"metropolis2\"", JsonSerializer.Serialize(TransitionPattern.Metropolis2));
        Assert.Equal("\"migration3\"", JsonSerializer.Serialize(TransitionPattern.Migration3));
        Assert.Equal("\"interstitialcy5\"", JsonSerializer.Serialize(TransitionPattern.Interstitialcy5));
        Assert.Equal("\"interstitialcy7\"", JsonSerializer.Serialize(TransitionPattern.Interstitialcy7));
        Assert.Equal("\"vehicle5\"", JsonSerializer.Serialize(TransitionPattern.Vehicle5));
        Assert.Equal("\"vehicle6\"", JsonSerializer.Serialize(TransitionPattern.Vehicle6));
        Assert.Equal("\"custom\"", JsonSerializer.Serialize(TransitionPattern.Custom));
        Assert.Equal("\"dynamic\"", JsonSerializer.Serialize(TransitionLinker.Dynamic));
        Assert.Equal("\"static\"", JsonSerializer.Serialize(TransitionLinker.Static));
        Assert.Equal("\"kinetic\"", JsonSerializer.Serialize(TransitionKind.Kinetic));
        Assert.Equal("\"metropolis\"", JsonSerializer.Serialize(TransitionKind.Metropolis));
    }

    [Fact]
    public void TransitionEnumsRejectUnknownNumericAndEmptyStrings()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionPattern>("\"unknown\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionPattern>("\"0\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionPattern>("0"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionPattern>("\"\""));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionLinker>("\"unknown\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionLinker>("\"1\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionKind>("\"unknown\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionKind>("\"1\""));
    }

    [Fact]
    public void KineticReferencePathUsesThreeNumberArrays()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel()));
        using var document = JsonDocument.Parse(json);
        var path = document.RootElement
            .GetProperty("model")
            .GetProperty("kineticTransitions")[0]
            .GetProperty("referencePath");

        Assert.Equal(JsonValueKind.Array, path.ValueKind);
        Assert.Equal(3, path.GetArrayLength());
        foreach (var point in path.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Array, point.ValueKind);
            Assert.Equal(3, point.GetArrayLength());
            Assert.All(point.EnumerateArray(), static value => Assert.Equal(JsonValueKind.Number, value.ValueKind));
        }

        var expected = TransitionTestModels.KineticTransition.ReferencePath;
        var actual = path.EnumerateArray()
            .Select(static point => new Fractional3(
                point[0].GetDouble(),
                point[1].GetDouble(),
                point[2].GetDouble()))
            .ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TransitionRefRoundTripsAndKeepsKindDistinct()
    {
        var kineticId = KineticTransitionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f50"));
        var metropolisId = MetropolisTransitionId.Create(kineticId.Value);
        var kineticRef = TransitionRef.From(kineticId);
        var metropolisRef = TransitionRef.From(metropolisId);

        Assert.True(kineticRef.IsKinetic);
        Assert.False(metropolisRef.IsKinetic);
        Assert.NotEqual(kineticRef, metropolisRef);

        var kineticJson = JsonSerializer.Serialize(kineticRef);
        var metropolisJson = JsonSerializer.Serialize(metropolisRef);
        Assert.Equal($$"""{"kind":"kinetic","id":"{{kineticId}}"}""", kineticJson);
        Assert.Equal($$"""{"kind":"metropolis","id":"{{metropolisId}}"}""", metropolisJson);
        Assert.Equal(kineticRef, JsonSerializer.Deserialize<TransitionRef>(kineticJson));
        Assert.Equal(metropolisRef, JsonSerializer.Deserialize<TransitionRef>(metropolisJson));
    }

    [Fact]
    public void TransitionRefRejectsUnknownMalformedAndDuplicateData()
    {
        var id = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5f50");

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"kind":"unknown","id":"{{id}}"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"kind":0,"id":"{{id}}"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"kind":"kinetic","id":"not-a-uuid"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"kind":"kinetic","id":"{{id}}","unexpected":true}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"kind":"kinetic","kind":"metropolis","id":"{{id}}"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            """{"kind":"kinetic"}"""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TransitionRef>(
            $$"""{"id":"{{id}}"}"""));
    }

    [Fact]
    public void TransitionPreviewDataIsNotPersisted()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            TransitionTestModels.CreateProject(TransitionTestModels.CreateCombinedModel()));
        using var document = JsonDocument.Parse(json);
        var model = document.RootElement.GetProperty("model");
        var propertyNames = model.EnumerateObject().Select(static property => property.Name).ToArray();

        Assert.Equal(
            [
                "particles",
                "particleSets",
                "structure",
                "stateChanges",
                "stateChangeGroups",
                "chains",
                "kineticTransitions",
                "metropolisTransitions",
            ],
            propertyNames);
        Assert.DoesNotContain(propertyNames, static name => name.Contains("preview", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("resolved", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("matrix", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("transitions", propertyNames);
    }

    private static void AssertCanonicalRoundTrip(ProjectModel project)
    {
        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
    }

    private static void AssertSortedById(JsonElement model, string propertyName)
    {
        var ids = model.GetProperty(propertyName)
            .EnumerateArray()
            .Select(static item => item.GetProperty("id").GetString())
            .ToArray();

        Assert.Equal(ids.Order(StringComparer.Ordinal), ids);
    }
}
