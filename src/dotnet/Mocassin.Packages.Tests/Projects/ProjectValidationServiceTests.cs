// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Editing;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Particles;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;
using Mocassin.Packages.Projects;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Packages.Tests.Projects;

[Trait("Tier", "PR-fast")]
public sealed class ProjectValidationServiceTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void MatchingSymmetryMetadataProducesNoProj034()
    {
        var project = ReadFixture("project-symmetry.mocprj");

        var diagnostics = ProjectValidationService.Validate(project);

        Assert.DoesNotContain(
            diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch);
    }

    [Fact]
    public void MismatchedSymmetryMetadataProducesProj034Warning()
    {
        var project = ReadFixture("project-symmetry-mismatch.mocprj");

        var warnings = ProjectValidationService.Validate(project)
            .Where(static item => item.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch)
            .ToArray();

        Assert.Equal(3, warnings.Length);
        Assert.All(warnings, static warning => Assert.Equal(DiagnosticSeverity.Warning, warning.Severity));
        Assert.Contains(
            warnings,
            static warning => warning.Path?.Property == "symmetry.spaceGroupDataVersion");
    }

    [Fact]
    public void PartialAndMissingSymmetryMetadataAreAccepted()
    {
        Assert.DoesNotContain(
            ProjectValidationService.Validate(ReadFixture("project-symmetry-partial.mocprj")),
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch);
        Assert.DoesNotContain(
            ProjectValidationService.Validate(ReadFixture("project-minimal.mocprj")),
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch);
    }

    [Fact]
    public void PartialMismatchReportsOnlyThePresentField()
    {
        var project = CreateProject(
            symmetry: new ProjectSymmetryMetadata(
                "spglib-9.9.9/catalog-0",
                SpaceGroupDataSha256: null,
                SymmetryContractVersion: null));

        var diagnostics = ProjectValidationService.Validate(project);

        var warning = Assert.Single(
            diagnostics.Where(
                static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch));
        Assert.Equal("symmetry.spaceGroupDataVersion", warning.Path?.Property);
    }

    [Fact]
    public void ComposedValidationIncludesSymmetryStructureRules()
    {
        var invalid = CreateProject(invalidCell: true);

        var composed = ProjectValidationService.Validate(invalid);
        var core = ProjectValidator.Validate(invalid);

        Assert.Contains(composed, static diagnostic => diagnostic.Code == "STR003");
        Assert.DoesNotContain(core, static diagnostic => diagnostic.Code == "STR003");
    }

    [Fact]
    public void ChargedVacancyAndChargedDefaultProducePar004Warning()
    {
        var project = CreateProject(chargedVacancy: true);

        var diagnostic = Assert.Single(
            ProjectValidationService.Validate(project).Where(
                static item => item.Code == ParticleDiagnosticCodes.ChargedVacancy));

        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("particleSet", diagnostic.Path?.Domain);
    }

    [Fact]
    public void UnchargedVacancyOrUnchargedDefaultProducesNoPar004()
    {
        var unchargedVacancy = CreateProject(chargedVacancy: false);
        var unchargedDefault = CreateProject(defaultUncharged: true);

        Assert.DoesNotContain(
            ProjectValidationService.Validate(unchargedVacancy),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ChargedVacancy);
        Assert.DoesNotContain(
            ProjectValidationService.Validate(unchargedDefault),
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.ChargedVacancy);
    }

    [Fact]
    public async Task ProjectStoreUsesComposedValidatorByDefault()
    {
        using var directory = new TempDirectory();
        var store = new ProjectStore();
        var invalidPath = directory.File("invalid-cell.mocprj");
        var mismatchPath = directory.File("mismatch.mocprj");
        var validPath = directory.File("valid.mocprj");

        await File.WriteAllBytesAsync(
            invalidPath,
            ProjectJson.SerializeToUtf8Bytes(CreateProject(invalidCell: true)));
        await File.WriteAllBytesAsync(
            mismatchPath,
            ProjectJson.SerializeToUtf8Bytes(CreateProject(
                symmetry: new ProjectSymmetryMetadata(
                    "spglib-9.9.9/catalog-0",
                    new string('0', 64),
                    "symmetry-0"))));
        await File.WriteAllBytesAsync(validPath, ProjectJson.SerializeToUtf8Bytes(CreateProject()));

        var invalid = await store.LoadAsync(invalidPath);
        var mismatch = await store.LoadAsync(mismatchPath);
        var valid = await store.LoadAsync(validPath);

        Assert.NotNull(invalid.Project);
        Assert.Contains(invalid.Diagnostics, static diagnostic => diagnostic.Code == "STR003");
        Assert.NotNull(mismatch.Project);
        Assert.Contains(
            mismatch.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.SymmetryMetadataMismatch);
        Assert.NotNull(valid.Project);
        Assert.Empty(valid.Diagnostics);
    }

    [Fact]
    public async Task ProjectStoreAcceptsInjectedValidator()
    {
        using var directory = new TempDirectory();
        var path = directory.File("valid.mocprj");
        await File.WriteAllBytesAsync(path, ProjectJson.SerializeToUtf8Bytes(CreateProject()));
        var injected = new Diagnostic("TEST001", DiagnosticSeverity.Error, "Injected validation failure.");
        var store = new ProjectStore(
            TimeProvider.System,
            new ProjectStoreOptions(),
            new AtomicProjectFileWriter(),
            _ => [injected]);

        var result = await store.LoadAsync(path);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Code == "TEST001");
    }

    [Fact]
    public void ComposedEditorRejectsSymmetryInvalidEdits()
    {
        var project = CreateProject();
        var composedEditor = new ProjectEditor(ProjectValidationService.Validate);
        var coreEditor = new ProjectEditor();
        var invalidCell = project.Model.Structure.Cell with { A = new LengthAngstrom(5.0) };

        var composed = composedEditor.SetCellParameters(project, invalidCell);
        var core = coreEditor.SetCellParameters(project, invalidCell);
        var invalidSpaceGroup = composedEditor.SetSpaceGroup(
            project,
            new SpaceGroupRef(999, AxisSetting.None, OriginChoice.None));

        Assert.False(composed.IsSuccess);
        Assert.Same(project, composed.Project);
        Assert.Contains(composed.Diagnostics, static diagnostic => diagnostic.Code == "STR003");
        Assert.True(core.IsSuccess);
        Assert.False(invalidSpaceGroup.IsSuccess);
        Assert.Contains(invalidSpaceGroup.Diagnostics, static diagnostic => diagnostic.Code == "STR001");
    }

    private static ProjectModel ReadFixture(string fileName)
    {
        var bytes = File.ReadAllBytes(RepositoryPaths.Combine("tests", "contracts", fileName));
        return ProjectJson.Deserialize(bytes);
    }

    private static ProjectModel CreateProject(
        bool invalidCell = false,
        bool chargedVacancy = false,
        bool defaultUncharged = false,
        ProjectSymmetryMetadata? symmetry = null)
    {
        var oxygen = CreateParticle(
            "O2-",
            "O",
            -2.0,
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80");
        var vacancy = CreateParticle(
            "VO",
            "O",
            chargedVacancy ? -2.0 : 0.0,
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e81",
            isVacancy: true);
        var cerium = CreateParticle(
            "Ce4+",
            "Ce",
            4.0,
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82");
        var oxygenSet = defaultUncharged
            ? CreateSet("Oxygen site", "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83", vacancy.Id, oxygen.Id)
            : CreateSet("Oxygen site", "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83", oxygen.Id, vacancy.Id);
        var ceriumSet = CreateSet("Cerium site", "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e84", cerium.Id);
        var length = invalidCell ? 5.0 : 5.41;
        var structure = StructureModel.Default with
        {
            Name = "Ceria",
            SpaceGroup = new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None),
            Cell = new CellParameters(
                new LengthAngstrom(length),
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
            ],
        };

        return new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e90"),
                new string('a', 64),
                "Validation fixture",
                null,
                createdUtc,
                createdUtc),
            new ModelDefinition
            {
                Particles = [oxygen, vacancy, cerium],
                ParticleSets = [oxygenSet, ceriumSet],
                Structure = structure,
            },
            [],
            [],
            [],
            ViewSettings.Default,
            symmetry);
    }

    private static Particle CreateParticle(
        string name,
        string symbol,
        double charge,
        string id,
        bool isVacancy = false) =>
        new(
            ParticleId.Create(Guid.Parse(id)),
            name,
            symbol,
            new ChargeElementary(charge),
            isVacancy);

    private static ParticleSet CreateSet(string name, string id, params ParticleId[] members) =>
        new(ParticleSetId.Create(Guid.Parse(id)), name, [.. members]);

    private sealed class TempDirectory : IDisposable
    {
        private readonly string path = Directory.CreateTempSubdirectory("mocassin-validation-").FullName;

        public string File(string name) => Path.Combine(path, name);

        public void Dispose()
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (IOException)
            {
                // Test cleanup is best-effort.
            }
            catch (UnauthorizedAccessException)
            {
                // Test cleanup is best-effort.
            }
        }
    }
}
