// SPDX-License-Identifier: MIT

using System.Text.Json;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Lattice;
using Mocassin.Core.Project;
using Mocassin.Core.Tests.TestSupport;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Lattice;

[Trait("Tier", "PR-fast")]
public sealed class LatticeModelTests
{
    [Fact]
    public void ValidLatticeModelHasNoLatticeOrProjectDiagnostics()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        Assert.Empty(LatticeModelValidator.Validate(project.Model));
        Assert.Empty(ProjectValidator.Validate(project));
    }

    [Fact]
    public void DefaultBuildingBlockUsesFirstSetParticleAndVoidOnUnstableSites()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        var buildingBlock = project.DefaultBuildingBlock;

        Assert.Equal(3, buildingBlock.Length);
        Assert.Equal(
            new BuildingBlockEntry(LatticeTestModels.CeSite.Id, LatticeTestModels.Ce4Plus.Id),
            buildingBlock.Single(static entry => entry.SiteId == LatticeTestModels.CeSite.Id));
        Assert.Equal(
            new BuildingBlockEntry(LatticeTestModels.OSite.Id, LatticeTestModels.O2Minus.Id),
            buildingBlock.Single(static entry => entry.SiteId == LatticeTestModels.OSite.Id));
        Assert.Equal(
            new BuildingBlockEntry(LatticeTestModels.UnstableSite.Id, ParticleId.Void),
            buildingBlock.Single(static entry => entry.SiteId == LatticeTestModels.UnstableSite.Id));
    }

    [Fact]
    public void BuildingBlockSkipsStableSitesWithoutUsableParticleSet()
    {
        var model = LatticeTestModels.CreateLatticeModel();
        var site = LatticeTestModels.CeSite with { ParticleSetId = ParticleSetId.New() };

        var buildingBlock = DefaultBuildingBlockBuilder.Build(model with
        {
            Structure = model.Structure with { Sites = [site] },
        });

        Assert.Empty(buildingBlock);
    }

    [Fact]
    public void ProjectExposesLatticeQueriesThroughIProjectQuery()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        Assert.Contains(typeof(ILatticeQuery), typeof(IProjectQuery).GetInterfaces());
        Assert.Contains(typeof(ILatticeQuery), typeof(ProjectModel).GetInterfaces());
        Assert.Equal(3, project.OccupationExchanges.Length);
        Assert.Equal(2, project.Dopings.Length);
        Assert.Equal(
            LatticeTestModels.CeReduction,
            project.Find(LatticeTestModels.CeReduction.Id));
        Assert.Equal(
            LatticeTestModels.CeriaReductionDoping,
            project.Find(LatticeTestModels.CeriaReductionDoping.Id));
        Assert.Null(project.Find(OccupationExchangeId.New()));
        Assert.Null(project.Find(DopingId.New()));
    }

    [Fact]
    public void LatticeSectionsRoundTripThroughProjectJson()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(3, loaded.Model.OccupationExchanges.Length);
        Assert.Equal(2, loaded.Model.Dopings.Length);
        Assert.Equal(
            LatticeTestModels.CeriaReductionDoping,
            loaded.Model.Dopings.Single(static doping => doping.Id == LatticeTestModels.CeriaReductionDoping.Id));
    }

    [Fact]
    public void EmptyLatticeSectionsAreOmittedAndBuildingBlockIsNotPersisted()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel()));
        using var document = JsonDocument.Parse(json);
        var model = document.RootElement.GetProperty("model");

        Assert.True(model.TryGetProperty("occupationExchanges", out _));
        Assert.True(model.TryGetProperty("dopings", out _));
        Assert.False(model.TryGetProperty("defaultBuildingBlock", out _));
        Assert.False(model.TryGetProperty("buildingBlock", out _));

        var emptyJson = ProjectCanonicalizer.ToCanonicalJson(LatticeTestModels.CreateProject());
        using var emptyDocument = JsonDocument.Parse(emptyJson);
        var emptyModel = emptyDocument.RootElement.GetProperty("model");
        Assert.False(emptyModel.TryGetProperty("occupationExchanges", out _));
        Assert.False(emptyModel.TryGetProperty("dopings", out _));
    }

    [Fact]
    public void CanonicalModelSortsExchangesByIdAndDopingsByPriority()
    {
        var model = LatticeTestModels.CreateLatticeModel();
        var shuffled = model with
        {
            OccupationExchanges = [.. model.OccupationExchanges.Reverse()],
            Dopings = [.. model.Dopings.Reverse()],
        };

        var canonical = ProjectCanonicalizer.ToCanonicalJson(
            LatticeTestModels.CreateProject(shuffled));
        using var document = JsonDocument.Parse(canonical);
        var modelElement = document.RootElement.GetProperty("model");
        var exchangeIds = modelElement.GetProperty("occupationExchanges")
            .EnumerateArray()
            .Select(static item => item.GetProperty("id").GetString())
            .ToArray();
        var priorities = modelElement.GetProperty("dopings")
            .EnumerateArray()
            .Select(static item => item.GetProperty("priority").GetInt32())
            .ToArray();

        Assert.Equal(exchangeIds.Order(StringComparer.Ordinal), exchangeIds);
        Assert.Equal(priorities.Order(), priorities);
    }

    [Fact]
    public void LatticeEditsChangeModelAndDocumentHashes()
    {
        var project = LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel());
        var edited = project with
        {
            Model = project.Model with
            {
                Dopings = [project.Model.Dopings[0] with { Name = "Reduced ceria" }, project.Model.Dopings[1]],
            },
        };

        Assert.NotEqual(ProjectHasher.Compute(project), ProjectHasher.Compute(edited));
        Assert.NotEqual(ProjectHasher.ComputeDocument(project), ProjectHasher.ComputeDocument(edited));
    }

    [Fact]
    public void LatticeOnlyModelLoadsAndValidatesIndependently()
    {
        var model = LatticeTestModels.CreateLatticeOnlyModel();

        var diagnostics = LatticeModelValidator.Validate(model);

        Assert.Empty(diagnostics);
        Assert.Equal(2, model.OccupationExchanges.Length);
        Assert.Single(model.Dopings);
        Assert.Equal(3, DefaultBuildingBlockBuilder.Build(model).Length);
    }

    [Fact]
    public void UnknownLatticePropertiesAreRejected()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(
            LatticeTestModels.CreateProject(LatticeTestModels.CreateLatticeModel()));
        var modified = json.Replace(
            "\"occupationExchanges\":[",
            "\"occupationExchanges\":[{\"id\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"x\",\"siteId\":\"00000000-0000-0000-0000-000000000000\",\"sourceParticleId\":\"00000000-0000-0000-0000-000000000000\",\"targetParticleId\":\"00000000-0000-0000-0000-000000000000\",\"unknown\":1},",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(modified));
    }

    [Fact]
    public void DuplicateLatticeIdentifiersAreReportedByProjectValidator()
    {
        var model = LatticeTestModels.CreateLatticeModel();
        var duplicateExchange = LatticeTestModels.CeIsovalent with
        {
            Id = LatticeTestModels.CeReduction.Id,
        };

        var diagnostics = ProjectValidator.Validate(LatticeTestModels.CreateProject(model with
        {
            OccupationExchanges = [.. model.OccupationExchanges, duplicateExchange],
        }));

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.DuplicateId);
    }

    [Fact]
    public void EmptyModelHasNoLatticeDiagnostics()
    {
        Assert.Empty(LatticeModelValidator.Validate(ModelDefinition.Empty));
        Assert.Empty(LatticeTestModels.CreateProject().DefaultBuildingBlock);
    }
}
