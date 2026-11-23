// SPDX-License-Identifier: MIT

using Mocassin.App.Tests.TestSupport;
using Mocassin.App.ViewModels.Editors.Structure;
using Mocassin.Core.Model.Structure;
using Mocassin.Symmetry.Structure;

namespace Mocassin.App.Tests.Editors.StructureEditors;

/// <summary>
/// Recomputes every structure-summary value from <see cref="StructureModel"/> and <see cref="StructurePreview"/>
/// and proves the summary does not depend on the statistics service.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class StructureSummaryTests
{
    [Fact]
    public async Task SummaryValuesEqualTheRecomputedStructureAndPreviewValues()
    {
        var entry = await ProjectFixtures.LoadCeriaAsync();
        var previewService = new StructurePreviewService();
        var validator = new StructureValidator();
        var preview = previewService.TryCreatePreview(entry.Project.Structure, 1, 1, 1, out _)!;
        var sites = entry.Project.Structure.Sites;
        using var summary = new StructureSummaryViewModel(entry.Session, previewService, validator);

        Assert.True(summary.HasPreview);
        Assert.Equal(sites.Length, summary.ReferenceSiteCount);
        Assert.Equal(preview.PositionList.Length, summary.P1PositionCount);
        var stable = preview.PositionList.Count(position =>
            position.ReferenceSiteIndex >= 0
            && position.ReferenceSiteIndex < sites.Length
            && sites[position.ReferenceSiteIndex].Stability == SiteStability.Stable);
        var unstable = preview.PositionList.Length - stable;
        Assert.Equal(stable, summary.StablePositionCount);
        Assert.Equal(unstable, summary.UnstablePositionCount);
        Assert.Equal(preview.CellVolumeAngstrom3, summary.CellVolumeAngstrom3, 10);
        Assert.Equal(
            summary.ReferenceSiteCount / preview.CellVolumeAngstrom3,
            summary.ReferenceSiteDensity,
            10);
        Assert.Contains("Å³", summary.CellVolumeText, StringComparison.Ordinal);
        Assert.Contains("Å⁻³", summary.ReferenceSiteDensityText, StringComparison.Ordinal);
        Assert.Contains("reference sites", summary.DensityLabel, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(preview.Orbits.Length, summary.Orbits.Count);
        for (var index = 0; index < preview.Orbits.Length; index++)
        {
            var orbit = preview.Orbits[index];
            var row = summary.Orbits[index];
            Assert.Equal(sites[orbit.ReferenceSiteIndex].Name, row.SiteName);
            Assert.Equal(orbit.Multiplicity, row.Multiplicity);
            Assert.Equal(orbit.Positions.Length, row.PositionCount);
        }

        Assert.False(summary.HasLargeP1Warning);
    }

    [Fact]
    public void TheSummaryDoesNotDependOnTheStatisticsService()
    {
        var constructors = typeof(StructureSummaryViewModel).GetConstructors();
        Assert.All(
            constructors,
            constructor => Assert.DoesNotContain(
                constructor.GetParameters(),
                parameter => parameter.ParameterType == typeof(Mocassin.Packages.Projects.Validation.ModelStatistics)));
        Assert.DoesNotContain(
            typeof(StructureSummaryViewModel).GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public),
            field => field.FieldType == typeof(Mocassin.Packages.Projects.Validation.ModelStatistics));
    }
}
