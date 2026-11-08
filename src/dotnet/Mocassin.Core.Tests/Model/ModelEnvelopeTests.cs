// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mocassin.Core.Diagnostics;
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
public sealed class ModelEnvelopeTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        CultureInfo.InvariantCulture);

    private static readonly Guid projectId = Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e90");

    [Fact]
    public void EmptyModelUsesDefaultStructure()
    {
        var model = ModelDefinition.Empty;

        Assert.Empty(model.Particles);
        Assert.Empty(model.ParticleSets);
        Assert.Same(StructureModel.Default, model.Structure);
        Assert.Equal("New Structure", model.Structure.Name);
        Assert.Equal(1, model.Structure.SpaceGroup.Number);
        Assert.Equal(1.0, model.Structure.Cell.A.Value);
        Assert.Equal(90.0, model.Structure.Cell.Alpha.Value);
    }

    [Fact]
    public void EmptyModelDocumentLoadsAsDefault()
    {
        var bytes = File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", "project-minimal.mocprj"));

        var project = ProjectJson.Deserialize(bytes);

        Assert.Empty(project.Model.Particles);
        Assert.Empty(project.Model.ParticleSets);
        Assert.Equal(StructureModel.Default.Name, project.Model.Structure.Name);
        Assert.Empty(project.Model.Structure.Sites);
        Assert.Empty(project.Model.Structure.DummyPositions);
    }

    [Fact]
    public void ModelRoundTripsThroughProjectJson()
    {
        var project = CreateProject(CreateModel());

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(project));

        Assert.Equal(
            ProjectCanonicalizer.ToCanonicalJson(project),
            ProjectCanonicalizer.ToCanonicalJson(loaded));
        Assert.Equal(3, loaded.Model.Particles.Length);
        Assert.Equal(2, loaded.Model.ParticleSets.Length);
        Assert.Equal("Ceria", loaded.Model.Structure.Name);
        Assert.Equal(3, loaded.Model.Structure.Sites.Length);
        Assert.Single(loaded.Model.Structure.DummyPositions);
    }

    [Fact]
    public void PersistedJsonUsesArraysCamelCaseEnumsAndNoIsVoid()
    {
        var json = ProjectCanonicalizer.ToCanonicalJson(CreateProject(CreateModel()));
        using var document = JsonDocument.Parse(json);
        var model = document.RootElement.GetProperty("model");
        var structure = model.GetProperty("structure");
        var firstSite = structure.GetProperty("sites")[0];
        var particle = model.GetProperty("particles")[0];

        Assert.Equal(JsonValueKind.Array, firstSite.GetProperty("fractional").ValueKind);
        Assert.Equal(3, firstSite.GetProperty("fractional").GetArrayLength());
        Assert.Equal("stable", firstSite.GetProperty("stability").GetString());
        Assert.Equal("none", structure.GetProperty("spaceGroup").GetProperty("axis").GetString());
        Assert.Equal("none", structure.GetProperty("spaceGroup").GetProperty("origin").GetString());
        Assert.False(particle.TryGetProperty("isVoid", out _));
    }

    [Fact]
    public void CanonicalModelHashIgnoresArrayOrderButPreservesParticleIdOrder()
    {
        var model = CreateModel();
        var shuffled = new ModelDefinition
        {
            Particles = [.. model.Particles.Reverse()],
            ParticleSets = [.. model.ParticleSets.Reverse()],
            Structure = model.Structure with
            {
                Sites = [.. model.Structure.Sites.Reverse()],
                DummyPositions = [.. model.Structure.DummyPositions.Reverse()],
            },
        };

        Assert.Equal(
            ProjectHasher.Compute(CreateProject(model)),
            ProjectHasher.Compute(CreateProject(shuffled)));

        var firstSet = model.ParticleSets[0];
        var reorderedSet = firstSet with { ParticleIds = [.. firstSet.ParticleIds.Reverse()] };
        var reordered = new ModelDefinition
        {
            Particles = model.Particles,
            ParticleSets = [reorderedSet, model.ParticleSets[1]],
            Structure = model.Structure,
        };

        Assert.NotEqual(
            ProjectHasher.Compute(CreateProject(model)),
            ProjectHasher.Compute(CreateProject(reordered)));
    }

    [Fact]
    public void StructureHashIgnoresPositionOrderAndChangesWithStructure()
    {
        var structure = CreateModel().Structure;
        var shuffled = structure with
        {
            Sites = [.. structure.Sites.Reverse()],
            DummyPositions = [.. structure.DummyPositions.Reverse()],
        };

        Assert.Equal(
            ProjectHasher.ComputeStructure(structure),
            ProjectHasher.ComputeStructure(shuffled));
        Assert.NotEqual(
            ProjectHasher.ComputeStructure(structure),
            ProjectHasher.ComputeStructure(structure with { Name = "Changed" }));
    }

    [Fact]
    public void SymmetryMetadataRoundTripsAndChangesOnlyTheDocumentHash()
    {
        var metadata = new ProjectSymmetryMetadata(
            "spglib-2.8.0/catalog-1",
            "1f3bf67d92b9258816ee27af2ef12f0035387fa8bad6527aff86f57a740e3395",
            "symmetry-1");
        var without = CreateProject(CreateModel());
        var withMetadata = CreateProject(CreateModel(), metadata);

        Assert.Equal(ProjectHasher.Compute(without), ProjectHasher.Compute(withMetadata));
        Assert.NotEqual(
            ProjectHasher.ComputeDocument(without),
            ProjectHasher.ComputeDocument(withMetadata));

        var loaded = ProjectJson.Deserialize(ProjectJson.Serialize(withMetadata));
        Assert.Equal(metadata, loaded.Symmetry);
    }

    [Fact]
    public void PartialSymmetryMetadataIsAccepted()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["symmetry"] = new JsonObject { ["symmetryContractVersion"] = "symmetry-1" };

        var loaded = ProjectJson.Deserialize(root.ToJsonString());

        Assert.NotNull(loaded.Symmetry);
        Assert.Null(loaded.Symmetry.SpaceGroupDataVersion);
        Assert.Null(loaded.Symmetry.SpaceGroupDataSha256);
        Assert.Equal("symmetry-1", loaded.Symmetry.SymmetryContractVersion);
    }

    [Fact]
    public void ProjectValidatorReportsParticleAndStructureDiagnostics()
    {
        var model = CreateModel();
        var invalidParticle = model.Particles[0] with { Name = " " };
        var invalidSite = model.Structure.Sites[0] with { ParticleSetId = ParticleSetId.Empty };
        var invalidModel = new ModelDefinition
        {
            Particles = [invalidParticle, .. model.Particles.Skip(1)],
            ParticleSets = model.ParticleSets,
            Structure = model.Structure with { Sites = [invalidSite] },
        };

        var diagnostics = ProjectValidator.Validate(CreateProject(invalidModel));

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidName);
        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.MissingParticleSet);
    }

    [Fact]
    public void ProjectValidatorReportsUnknownAndWrongDomainSiteReferences()
    {
        var model = CreateModel();
        var unknownSite = model.Structure.Sites[0] with { ParticleSetId = ParticleSetId.New() };
        var wrongDomainSite = model.Structure.Sites[0] with
        {
            ParticleSetId = ParticleSetId.Create(model.Particles[0].Id.Value),
        };

        var unknownDiagnostics = ProjectValidator.Validate(CreateProject(new ModelDefinition
        {
            Particles = model.Particles,
            ParticleSets = model.ParticleSets,
            Structure = model.Structure with { Sites = [unknownSite] },
        }));
        var wrongDomainDiagnostics = ProjectValidator.Validate(CreateProject(new ModelDefinition
        {
            Particles = model.Particles,
            ParticleSets = model.ParticleSets,
            Structure = model.Structure with { Sites = [wrongDomainSite] },
        }));

        Assert.Contains(
            unknownDiagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
        Assert.Contains(
            wrongDomainDiagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.WrongDomainReference);
    }

    [Fact]
    public void ValidModelProducesNoDiagnostics()
    {
        Assert.Empty(ProjectValidator.Validate(CreateProject(CreateModel())));
    }

    [Fact]
    public void CanonicalJsonSortsModelArraysById()
    {
        var model = CreateModel();
        var shuffled = new ModelDefinition
        {
            Particles = [.. model.Particles.Reverse()],
            ParticleSets = [.. model.ParticleSets.Reverse()],
            Structure = model.Structure with
            {
                Sites = [.. model.Structure.Sites.Reverse()],
                DummyPositions = [.. model.Structure.DummyPositions.Reverse()],
            },
        };

        using var document = JsonDocument.Parse(ProjectCanonicalizer.ToCanonicalJson(CreateProject(shuffled)));
        var modelElement = document.RootElement.GetProperty("model");

        Assert.Equal(
            modelElement.GetProperty("particles").EnumerateArray().Select(static item => item.GetProperty("id").GetString()),
            modelElement.GetProperty("particles").EnumerateArray()
                .Select(static item => item.GetProperty("id").GetString())
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            modelElement.GetProperty("structure").GetProperty("sites").EnumerateArray()
                .Select(static item => item.GetProperty("id").GetString()),
            modelElement.GetProperty("structure").GetProperty("sites").EnumerateArray()
                .Select(static item => item.GetProperty("id").GetString())
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void UnknownModelAndStructurePropertiesAreRejected()
    {
        var root = JsonNode.Parse(ProjectJson.Serialize(CreateProject()))!.AsObject();
        root["model"] = new JsonObject { ["unexpected"] = 1 };
        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));

        root["model"] = new JsonObject
        {
            ["structure"] = new JsonObject { ["unexpected"] = 1 },
        };
        Assert.Throws<JsonException>(() => ProjectJson.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void Fractional3UsesThreeNumberArrayAndRejectsInvalidInput()
    {
        var value = JsonSerializer.Deserialize<Fractional3>("[0.25,0.5,0.75]");
        Assert.Equal(new Fractional3(0.25, 0.5, 0.75), value);
        Assert.Equal("[0,0.5,0.75]", JsonSerializer.Serialize(new Fractional3(-0.0, 0.5, 0.75)));

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Fractional3>("[0.25,0.5]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Fractional3>("[0.25,0.5,0.75,1]"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Fractional3>("[0.25,0.5,1e400]"));
    }

    [Fact]
    public void EnumsUseCamelCaseStrings()
    {
        Assert.Equal("\"stable\"", JsonSerializer.Serialize(SiteStability.Stable));
        Assert.Equal("\"unstable\"", JsonSerializer.Serialize(SiteStability.Unstable));
        Assert.Equal("\"uniqueAxisB\"", JsonSerializer.Serialize(AxisSetting.UniqueAxisB));
        Assert.Equal("\"originChoice2\"", JsonSerializer.Serialize(OriginChoice.OriginChoice2));
        Assert.Equal(SiteStability.Stable, JsonSerializer.Deserialize<SiteStability>("\"stable\""));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SiteStability>("\"0\""));
    }

    private static ModelDefinition CreateModel()
    {
        var oxygen = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80")),
            "O2-",
            "O",
            new ChargeElementary(-2.0),
            IsVacancy: false);
        var vacancy = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e81")),
            "VO",
            "O",
            new ChargeElementary(0.0),
            IsVacancy: true);
        var cerium = new Particle(
            ParticleId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82")),
            "Ce4+",
            "Ce",
            new ChargeElementary(4.0),
            IsVacancy: false);
        var oxygenSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83")),
            "Oxygen site",
            [oxygen.Id, vacancy.Id]);
        var ceriumSet = new ParticleSet(
            ParticleSetId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e84")),
            "Cerium site",
            [cerium.Id]);
        var structure = StructureModel.Default with
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
            Sites =
            [
                new CellSite(
                    CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e85")),
                    "Ce",
                    Fractional3.Zero,
                    ceriumSet.Id,
                    SiteStability.Stable),
                new CellSite(
                    CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e86")),
                    "O",
                    new Fractional3(0.25, 0.25, 0.25),
                    oxygenSet.Id,
                    SiteStability.Stable),
                new CellSite(
                    CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e87")),
                    "O transition",
                    new Fractional3(0.5, 0.25, 0.25),
                    oxygenSet.Id,
                    SiteStability.Unstable),
            ],
            DummyPositions =
            [
                new CellDummyPosition(
                    CellDummyPositionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e88")),
                    "Center",
                    new Fractional3(0.5, 0.5, 0.5)),
            ],
        };

        return new ModelDefinition
        {
            Particles = [oxygen, vacancy, cerium],
            ParticleSets = [oxygenSet, ceriumSet],
            Structure = structure,
        };
    }

    private static ProjectModel CreateProject(
        ModelDefinition? model = null,
        ProjectSymmetryMetadata? symmetry = null) =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                projectId,
                new string('a', 64),
                "Model envelope",
                null,
                createdUtc,
                createdUtc),
            model ?? ModelDefinition.Empty,
            [],
            [],
            [],
            ViewSettings.Default,
            symmetry);
}
