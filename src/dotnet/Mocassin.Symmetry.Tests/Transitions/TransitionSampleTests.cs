// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Project;
using Mocassin.Symmetry.Transitions;
using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Symmetry.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionSampleTests
{
    private static readonly string[] sampleFiles =
    [
        "ceria-kmc.mocprj",
        "ceria-mmc.mocprj",
        "interstitialcy5.mocprj",
        "polaron-hop.mocprj",
        "vehicle5.mocprj",
    ];

    [Theory]
    [MemberData(nameof(Samples))]
    public void TransitionSampleValidatesAndPreviews(string fileName)
    {
        var project = ReadSample(fileName);
        var pathValidator = new TransitionPathValidator();
        var previewService = new TransitionPreviewService();

        Assert.Empty(ProjectValidator.Validate(project));
        Assert.Empty(pathValidator.Validate(project.Model));

        foreach (var transition in project.Model.KineticTransitions)
        {
            var preview = previewService.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);
            Assert.Empty(diagnostics);
            Assert.NotNull(preview);
        }

        foreach (var transition in project.Model.MetropolisTransitions)
        {
            var preview = previewService.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);
            Assert.Empty(diagnostics);
            Assert.NotNull(preview);
        }
    }

    [Fact]
    public void CeriaExpandedCountsMatchBruteForce()
    {
        var kineticProject = ReadSample("ceria-kmc.mocprj");
        var kineticPreview = new TransitionPreviewService().TryCreatePreview(
            kineticProject.Model.KineticTransitions[0],
            kineticProject,
            1,
            1,
            1,
            out var kineticDiagnostics);
        Assert.Empty(kineticDiagnostics);
        Assert.NotNull(kineticPreview);
        Assert.True(TransitionPathResolver.TryCreate(kineticProject.Structure, null, out var kineticResolver, out _));
        var expectedKineticCount = CountExpandedPaths(
            kineticResolver!.Encoder.Setting.Operations,
            kineticProject.Model.KineticTransitions[0].ReferencePath);
        Assert.Equal(expectedKineticCount, kineticPreview!.ExpandedPaths.Length);
        Assert.True(expectedKineticCount > 1);

        var metropolisProject = ReadSample("ceria-mmc.mocprj");
        var metropolisPreview = new TransitionPreviewService().TryCreatePreview(
            metropolisProject.Model.MetropolisTransitions[0],
            metropolisProject,
            1,
            1,
            1,
            out var metropolisDiagnostics);
        Assert.Empty(metropolisDiagnostics);
        Assert.NotNull(metropolisPreview);
        Assert.True(TransitionPathResolver.TryCreate(metropolisProject.Structure, null, out var metropolisResolver, out _));
        var siteA = metropolisProject.Model.Structure.Sites.Single(
            site => site.Id == metropolisProject.Model.MetropolisTransitions[0].SiteAId);
        var siteB = metropolisProject.Model.Structure.Sites.Single(
            site => site.Id == metropolisProject.Model.MetropolisTransitions[0].SiteBId);
        var expectedPairCount = CountExpandedSitePairs(
            metropolisResolver!.Encoder.Setting.Operations,
            siteA.Fractional,
            siteB.Fractional);
        Assert.Equal(expectedPairCount, metropolisPreview!.ExpandedPaths.Length);
        Assert.True(expectedPairCount > 1);
    }

    private static int CountExpandedPaths(
        System.Collections.Immutable.ImmutableArray<Mocassin.Symmetry.SpaceGroups.SymmetryOperation> operations,
        System.Collections.Immutable.ImmutableArray<Fractional3> path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            var expanded = path.Select(point => operation.Transform(point).TrimToUnitCell());
            seen.Add(string.Join('|', expanded.Select(static point => point.ToString())));
        }

        return seen.Count;
    }

    private static int CountExpandedSitePairs(
        System.Collections.Immutable.ImmutableArray<Mocassin.Symmetry.SpaceGroups.SymmetryOperation> operations,
        Fractional3 siteA,
        Fractional3 siteB)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in operations)
        {
            var first = operation.Transform(siteA).TrimToUnitCell();
            var second = operation.Transform(siteB).TrimToUnitCell();
            seen.Add(first + "|" + second);
        }

        return seen.Count;
    }

    private static ProjectModel ReadSample(string fileName) =>
        ProjectJson.Deserialize(File.ReadAllBytes(RepositoryPaths.Combine("samples", "transitions", fileName)));

    public static TheoryData<string> Samples => [.. sampleFiles];
}
