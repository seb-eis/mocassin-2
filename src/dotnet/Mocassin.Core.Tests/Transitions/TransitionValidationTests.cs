// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionValidationTests
{
    [Fact]
    public void ValidCombinedModelHasNoCoreTransitionDiagnostics()
    {
        Assert.Empty(TransitionModelValidator.Validate(TransitionTestModels.CreateCombinedModel()));
    }

    [Fact]
    public void KineticTransitionUsingMetropolisChainIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var transition = model.KineticTransitions[0] with { ChainId = model.Chains[0].Id };

        var diagnostics = TransitionModelValidator.Validate(
            model with { KineticTransitions = [transition] });

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain);
    }

    [Fact]
    public void KineticTransitionUsingUnknownChainIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var transition = model.KineticTransitions[0] with { ChainId = TransitionChainId.New() };

        var diagnostics = TransitionModelValidator.Validate(
            model with { KineticTransitions = [transition] });

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain);
    }

    [Fact]
    public void ComplexTransitionWithoutReverseLinkWarns()
    {
        var model = CreateComplexModel(reverse: false);

        var diagnostics = TransitionModelValidator.Validate(model);

        var warning = Assert.Single(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.MissingReverseLink);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void SymmetricComplexReverseLinksClearTheWarning()
    {
        var model = CreateComplexModel(reverse: true);

        Assert.DoesNotContain(
            TransitionModelValidator.Validate(model),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.MissingReverseLink);
    }

    [Fact]
    public void MoreThanOneHundredTransitionsIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var transitions = Enumerable.Range(0, 101)
            .Select(index => new MetropolisTransition(
                MetropolisTransitionId.New(),
                $"Metropolis {index}",
                model.Chains[0].Id,
                model.Structure.Sites[0].Id,
                model.Structure.Sites[1].Id,
                ReverseTransitionId: null))
            .ToImmutableArray();

        var diagnostics = TransitionModelValidator.Validate(
            model with { KineticTransitions = [], MetropolisTransitions = transitions });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.TransitionLimitExceeded);
    }

    [Fact]
    public void CustomKineticChainWithoutUnstableGroupIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = new TransitionChain(
            TransitionChainId.New(),
            "Custom stable",
            TransitionPattern.Custom,
            [model.StateChangeGroups[0].Id, model.StateChangeGroups[0].Id, model.StateChangeGroups[0].Id],
            [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
            GeneratesReverse: false);
        var transition = model.KineticTransitions[0] with { ChainId = chain.Id };

        var diagnostics = TransitionModelValidator.Validate(model with
        {
            Chains = [chain],
            KineticTransitions = [transition],
            MetropolisTransitions = [],
        });

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition);
    }

    [Fact]
    public void BuiltInUnstablePositionGroupMustContainExactlyOneVoidPair()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var stableGroup = model.StateChangeGroups.First(
            static group => group.Name == TransitionTestModels.ForwardGroup.Name);
        var chain = model.Chains.Single(
            static chain => chain.Pattern == TransitionPattern.Migration3) with
        {
            GroupIds = [stableGroup.Id, stableGroup.Id, stableGroup.Id],
        };
        var transition = model.KineticTransitions[0] with { ChainId = chain.Id };

        var diagnostics = TransitionModelValidator.Validate(model with
        {
            Chains = [chain],
            KineticTransitions = [transition],
            MetropolisTransitions = [],
        });

        Assert.Contains(
            diagnostics,
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidUnstablePositionGroup);
    }

    [Fact]
    public void SelfReferentialUnknownAndAsymmetricReverseLinksAreReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var kinetic = model.KineticTransitions[0];
        var self = kinetic with { ReverseTransitionId = kinetic.Id };
        var unknown = kinetic with { ReverseTransitionId = KineticTransitionId.New() };
        var first = kinetic with { Id = KineticTransitionId.New(), Name = "First" };
        var second = kinetic with
        {
            Id = KineticTransitionId.New(),
            Name = "Second",
            ReverseTransitionId = null,
        };
        first = first with { ReverseTransitionId = second.Id };

        Assert.Contains(
            TransitionModelValidator.Validate(model with { KineticTransitions = [self] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidReverseLink);
        Assert.Contains(
            TransitionModelValidator.Validate(model with { KineticTransitions = [unknown] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidReverseLink);
        Assert.Contains(
            TransitionModelValidator.Validate(model with { KineticTransitions = [first, second] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidReverseLink);
    }

    [Fact]
    public void CrossKindReverseLinkIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var metropolis = model.MetropolisTransitions[0];
        var kinetic = model.KineticTransitions[0] with
        {
            ReverseTransitionId = KineticTransitionId.Create(metropolis.Id.Value),
        };

        var diagnostics = TransitionModelValidator.Validate(model with
        {
            KineticTransitions = [kinetic],
        });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidReverseLink);
    }

    [Fact]
    public void KineticAndMetropolisNamesMayCoincideButNotWithinAKind()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var kinetic = model.KineticTransitions[0] with { Name = model.MetropolisTransitions[0].Name };
        var duplicateKinetic = kinetic with { Id = KineticTransitionId.New() };

        Assert.DoesNotContain(
            TransitionModelValidator.Validate(model with { KineticTransitions = [kinetic] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.DuplicateName);
        Assert.Contains(
            TransitionModelValidator.Validate(model with { KineticTransitions = [kinetic, duplicateKinetic] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.DuplicateName);
    }

    private static ModelDefinition CreateComplexModel(bool reverse)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var groups = model.StateChangeGroups;
        var chain = new TransitionChain(
            TransitionChainId.New(),
            "Vehicle",
            TransitionPattern.Vehicle5,
            [groups[0].Id, groups[0].Id, groups[2].Id, groups[0].Id, groups[0].Id],
            [
                TransitionLinker.Dynamic,
                TransitionLinker.Static,
                TransitionLinker.Static,
                TransitionLinker.Dynamic,
            ],
            GeneratesReverse: false);
        var first = new KineticTransition(
            KineticTransitionId.New(),
            "Forward",
            chain.Id,
            [
                new Fractional3(0.25, 0.25, 0.25),
                new Fractional3(0.25, 0.25, 0.25),
                new Fractional3(0.5, 0.25, 0.25),
                new Fractional3(0.75, 0.25, 0.25),
                new Fractional3(0.75, 0.25, 0.25),
            ],
            ReverseTransitionId: null);
        if (reverse)
        {
            var second = first with
            {
                Id = KineticTransitionId.New(),
                Name = "Reverse",
                ReverseTransitionId = first.Id,
            };
            first = first with { ReverseTransitionId = second.Id };
            return model with
            {
                Chains = [chain],
                KineticTransitions = [first, second],
                MetropolisTransitions = [],
            };
        }

        return model with
        {
            Chains = [chain],
            KineticTransitions = [first],
            MetropolisTransitions = [],
        };
    }
}
