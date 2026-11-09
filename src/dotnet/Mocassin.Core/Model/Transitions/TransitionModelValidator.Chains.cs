// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Core.Model.Transitions;

internal static partial class TransitionModelValidator
{
    private static void ValidateChains(
        ImmutableArray<TransitionChain> chains,
        ImmutableArray<StateChangeGroup> groups,
        DiagnosticBag bag)
    {
        var groupIds = groups.Select(static group => group.Id).ToHashSet();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var chain in chains)
        {
            ValidateName(chain.Name, "chain", chain.Id.Value, names, bag);

            var groupCount = chain.GroupIds.IsDefault ? 0 : chain.GroupIds.Length;
            var linkers = chain.Linkers.IsDefault ? ImmutableArray<TransitionLinker>.Empty : chain.Linkers;

            if (linkers.Length != Math.Max(groupCount - 1, 0))
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.GroupLinkerCountMismatch,
                    $"Chain has {groupCount} positions and {linkers.Length} linkers; expected {Math.Max(groupCount - 1, 0)} linkers.",
                    "chain",
                    chain.Id.Value,
                    "linkers"));
            }

            if (GetBuiltInPositionCount(chain.Pattern) is { } expectedPositionCount &&
                groupCount != expectedPositionCount)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.InvalidPatternPositionCount,
                    $"Chain pattern '{chain.Pattern}' requires {expectedPositionCount} positions but declares {groupCount}.",
                    "chain",
                    chain.Id.Value,
                    "groupIds"));
            }

            var validLinkers = chain.Pattern == TransitionPattern.Custom
                ? IsValidCustomLinkerSequence(groupCount, linkers)
                : IsValidBuiltInLinkerSequence(chain.Pattern, linkers);
            if (!validLinkers)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.InvalidLinkerSequence,
                    $"Chain linker sequence [{string.Join(", ", linkers)}] is invalid for pattern '{chain.Pattern}'.",
                    "chain",
                    chain.Id.Value,
                    "linkers"));
            }

            foreach (var groupId in chain.GroupIds.IsDefault ? [] : chain.GroupIds)
            {
                if (!groupIds.Contains(groupId))
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.UnknownChainGroup,
                        $"Chain references unknown state-change group '{groupId:D}'.",
                        "chain",
                        chain.Id.Value,
                        "groupIds"));
                }
            }

            var shouldGenerateReverse = chain.Pattern is TransitionPattern.Metropolis2 or TransitionPattern.Migration3;
            if (chain.GeneratesReverse != shouldGenerateReverse)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.GeneratesReverseMismatch,
                    $"Chain pattern '{chain.Pattern}' requires generatesReverse={shouldGenerateReverse.ToString().ToLowerInvariant()}.",
                    "chain",
                    chain.Id.Value,
                    "generatesReverse"));
            }
        }
    }

    private static int? GetBuiltInPositionCount(TransitionPattern pattern) => pattern switch
    {
        TransitionPattern.Metropolis2 => 2,
        TransitionPattern.Migration3 => 3,
        TransitionPattern.Interstitialcy5 => 5,
        TransitionPattern.Interstitialcy7 => 7,
        TransitionPattern.Vehicle5 => 5,
        TransitionPattern.Vehicle6 => 6,
        TransitionPattern.Custom => null,
        _ => null,
    };

    private static bool IsValidBuiltInLinkerSequence(
        TransitionPattern pattern,
        ImmutableArray<TransitionLinker> linkers)
    {
        return pattern switch
        {
            TransitionPattern.Metropolis2 =>
                linkers.SequenceEqual([TransitionLinker.Dynamic]),
            TransitionPattern.Migration3 =>
                linkers.SequenceEqual([TransitionLinker.Dynamic, TransitionLinker.Dynamic]),
            TransitionPattern.Interstitialcy5 =>
                linkers.SequenceEqual(
                [
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                ]),
            TransitionPattern.Interstitialcy7 =>
                linkers.SequenceEqual(
                [
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                ]),
            TransitionPattern.Vehicle5 =>
                linkers.SequenceEqual(
                [
                    TransitionLinker.Dynamic,
                    TransitionLinker.Static,
                    TransitionLinker.Static,
                    TransitionLinker.Dynamic,
                ]),
            TransitionPattern.Vehicle6 =>
                linkers.SequenceEqual(
                [
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Static,
                    TransitionLinker.Dynamic,
                    TransitionLinker.Dynamic,
                ]),
            TransitionPattern.Custom => false,
            _ => false,
        };
    }

    private static bool IsValidCustomLinkerSequence(
        int positionCount,
        ImmutableArray<TransitionLinker> linkers)
    {
        if (positionCount is < 2 or > 8 || linkers.Length != positionCount - 1)
        {
            return false;
        }

        if (linkers.All(static linker => linker == TransitionLinker.Dynamic))
        {
            return true;
        }

        if (positionCount == 2)
        {
            return false;
        }

        var index = 0;
        var dynamicPrefix = 0;
        while (index < linkers.Length && linkers[index] == TransitionLinker.Dynamic)
        {
            dynamicPrefix++;
            index++;
        }

        var staticRun = 0;
        while (index < linkers.Length && linkers[index] == TransitionLinker.Static)
        {
            staticRun++;
            index++;
        }

        var dynamicSuffix = 0;
        while (index < linkers.Length && linkers[index] == TransitionLinker.Dynamic)
        {
            dynamicSuffix++;
            index++;
        }

        if (index != linkers.Length || dynamicPrefix == 0 || staticRun == 0 || dynamicSuffix == 0)
        {
            return false;
        }

        // A single static connector between two single dynamic connectors is not a physical mechanism.
        return !(dynamicPrefix == 1 && staticRun == 1 && dynamicSuffix == 1);
    }
}
