// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Structure;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Symmetry.Tests.Structure;

[Trait("Tier", "PR-fast")]
public sealed class SampleStructureTests
{
    private static readonly int[] ceriaOrbit0 = [0, 3, 18, 21];
    private static readonly int[] ceriaOrbit1 = [9, 11, 15, 17, 27, 29, 33, 35];
    private static readonly int[] ceriaOrbit2 =
    [
        1, 2, 4, 5, 6, 7, 8, 10, 12, 13, 14, 16,
        19, 20, 22, 23, 24, 25, 26, 28, 30, 31, 32, 34,
    ];
    private static readonly int[] ceriaOrbitSizes = [4, 8, 24];
    private static readonly int[] bariumZirconateOrbitSizes = [1, 1, 3, 12];
    private static readonly int[] bariumZirconateOrbit0 = [0];
    private static readonly int[] bariumZirconateOrbit1 = [13];
    private static readonly int[] bariumZirconateOrbit2 = [3, 9, 12];
    private static readonly int[] bariumZirconateOrbit3 = [1, 2, 4, 5, 6, 7, 8, 10, 11, 14, 15, 16];

    [Fact]
    public void CeriaSamplePreviewMatchesGoldenPOrder()
    {
        var project = ReadSample("samples/ceria/ceria.mocprj");
        var preview = CreatePreview(project.Structure);

        Assert.Empty(new StructureValidator().Validate(project.Structure));
        Assert.Equal(36, preview.PositionList.Length);
        Assert.Equal(ceriaOrbitSizes, preview.Orbits.Select(static orbit => orbit.Multiplicity).ToArray());
        Assert.Equal(ceriaOrbit0, preview.Orbits[0].PositionIndices);
        Assert.Equal(ceriaOrbit1, preview.Orbits[1].PositionIndices);
        Assert.Equal(ceriaOrbit2, preview.Orbits[2].PositionIndices);
        Assert.Equal(Enumerable.Range(0, 36), preview.PositionList.Select(static site => site.P));
        Assert.Equal(Fractional3.Zero, preview.PositionList[0].Fractional);
        Assert.Equal(new Fractional3(0.75, 0.75, 0.75), preview.PositionList[^1].Fractional);
    }

    [Fact]
    public void BariumZirconateSamplePreviewMatchesGoldenPOrder()
    {
        var project = ReadSample("samples/barium-zirconate/barium-zirconate.mocprj");
        var preview = CreatePreview(project.Structure);

        Assert.Empty(new StructureValidator().Validate(project.Structure));
        Assert.Contains(
            project.Model.Structure.Sites,
            static site => site.Name == "H interstitial" && site.Stability == SiteStability.Stable);
        Assert.Equal(17, preview.PositionList.Length);
        Assert.Equal(
            bariumZirconateOrbitSizes,
            preview.Orbits.Select(static orbit => orbit.Multiplicity).ToArray());
        Assert.Equal(bariumZirconateOrbit0, preview.Orbits[0].PositionIndices);
        Assert.Equal(bariumZirconateOrbit1, preview.Orbits[1].PositionIndices);
        Assert.Equal(bariumZirconateOrbit2, preview.Orbits[2].PositionIndices);
        Assert.Equal(bariumZirconateOrbit3, preview.Orbits[3].PositionIndices);
        Assert.Equal(Enumerable.Range(0, 17), preview.PositionList.Select(static site => site.P));
        Assert.Equal(Fractional3.Zero, preview.PositionList[0].Fractional);
        Assert.Equal(new Fractional3(0.734, 0.5, 0.0), preview.PositionList[^1].Fractional);
    }

    [Fact]
    public void BariumZirconateSharesOneUnchargedVacancyParticle()
    {
        var project = ReadSample("samples/barium-zirconate/barium-zirconate.mocprj");

        var vacancy = Assert.Single(project.Model.Particles.Where(static particle => particle.IsVacancy));

        Assert.Equal("V", vacancy.Symbol);
        Assert.Equal(0.0, vacancy.Charge.Value);
        Assert.Contains(
            project.Model.ParticleSets,
            particleSet => particleSet.Name == "Oxygen site" && particleSet.ParticleIds.Contains(vacancy.Id));
        Assert.Contains(
            project.Model.ParticleSets,
            particleSet => particleSet.Name == "Proton site" && particleSet.ParticleIds.Contains(vacancy.Id));
    }

    [Fact]
    public void CeriaUsesADistinctUnchargedVacancySymbol()
    {
        var project = ReadSample("samples/ceria/ceria.mocprj");

        var vacancy = Assert.Single(project.Model.Particles.Where(static particle => particle.IsVacancy));

        Assert.Equal("Vc", vacancy.Symbol);
        Assert.Equal(0.0, vacancy.Charge.Value);
        Assert.DoesNotContain(
            project.Model.Particles,
            particle => !particle.IsVacancy && particle.Symbol == vacancy.Symbol);
    }

    private static ProjectModel ReadSample(string relativePath)
    {
        var bytes = File.ReadAllBytes(RepositoryPaths.Combine(relativePath.Split('/')));
        return ProjectJson.Deserialize(bytes);
    }

    private static StructurePreview CreatePreview(StructureModel structure)
    {
        var preview = new StructurePreviewService().TryCreatePreview(structure, 1, 1, 1, out var diagnostics);
        Assert.NotNull(preview);
        Assert.Empty(diagnostics);
        return preview;
    }
}
