// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Symmetry.Tests.TestSupport;
using Mocassin.Symmetry.Transitions;

namespace Mocassin.Symmetry.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionPathTests
{
    private static readonly Fractional3 stableA = new(0.25, 0.25, 0.25);
    private static readonly Fractional3 unstablePoint = new(0.5, 0.25, 0.25);
    private static readonly Fractional3 stableB = new(0.75, 0.25, 0.25);
    private static readonly Fractional3 unstablePointB = new(0.5, 0.5, 0.25);

    [Fact]
    public void ValidMigrationPathResolvesToOwningSites()
    {
        var model = TransitionTestModels.CreateModel();
        var transition = TransitionTestModels.KineticTransitionWith(model, stableA, unstablePoint, stableB);

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out var resolverDiagnostics));
        Assert.Empty(resolverDiagnostics);
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(path);
        Assert.Equal(3, path.Positions.Length);
        Assert.Equal(
            [
                TransitionTestModels.SiteId(TransitionTestModels.Site2),
                TransitionTestModels.SiteId(TransitionTestModels.Site3),
                TransitionTestModels.SiteId(TransitionTestModels.Site4),
            ],
            path.Positions.Select(static position => position.SiteId));
        Assert.Equal(
            [SiteStability.Stable, SiteStability.Unstable, SiteStability.Stable],
            path.Positions.Select(static position => position.Stability));
    }

    [Fact]
    public void NearBoundaryPointUsesNearestPeriodicImage()
    {
        var model = TransitionTestModels.CreateModel();
        var transition = TransitionTestModels.KineticTransitionWith(model, new Fractional3(0.9999995, 0.0, 0.0));

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Empty(diagnostics);
        var position = Assert.Single(path!.Positions);
        Assert.Equal(0, position.P);
        Assert.Equal(new Vector4I(1, 0, 0, 0), position.Encoding);
        Assert.Equal(Fractional3.Zero, position.CanonicalPosition);
        Assert.Equal(TransitionTestModels.SiteId(TransitionTestModels.Site1), position.SiteId);
    }

    [Fact]
    public void SamePositionInDifferentPeriodicCellsIsNotADuplicate()
    {
        var model = TransitionTestModels.CreateModel();
        var transition = TransitionTestModels.KineticTransitionWith(
            model,
            stableA,
            stableA + new Fractional3(1.0, 0.0, 0.0));

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(2, path!.Positions.Length);
        Assert.NotEqual(path.Positions[0].Encoding, path.Positions[1].Encoding);
    }

    [Fact]
    public void RepeatedEncodedPositionIsReported()
    {
        var model = TransitionTestModels.CreateModel();
        var transition = TransitionTestModels.KineticTransitionWith(model, stableA, stableA);

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Null(path);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.RepeatedPathPosition);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(0.1)]
    public void NonFiniteOrUnmatchedPointsAreReported(double value)
    {
        var model = TransitionTestModels.CreateModel();
        var transition = TransitionTestModels.KineticTransitionWith(model, new Fractional3(value, 0.1, 0.1));

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Null(path);
        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnmatchedPathPosition);
    }

    [Fact]
    public void AmbiguousPathMatchListsCandidatePIndices()
    {
        var model = TransitionTestModels.CreateModel();
        var sites = new[]
        {
            new CellSite(
                Mocassin.Core.Ids.CellSiteId.New(),
                "Close 1",
                new Fractional3(0.25, 0.25, 0.25),
                TransitionTestModels.OxygenSet.Id,
                SiteStability.Stable),
            new CellSite(
                Mocassin.Core.Ids.CellSiteId.New(),
                "Close 2",
                new Fractional3(0.250001, 0.25, 0.25),
                TransitionTestModels.OxygenSet.Id,
                SiteStability.Stable),
        };
        model = model with { Structure = model.Structure with { Sites = [.. sites] } };
        var transition = TransitionTestModels.KineticTransitionWith(
            model,
            new Fractional3(0.2500005, 0.25, 0.25));

        Assert.True(TransitionPathResolver.TryCreate(model.Structure, catalog: null, out var resolver, out _));
        var path = resolver!.TryResolve(transition, out var diagnostics);

        Assert.Null(path);
        var diagnostic = Assert.Single(diagnostics, static item => item.Code == TransitionDiagnosticCodes.UnmatchedPathPosition);
        Assert.Contains(
            diagnostic.Related,
            static item => item.Domain == "candidatePosition");
    }

    [Fact]
    public void ValidKineticAndMetropolisModelsHaveNoSymmetryTransitionDiagnostics()
    {
        var model = TransitionTestModels.CreateModel();
        var kinetic = TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB);
        var metropolis = TransitionTestModels.WithMetropolisTransition(
            model,
            TransitionTestModels.SiteId(TransitionTestModels.Site2),
            TransitionTestModels.SiteId(TransitionTestModels.Site4));
        var validator = new TransitionPathValidator();

        Assert.Empty(validator.Validate(kinetic));
        Assert.Empty(validator.Validate(metropolis));
    }

    [Fact]
    public void PathLengthMismatchIsReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            stableA,
            unstablePoint);

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.PathLengthMismatch);
    }

    [Fact]
    public void UnstableEndpointIsReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            unstablePoint,
            stableA,
            stableB);

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnstablePathEndpoint);
    }

    [Fact]
    public void ConsecutiveUnstablePositionsAreReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            stableA,
            unstablePoint,
            unstablePointB);

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.ConsecutiveUnstablePositions);
    }

    [Fact]
    public void RepeatedPathPositionIsReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            stableA,
            unstablePoint,
            stableA);

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.RepeatedPathPosition);
    }

    [Fact]
    public void GroupStabilityMismatchIsReported()
    {
        var model = TransitionTestModels.CreateModel();
        var chain = TransitionTestModels.Migration3 with
        {
            GroupIds =
            [
                TransitionTestModels.StableGroup.Id,
                TransitionTestModels.StableGroup.Id,
                TransitionTestModels.StableGroup.Id,
            ],
        };
        model = TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB) with
        {
            Chains = [chain],
        };

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.GroupStabilityMismatch);
    }

    [Fact]
    public void BuiltInStabilitySequenceMismatchIsReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            stableA,
            stableB,
            new Fractional3(0.0, 0.0, 0.0));

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.PathStabilityMismatch);
    }

    [Fact]
    public void UnmatchedPathPointIsReported()
    {
        var model = TransitionTestModels.WithKineticPath(
            TransitionTestModels.CreateModel(),
            stableA,
            new Fractional3(0.1, 0.1, 0.1),
            stableB);

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnmatchedPathPosition);
    }

    [Fact]
    public void InvalidMetropolisChainAndSiteAreReported()
    {
        var model = TransitionTestModels.CreateModel();
        var wrongChain = TransitionTestModels.WithMetropolisTransition(
            model,
            TransitionTestModels.SiteId(TransitionTestModels.Site2),
            TransitionTestModels.SiteId(TransitionTestModels.Site4),
            TransitionTestModels.Migration3.Id);
        var unknownSite = TransitionTestModels.WithMetropolisTransition(
            model,
            new Mocassin.Core.Ids.CellSiteId(Guid.NewGuid()),
            TransitionTestModels.SiteId(TransitionTestModels.Site4));
        var unstableSite = TransitionTestModels.WithMetropolisTransition(
            model,
            TransitionTestModels.SiteId(TransitionTestModels.Site3),
            TransitionTestModels.SiteId(TransitionTestModels.Site4));
        var validator = new TransitionPathValidator();

        Assert.Contains(
            validator.Validate(wrongChain),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidMetropolisChain);
        Assert.Contains(
            validator.Validate(unknownSite),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidMetropolisSite);
        Assert.Contains(
            validator.Validate(unstableSite),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidMetropolisSite);
    }

    [Fact]
    public void CustomPathWithoutUnstablePositionIsReported()
    {
        var model = TransitionTestModels.CreateModel();
        var custom = new TransitionChain(
            Mocassin.Core.Ids.TransitionChainId.New(),
            "Custom",
            TransitionPattern.Custom,
            [
                TransitionTestModels.StableGroup.Id,
                TransitionTestModels.StableGroup.Id,
                TransitionTestModels.StableGroup.Id,
            ],
            [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
            GeneratesReverse: false);
        model = TransitionTestModels.WithKineticPath(model, stableA, stableB, new Fractional3(0.0, 0.0, 0.0));
        model = model with
        {
            Chains = [custom],
            KineticTransitions = [model.KineticTransitions[0] with { ChainId = custom.Id }],
        };

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition);
    }

    [Fact]
    public void CustomUnstablePositionWithTwoVoidPairsIsReported()
    {
        var model = TransitionTestModels.CreateModel();
        var extraUnstable = TransitionTestModels.Unstable with
        {
            Id = Mocassin.Core.Ids.StateChangeId.New(),
            Name = "Second void exchange",
        };
        var unstableGroup = TransitionTestModels.UnstableGroup with
        {
            StateChangeIds = [TransitionTestModels.Unstable.Id, extraUnstable.Id],
        };
        var custom = new TransitionChain(
            Mocassin.Core.Ids.TransitionChainId.New(),
            "Custom",
            TransitionPattern.Custom,
            [TransitionTestModels.StableGroup.Id, unstableGroup.Id, TransitionTestModels.StableGroup.Id],
            [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
            GeneratesReverse: false);
        model = TransitionTestModels.WithKineticPath(model, stableA, unstablePoint, stableB);
        model = model with
        {
            StateChanges = [.. model.StateChanges, extraUnstable],
            StateChangeGroups = [TransitionTestModels.StableGroup, unstableGroup],
            Chains = [custom],
            KineticTransitions = [model.KineticTransitions[0] with { ChainId = custom.Id }],
        };

        var diagnostics = new TransitionPathValidator().Validate(model);

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidUnstablePositionGroup);
    }
}
