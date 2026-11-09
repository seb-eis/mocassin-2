// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Transitions;
using Mocassin.Core.Tests.TestSupport;

namespace Mocassin.Core.Tests.Transitions;

[Trait("Tier", "PR-fast")]
public sealed class TransitionChainTests
{
    [Theory]
    [InlineData(TransitionPattern.Metropolis2)]
    [InlineData(TransitionPattern.Migration3)]
    [InlineData(TransitionPattern.Interstitialcy5)]
    [InlineData(TransitionPattern.Interstitialcy7)]
    [InlineData(TransitionPattern.Vehicle5)]
    [InlineData(TransitionPattern.Vehicle6)]
    [InlineData(TransitionPattern.Custom)]
    public void BuiltInAndCustomPatternsAcceptTheirValidSequences(TransitionPattern pattern)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, pattern);

        Assert.Empty(
            TransitionModelValidator.Validate(model with { Chains = [chain] })
                .Where(static diagnostic => diagnostic.Code.StartsWith("TRN", StringComparison.Ordinal)));
    }

    [Fact]
    public void GroupAndLinkerCountMismatchIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, TransitionPattern.Migration3) with
        {
            Linkers = [TransitionLinker.Dynamic],
        };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [chain] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.GroupLinkerCountMismatch);
    }

    [Fact]
    public void WrongPatternPositionCountIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, TransitionPattern.Metropolis2) with
        {
            GroupIds = CreateGroups(model, 3),
            Linkers = [TransitionLinker.Dynamic, TransitionLinker.Dynamic],
        };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [chain] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidPatternPositionCount);
    }

    [Fact]
    public void InvalidBuiltInLinkerSequenceIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, TransitionPattern.Migration3) with
        {
            Linkers = [TransitionLinker.Dynamic, TransitionLinker.Static],
        };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [chain] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidLinkerSequence);
    }

    [Fact]
    public void UnknownChainGroupIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, TransitionPattern.Migration3);
        chain = chain with
        {
            GroupIds = [StateChangeGroupId.New(), .. chain.GroupIds.Skip(1)],
        };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [chain] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.UnknownChainGroup);
    }

    [Fact]
    public void GeneratesReverseMismatchIsReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateChain(model, TransitionPattern.Metropolis2) with { GeneratesReverse = false };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [chain] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.GeneratesReverseMismatch);
    }

    [Fact]
    public void DuplicateChainNamesAreReported()
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var first = CreateChain(model, TransitionPattern.Metropolis2);
        var second = CreateChain(model, TransitionPattern.Migration3) with { Name = first.Name };

        var diagnostics = TransitionModelValidator.Validate(model with { Chains = [first, second] });

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.DuplicateName);
    }

    [Theory]
    [InlineData(2, new[] { TransitionLinker.Dynamic })]
    [InlineData(3, new[] { TransitionLinker.Dynamic, TransitionLinker.Dynamic })]
    [InlineData(5, new[] { TransitionLinker.Dynamic, TransitionLinker.Static, TransitionLinker.Static, TransitionLinker.Dynamic })]
    [InlineData(5, new[] { TransitionLinker.Dynamic, TransitionLinker.Dynamic, TransitionLinker.Static, TransitionLinker.Dynamic })]
    public void ValidCustomLinkerSequencesAreAccepted(int positions, TransitionLinker[] linkers)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateCustomChain(model, positions, linkers);

        Assert.DoesNotContain(
            TransitionModelValidator.Validate(model with { Chains = [chain] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidLinkerSequence);
    }

    [Theory]
    [InlineData(2, new[] { TransitionLinker.Static })]
    [InlineData(4, new[] { TransitionLinker.Dynamic, TransitionLinker.Static, TransitionLinker.Dynamic })]
    [InlineData(4, new[] { TransitionLinker.Static, TransitionLinker.Dynamic, TransitionLinker.Dynamic })]
    [InlineData(4, new[] { TransitionLinker.Dynamic, TransitionLinker.Dynamic, TransitionLinker.Static })]
    [InlineData(5, new[] { TransitionLinker.Static, TransitionLinker.Dynamic, TransitionLinker.Dynamic, TransitionLinker.Dynamic })]
    public void InvalidCustomLinkerSequencesAreRejected(int positions, TransitionLinker[] linkers)
    {
        var model = TransitionTestModels.CreateCombinedModel();
        var chain = CreateCustomChain(model, positions, linkers);

        Assert.Contains(
            TransitionModelValidator.Validate(model with { Chains = [chain] }),
            static diagnostic => diagnostic.Code == TransitionDiagnosticCodes.InvalidLinkerSequence);
    }

    private static TransitionChain CreateChain(ModelDefinition model, TransitionPattern pattern)
    {
        var (positions, linkers, generatesReverse) = pattern switch
        {
            TransitionPattern.Metropolis2 => (2, new[] { TransitionLinker.Dynamic }, true),
            TransitionPattern.Migration3 => (3, new[] { TransitionLinker.Dynamic, TransitionLinker.Dynamic }, true),
            TransitionPattern.Interstitialcy5 => (5, Repeat(TransitionLinker.Dynamic, 4), false),
            TransitionPattern.Interstitialcy7 => (7, Repeat(TransitionLinker.Dynamic, 6), false),
            TransitionPattern.Vehicle5 => (
                5,
                new[] { TransitionLinker.Dynamic, TransitionLinker.Static, TransitionLinker.Static, TransitionLinker.Dynamic },
                false),
            TransitionPattern.Vehicle6 => (
                6,
                new[]
                {
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Static,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                },
                false),
            TransitionPattern.Custom => (2, new[] { TransitionLinker.Dynamic }, false),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
        };

        return new TransitionChain(
            TransitionChainId.New(),
            pattern.ToString(),
            pattern,
            CreateGroups(model, positions),
            linkers.ToImmutableArray(),
            generatesReverse);
    }

    private static TransitionChain CreateCustomChain(
        ModelDefinition model,
        int positions,
        TransitionLinker[] linkers) =>
        new(
            TransitionChainId.New(),
            $"Custom {positions}",
            TransitionPattern.Custom,
            CreateGroups(model, positions),
            linkers.ToImmutableArray(),
            GeneratesReverse: false);

    private static ImmutableArray<StateChangeGroupId> CreateGroups(ModelDefinition model, int count)
    {
        var groupIds = model.StateChangeGroups.Select(static group => group.Id).ToArray();
        return [.. Enumerable.Range(0, count).Select(index => groupIds[index % groupIds.Length])];
    }

    private static TransitionLinker[] Repeat(TransitionLinker linker, int count) =>
        [.. Enumerable.Repeat(linker, count)];
}
