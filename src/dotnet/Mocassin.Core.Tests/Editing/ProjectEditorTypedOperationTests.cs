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
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Tests.Editing;

[Trait("Tier", "PR-fast")]
public sealed class ProjectEditorTypedOperationTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        CultureInfo.InvariantCulture);

    private readonly ProjectEditor editor = new();

    [Fact]
    public void AddUpdateAndRemoveParticle()
    {
        var project = CreateProject();
        var particle = CreateParticle("Na+", "Na", 1.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e89");

        var added = editor.AddParticle(project, particle);
        Assert.True(added.IsSuccess, DiagnosticsText(added));
        Assert.NotNull(added.Change);
        Assert.Equal(4, added.Project.Model.Particles.Length);
        Assert.Same(added.Project, added.Change.After);
        Assert.Contains(added.Project.Particles, candidate => candidate.Id == particle.Id);

        var updatedParticle = particle with { Name = "Na+" };
        var updated = editor.UpdateParticle(added.Project, updatedParticle);
        Assert.True(updated.IsSuccess, DiagnosticsText(updated));
        Assert.Equal("Na+", updated.Project.Find(particle.Id)?.Name);

        var removed = editor.RemoveParticle(updated.Project, particle.Id);
        Assert.True(removed.IsSuccess, DiagnosticsText(removed));
        Assert.Null(removed.Project.Find(particle.Id));
    }

    [Fact]
    public void AddParticleWithDuplicateIdFailsAndLeavesProjectUnchanged()
    {
        var project = CreateProject();
        var duplicate = project.Model.Particles[0] with { Name = "Duplicate" };

        var result = editor.AddParticle(project, duplicate);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.DuplicateId);
    }

    [Fact]
    public void AddParticleWithEmptyNameFails()
    {
        var project = CreateProject();
        var particle = CreateParticle(" ", "Na", 1.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e89");

        var result = editor.AddParticle(project, particle);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.InvalidName);
    }

    [Fact]
    public void UpdateOrRemoveUnknownParticleReturnsUnknownReference()
    {
        var project = CreateProject();
        var unknown = CreateParticle("Unknown", "X", 0.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e89");

        var updated = editor.UpdateParticle(project, unknown);
        var removed = editor.RemoveParticle(project, unknown.Id);

        AssertUnknown(updated, project);
        AssertUnknown(removed, project);
    }

    [Fact]
    public void RemoveReferencedParticleFails()
    {
        var project = CreateProject();

        var result = editor.RemoveParticle(project, project.Model.Particles[0].Id);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.UnknownParticleReference);
    }

    [Fact]
    public void AddUpdateAndRemoveParticleSet()
    {
        var project = CreateProject();
        var particleSet = CreateSet(
            "Sodium site",
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e8a",
            project.Model.Particles[0].Id);

        var added = editor.AddParticleSet(project, particleSet);
        Assert.True(added.IsSuccess, DiagnosticsText(added));
        Assert.NotNull(added.Project.Find(particleSet.Id));

        var updated = editor.UpdateParticleSet(added.Project, particleSet with { Name = "Na site" });
        Assert.True(updated.IsSuccess, DiagnosticsText(updated));
        Assert.Equal("Na site", updated.Project.Find(particleSet.Id)?.Name);

        var removed = editor.RemoveParticleSet(updated.Project, particleSet.Id);
        Assert.True(removed.IsSuccess, DiagnosticsText(removed));
        Assert.Null(removed.Project.Find(particleSet.Id));
    }

    [Fact]
    public void AddParticleSetWithUnknownMemberFails()
    {
        var project = CreateProject();
        var particleSet = CreateSet(
            "Broken site",
            "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e8a",
            ParticleId.New());

        var result = editor.AddParticleSet(project, particleSet);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ParticleDiagnosticCodes.UnknownParticleReference);
    }

    [Fact]
    public void RemoveReferencedParticleSetFails()
    {
        var project = CreateProject();
        var referencedSet = project.Model.ParticleSets[0];

        var result = editor.RemoveParticleSet(project, referencedSet.Id);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    [Fact]
    public void SetStructureNameValidatesName()
    {
        var project = CreateProject();

        var valid = editor.SetStructureName(project, "Ceria copy");
        Assert.True(valid.IsSuccess, DiagnosticsText(valid));
        Assert.Equal("Ceria copy", valid.Project.Structure.Name);

        var invalid = editor.SetStructureName(project, " ");
        Assert.False(invalid.IsSuccess);
        Assert.Same(project, invalid.Project);
        Assert.Contains(
            invalid.Diagnostics,
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidStructureName);
    }

    [Fact]
    public void SetSpaceGroupAndCellParameters()
    {
        var project = CreateProject();

        var spaceGroup = editor.SetSpaceGroup(
            project,
            new SpaceGroupRef(221, AxisSetting.None, OriginChoice.None));
        Assert.True(spaceGroup.IsSuccess, DiagnosticsText(spaceGroup));
        Assert.Equal(221, spaceGroup.Project.Structure.SpaceGroup.Number);

        var cell = new CellParameters(
            new LengthAngstrom(4.19),
            new LengthAngstrom(4.19),
            new LengthAngstrom(4.19),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0));
        var parameters = editor.SetCellParameters(spaceGroup.Project, cell);
        Assert.True(parameters.IsSuccess, DiagnosticsText(parameters));
        Assert.Equal(4.19, parameters.Project.Structure.Cell.A.Value);
    }

    [Fact]
    public void AddUpdateAndRemoveCellSite()
    {
        var project = CreateProject();
        var site = new CellSite(
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e8b")),
            "New site",
            new Fractional3(0.5, 0.5, 0.5),
            project.Model.ParticleSets[0].Id,
            SiteStability.Stable);

        var added = editor.AddCellSite(project, site);
        Assert.True(added.IsSuccess, DiagnosticsText(added));
        Assert.NotNull(added.Project.Find(site.Id));

        var updated = editor.UpdateCellSite(added.Project, site with { Name = "Updated site" });
        Assert.True(updated.IsSuccess, DiagnosticsText(updated));
        Assert.Equal("Updated site", updated.Project.Find(site.Id)?.Name);

        var removed = editor.RemoveCellSite(updated.Project, site.Id);
        Assert.True(removed.IsSuccess, DiagnosticsText(removed));
        Assert.Null(removed.Project.Find(site.Id));
    }

    [Fact]
    public void AddCellSiteWithInvalidReferencesFails()
    {
        var project = CreateProject();
        var unknownSetSite = new CellSite(
            CellSiteId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e8b")),
            "Unknown set",
            new Fractional3(0.5, 0.5, 0.5),
            ParticleSetId.New(),
            SiteStability.Stable);
        var missingSetSite = unknownSetSite with { ParticleSetId = ParticleSetId.Empty };

        var unknown = editor.AddCellSite(project, unknownSetSite);
        var missing = editor.AddCellSite(project, missingSetSite);

        Assert.False(unknown.IsSuccess);
        Assert.Contains(
            unknown.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
        Assert.False(missing.IsSuccess);
        Assert.Contains(
            missing.Diagnostics,
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.MissingParticleSet);
    }

    [Fact]
    public void UpdateOrRemoveUnknownCellSiteReturnsUnknownReference()
    {
        var project = CreateProject();
        var site = new CellSite(
            CellSiteId.New(),
            "Unknown",
            Fractional3.Zero,
            project.Model.ParticleSets[0].Id,
            SiteStability.Stable);

        AssertUnknown(editor.UpdateCellSite(project, site), project);
        AssertUnknown(editor.RemoveCellSite(project, site.Id), project);
    }

    [Fact]
    public void AddUpdateAndRemoveDummyPosition()
    {
        var project = CreateProject();
        var dummy = new CellDummyPosition(
            CellDummyPositionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e8c")),
            "New center",
            new Fractional3(0.25, 0.25, 0.25));

        var added = editor.AddDummyPosition(project, dummy);
        Assert.True(added.IsSuccess, DiagnosticsText(added));
        Assert.Contains(added.Project.Structure.DummyPositions, candidate => candidate.Id == dummy.Id);

        var updated = editor.UpdateDummyPosition(
            added.Project,
            dummy with { Fractional = new Fractional3(0.5, 0.5, 0.5) });
        Assert.True(updated.IsSuccess, DiagnosticsText(updated));
        Assert.Equal(
            new Fractional3(0.5, 0.5, 0.5),
            updated.Project.Structure.DummyPositions.Single(candidate => candidate.Id == dummy.Id).Fractional);

        var removed = editor.RemoveDummyPosition(updated.Project, dummy.Id);
        Assert.True(removed.IsSuccess, DiagnosticsText(removed));
        Assert.DoesNotContain(removed.Project.Structure.DummyPositions, candidate => candidate.Id == dummy.Id);
    }

    [Fact]
    public void AddDummyWithInvalidCoordinateFails()
    {
        var project = CreateProject();
        var dummy = new CellDummyPosition(
            CellDummyPositionId.New(),
            "Invalid",
            new Fractional3(1.1, 0.5, 0.5));

        var result = editor.AddDummyPosition(project, dummy);

        Assert.False(result.IsSuccess);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == CoreStructureDiagnosticCodes.InvalidDummyPosition);
    }

    [Fact]
    public void UpdateOrRemoveUnknownDummyReturnsUnknownReference()
    {
        var project = CreateProject();
        var dummy = new CellDummyPosition(
            CellDummyPositionId.New(),
            "Unknown",
            new Fractional3(0.5, 0.5, 0.5));

        AssertUnknown(editor.UpdateDummyPosition(project, dummy), project);
        AssertUnknown(editor.RemoveDummyPosition(project, dummy.Id), project);
    }

    [Fact]
    public void NoOpUpdateDoesNotCreateChange()
    {
        var project = CreateProject();

        var result = editor.UpdateParticle(project, project.Model.Particles[0]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Same(project, result.Project);
    }

    private static string DiagnosticsText(ProjectEditResult result) =>
        string.Join(" | ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString()));

    private static void AssertUnknown(ProjectEditResult result, ProjectModel project)
    {
        Assert.False(result.IsSuccess);
        Assert.Null(result.Change);
        Assert.Same(project, result.Project);
        Assert.Contains(
            result.Diagnostics,
            static diagnostic => diagnostic.Code == ProjectDiagnosticCodes.UnknownReference);
    }

    private static ProjectModel CreateProject()
    {
        var oxygen = CreateParticle("O2-", "O", -2.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80");
        var vacancy = CreateParticle("Vacancy", "Vc", 0.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e81", isVacancy: true);
        var cerium = CreateParticle("Ce4+", "Ce", 4.0, "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e82");
        var oxygenSet = CreateSet("Oxygen site", "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e83", oxygen.Id, vacancy.Id);
        var ceriumSet = CreateSet("Cerium site", "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e84", cerium.Id);
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
            ],
            DummyPositions =
            [
                new CellDummyPosition(
                    CellDummyPositionId.Create(Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e88")),
                    "Center",
                    new Fractional3(0.5, 0.5, 0.5)),
            ],
        };

        return new ProjectModel(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e90"),
                new string('a', 64),
                "Editing fixture",
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
            ViewSettings.Default);
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
}
