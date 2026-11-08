// SPDX-License-Identifier: MIT

using System.Globalization;
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
public sealed class CeriaAuthoringTests
{
    private static readonly DateTimeOffset createdUtc = DateTimeOffset.Parse(
        "2026-11-08T12:00:00Z",
        CultureInfo.InvariantCulture);

    [Fact]
    public void CeriaCanBeAuthoredThroughThePublicEditingApi()
    {
        var editor = new ProjectEditor(ProjectValidationService.Validate);
        var project = CreateBlankProject();

        var cerium = CreateParticle("Ce4+", "Ce", 4.0);
        var oxygen = CreateParticle("O2-", "O", -2.0);
        var vacancy = CreateParticle("Vacancy", "Vc", 0.0, isVacancy: true);
        var ceriumSet = CreateSet("Cerium site", cerium.Id);
        var oxygenSet = CreateSet("Oxygen site", oxygen.Id, vacancy.Id);

        project = Apply(editor.AddParticle(project, cerium));
        project = Apply(editor.AddParticle(project, oxygen));
        project = Apply(editor.AddParticle(project, vacancy));
        project = Apply(editor.AddParticleSet(project, ceriumSet));
        project = Apply(editor.AddParticleSet(project, oxygenSet));
        project = Apply(editor.SetStructureName(project, "Ceria"));
        project = Apply(editor.SetSpaceGroup(
            project,
            new SpaceGroupRef(225, AxisSetting.None, OriginChoice.None)));
        project = Apply(editor.SetCellParameters(project, CubicCell(5.41)));
        project = Apply(editor.AddCellSite(
            project,
            CreateSite("Ce", Fractional3.Zero, ceriumSet.Id, SiteStability.Stable)));
        project = Apply(editor.AddCellSite(
            project,
            CreateSite("O", new Fractional3(0.25, 0.25, 0.25), oxygenSet.Id, SiteStability.Stable)));
        project = Apply(editor.AddCellSite(
            project,
            CreateSite(
                "O transition",
                new Fractional3(0.5, 0.25, 0.25),
                oxygenSet.Id,
                SiteStability.Unstable)));

        Assert.Empty(ProjectValidationService.Validate(project));
        Assert.Equal("Ceria", project.Structure.Name);
        Assert.Equal(225, project.Structure.SpaceGroup.Number);
        Assert.Equal(5.41, project.Structure.Cell.A.Value);
        Assert.Equal(3, project.Structure.Sites.Length);
        Assert.Contains(
            project.Structure.Sites,
            static site => site.Name == "O transition" && site.Stability == SiteStability.Unstable);
    }

    private static ProjectModel Apply(ProjectEditResult result)
    {
        Assert.True(
            result.IsSuccess,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        return result.Project;
    }

    private static ProjectModel CreateBlankProject() =>
        new(
            ProjectSchema.SchemaUri,
            ProjectSchema.CurrentVersion,
            new ProjectMetadata(
                Guid.Parse("018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e90"),
                new string('a', 64),
                "Ceria authoring test",
                null,
                createdUtc,
                createdUtc),
            ModelDefinition.Empty,
            [],
            [],
            [],
            ViewSettings.Default);

    private static Particle CreateParticle(
        string name,
        string symbol,
        double charge,
        bool isVacancy = false) =>
        new(ParticleId.New(), name, symbol, new ChargeElementary(charge), isVacancy);

    private static ParticleSet CreateSet(string name, params ParticleId[] members) =>
        new(ParticleSetId.New(), name, [.. members]);

    private static CellSite CreateSite(
        string name,
        Fractional3 position,
        ParticleSetId particleSetId,
        SiteStability stability) =>
        new(CellSiteId.New(), name, position, particleSetId, stability);

    private static CellParameters CubicCell(double length) =>
        new(
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new LengthAngstrom(length),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0),
            new AngleDegrees(90.0));
}
