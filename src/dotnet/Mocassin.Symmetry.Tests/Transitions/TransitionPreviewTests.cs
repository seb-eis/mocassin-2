// SPDX-License-Identifier: MIT

using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Symmetry.Tests.TestSupport;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Symmetry.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionPreviewTests
{
    private static readonly Fractional3 stableA = new(0.25, 0.25, 0.25);
    private static readonly Fractional3 unstablePoint = new(0.5, 0.25, 0.25);
    private static readonly Fractional3 stableB = new(0.75, 0.25, 0.25);

    [Fact]
    public void ValidKineticPreviewDescribesPathMatricesAndCounts()
    {
        var model = TransitionTestModels.CreateModel();
        var project = TransitionTestModels.CreateProject(
            TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB));
        var transition = project.Model.KineticTransitions[0];
        var service = new TransitionPreviewService();

        var preview = service.TryCreatePreview(transition, project, 2, 3, 4, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(preview);
        Assert.Equal(TransitionRef.From(transition.Id), preview.Transition);
        Assert.Equal(2, preview.CellCountA);
        Assert.Equal(3, preview.CellCountB);
        Assert.Equal(4, preview.CellCountC);
        Assert.Equal(3, preview.ResolvedPath.Length);
        Assert.Single(preview.ExpandedPaths);
        Assert.Equal(3, preview.StateChangeMatrices.Length);
        Assert.Equal(16, preview.StateChangeMatrices[0].Length);
        Assert.Equal(4, preview.ExpectedRuleCount);
        Assert.Equal(
            [
                TransitionTestModels.Oxygen.Id,
                TransitionTestModels.Vacancy.Id,
                TransitionTestModels.Cerium.Id,
                ParticleId.Void,
            ],
            preview.ParticleOrder.ToArray());
        Assert.Equal(SiteStability.Unstable, preview.ResolvedPath[1].Stability);
        Assert.Equal(1, preview.StateChangeMatrices[1].Count(static value => value != 0));
        Assert.Equal(1, preview.StateChangeMatrices[1][(0 * preview.ParticleOrder.Length) + 3]);
    }

    [Fact]
    public void ValidMetropolisPreviewSupportsSameSiteExchange()
    {
        var model = TransitionTestModels.CreateModel();
        var site = TransitionTestModels.SiteId(TransitionTestModels.Site2);
        var project = TransitionTestModels.CreateProject(
            TransitionTestModels.WithMetropolisTransition(model, site, site));
        var transition = project.Model.MetropolisTransitions[0];
        var service = new TransitionPreviewService();

        var preview = service.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(preview);
        Assert.Equal(2, preview.ReferencePath.Length);
        Assert.Equal(preview.ReferencePath[0], preview.ReferencePath[1]);
        Assert.Single(preview.ExpandedPaths);
        Assert.Equal(2, preview.StateChangeMatrices.Length);
        Assert.Equal(4, preview.ExpectedRuleCount);
        Assert.All(preview.StateChangeMatrices, static matrix => Assert.Equal(16, matrix.Length));
    }

    [Fact]
    public void InvalidPathReturnsNullWithDiagnostics()
    {
        var model = TransitionTestModels.CreateModel();
        var project = TransitionTestModels.CreateProject(
            TransitionTestModels.WithKineticPath(model, stableA, new Fractional3(0.1, 0.1, 0.1), stableB));
        var transition = project.Model.KineticTransitions[0];
        var service = new TransitionPreviewService();

        var preview = service.TryCreatePreview(transition, project, 1, 1, 1, out var diagnostics);

        Assert.Null(preview);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == "TRN020");
    }

    [Fact]
    public void NonPositiveCellCountsAreProgrammerErrors()
    {
        var model = TransitionTestModels.CreateModel();
        var project = TransitionTestModels.CreateProject(
            TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB));
        var transition = project.Model.KineticTransitions[0];
        var service = new TransitionPreviewService();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.TryCreatePreview(transition, project, 0, 1, 1, out _));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.TryCreatePreview(transition, project, 1, -1, 1, out _));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.TryCreatePreview(transition, project, 1, 1, 0, out _));
    }

    [Fact]
    public void CacheSeparatesCellCountsAndReturnsSamePreviewOnHit()
    {
        var model = TransitionTestModels.CreateModel();
        var project = TransitionTestModels.CreateProject(
            TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB));
        var transition = project.Model.KineticTransitions[0];
        var service = new TransitionPreviewService(cacheCapacity: 8);

        var first = service.TryCreatePreview(transition, project, 1, 1, 1, out _);
        var second = service.TryCreatePreview(transition, project, 1, 1, 1, out _);
        var differentCounts = service.TryCreatePreview(transition, project, 2, 1, 1, out _);

        Assert.Same(first, second);
        Assert.Equal(1, first!.CellCountA);
        Assert.Equal(2, differentCounts!.CellCountA);
        Assert.NotSame(first, differentCounts);
    }

    [Fact]
    public void PreviewServiceImplementsTheContract()
    {
        Assert.IsAssignableFrom<ITransitionPreviewService>(new TransitionPreviewService());
    }
}
