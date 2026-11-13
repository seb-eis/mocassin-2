// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Core.Model.Transitions;

internal static partial class TransitionModelValidator
{
    private const int maxTransitions = 100;

    private static void ValidateTransitions(
        ModelDefinition model,
        DiagnosticBag bag)
    {
        var stateChanges = model.StateChanges.IsDefault ? ImmutableArray<StateChange>.Empty : model.StateChanges;
        var groups = model.StateChangeGroups.IsDefault
            ? ImmutableArray<StateChangeGroup>.Empty
            : model.StateChangeGroups;
        var chains = model.Chains.IsDefault ? ImmutableArray<TransitionChain>.Empty : model.Chains;
        var kineticTransitions = model.KineticTransitions.IsDefault
            ? ImmutableArray<KineticTransition>.Empty
            : model.KineticTransitions;
        var metropolisTransitions = model.MetropolisTransitions.IsDefault
            ? ImmutableArray<MetropolisTransition>.Empty
            : model.MetropolisTransitions;

        var chainById = chains
            .GroupBy(static chain => chain.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var groupById = groups
            .GroupBy(static group => group.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var stateChangeById = stateChanges
            .GroupBy(static stateChange => stateChange.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var kineticById = kineticTransitions
            .GroupBy(static transition => transition.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var metropolisById = metropolisTransitions
            .GroupBy(static transition => transition.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var kineticGuids = kineticById.Keys.Select(static id => id.Value).ToHashSet();
        var metropolisGuids = metropolisById.Keys.Select(static id => id.Value).ToHashSet();

        if (kineticTransitions.Length + metropolisTransitions.Length > maxTransitions)
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.TransitionLimitExceeded,
                $"The model defines {kineticTransitions.Length + metropolisTransitions.Length} transitions; " +
                $"the limit is {maxTransitions}.",
                "model",
                Guid.Empty,
                "transitions"));
        }

        ValidateKineticTransitions(
            kineticTransitions,
            chainById,
            groupById,
            stateChangeById,
            kineticById,
            metropolisGuids,
            bag);
        ValidateMetropolisTransitions(
            metropolisTransitions,
            chainById,
            groupById,
            stateChangeById,
            kineticGuids,
            metropolisById,
            bag);
    }

    private static void ValidateKineticTransitions(
        ImmutableArray<KineticTransition> transitions,
        Dictionary<TransitionChainId, TransitionChain> chainById,
        Dictionary<StateChangeGroupId, StateChangeGroup> groupById,
        Dictionary<StateChangeId, StateChange> stateChangeById,
        Dictionary<KineticTransitionId, KineticTransition> transitionById,
        HashSet<Guid> crossKindIds,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var transition in transitions)
        {
            ValidateName(transition.Name, "kineticTransition", transition.Id.Value, names, bag);
            ValidateKineticReverseLink(transition, transitionById, crossKindIds, bag);

            if (!chainById.TryGetValue(transition.ChainId, out var chain))
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain,
                    $"Kinetic transition references unknown chain '{transition.ChainId:D}'.",
                    "kineticTransition",
                    transition.Id.Value,
                    "chainId"));
                continue;
            }

            if (chain.Pattern == TransitionPattern.Metropolis2 ||
                (chain.Pattern == TransitionPattern.Custom && chain.GroupIds.Length == 2))
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain,
                    "Kinetic transition must not use a Metropolis or two-position custom chain.",
                    "kineticTransition",
                    transition.Id.Value,
                    "chainId"));
            }

            if (IsComplexPattern(chain.Pattern) && transition.ReverseTransitionId is null)
            {
                bag.Add(Warning(
                    TransitionDiagnosticCodes.MissingReverseLink,
                    "Complex transition has no explicitly defined reverse link.",
                    "kineticTransition",
                    transition.Id.Value,
                    "reverseTransitionId"));
            }

            ValidateUnstablePositions(chain, groupById, stateChangeById, bag);
            ValidateSpectatorPositions(chain, groupById, stateChangeById, bag);
        }
    }

    private static void ValidateMetropolisTransitions(
        ImmutableArray<MetropolisTransition> transitions,
        Dictionary<TransitionChainId, TransitionChain> chainById,
        Dictionary<StateChangeGroupId, StateChangeGroup> groupById,
        Dictionary<StateChangeId, StateChange> stateChangeById,
        HashSet<Guid> crossKindIds,
        Dictionary<MetropolisTransitionId, MetropolisTransition> transitionById,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var transition in transitions)
        {
            ValidateName(transition.Name, "metropolisTransition", transition.Id.Value, names, bag);
            ValidateMetropolisReverseLink(transition, transitionById, crossKindIds, bag);

            if (!chainById.TryGetValue(transition.ChainId, out var chain))
            {
                continue;
            }

            if (IsComplexPattern(chain.Pattern) && transition.ReverseTransitionId is null)
            {
                bag.Add(Warning(
                    TransitionDiagnosticCodes.MissingReverseLink,
                    "Complex transition has no explicitly defined reverse link.",
                    "metropolisTransition",
                    transition.Id.Value,
                    "reverseTransitionId"));
            }

            if (!chain.GroupIds.IsDefault &&
                chain.GroupIds.Any(groupId =>
                    groupById.TryGetValue(groupId, out var group) &&
                    GroupHasUnstableRole(group, stateChangeById)))
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.GroupStabilityMismatch,
                    "Metropolis chain positions must use stable-role groups.",
                    "metropolisTransition",
                    transition.Id.Value,
                    "chainId"));
            }
        }
    }

    private static void ValidateKineticReverseLink(
        KineticTransition transition,
        Dictionary<KineticTransitionId, KineticTransition> transitionById,
        HashSet<Guid> crossKindIds,
        DiagnosticBag bag)
    {
        if (transition.ReverseTransitionId is not { } reverseId)
        {
            return;
        }

        if (reverseId == transition.Id)
        {
            AddSelfReverseLink(transition.Id.Value, "kineticTransition", bag);
            return;
        }

        if (transitionById.TryGetValue(reverseId, out var reverse))
        {
            if (reverse.ReverseTransitionId != transition.Id)
            {
                AddAsymmetricReverseLink(transition.Id.Value, "kineticTransition", bag);
            }

            return;
        }

        AddMissingReverseLink(transition.Id.Value, "kineticTransition", crossKindIds.Contains(reverseId.Value), bag);
    }

    private static void ValidateMetropolisReverseLink(
        MetropolisTransition transition,
        Dictionary<MetropolisTransitionId, MetropolisTransition> transitionById,
        HashSet<Guid> crossKindIds,
        DiagnosticBag bag)
    {
        if (transition.ReverseTransitionId is not { } reverseId)
        {
            return;
        }

        if (reverseId == transition.Id)
        {
            AddSelfReverseLink(transition.Id.Value, "metropolisTransition", bag);
            return;
        }

        if (transitionById.TryGetValue(reverseId, out var reverse))
        {
            if (reverse.ReverseTransitionId != transition.Id)
            {
                AddAsymmetricReverseLink(transition.Id.Value, "metropolisTransition", bag);
            }

            return;
        }

        AddMissingReverseLink(transition.Id.Value, "metropolisTransition", crossKindIds.Contains(reverseId.Value), bag);
    }

    private static void ValidateSpectatorPositions(
        TransitionChain chain,
        Dictionary<StateChangeGroupId, StateChangeGroup> groupById,
        Dictionary<StateChangeId, StateChange> stateChangeById,
        DiagnosticBag bag)
    {
        var stabilitySequence = GetBuiltInStabilitySequence(chain.Pattern);
        if (stabilitySequence is null ||
            chain.GroupIds.IsDefault ||
            chain.GroupIds.Length != stabilitySequence.Value.Length)
        {
            return;
        }

        for (var index = 0; index < chain.GroupIds.Length; index++)
        {
            if (!groupById.TryGetValue(chain.GroupIds[index], out var group) ||
                group.StateChangeIds.IsDefault)
            {
                continue;
            }

            var hasSpectator = group.StateChangeIds.Any(stateChangeId =>
                stateChangeById.TryGetValue(stateChangeId, out var stateChange) &&
                stateChange.DonorParticleId == stateChange.AcceptorParticleId);
            if (!hasSpectator)
            {
                continue;
            }

            var isInteriorStable = index > 0 &&
                                   index < stabilitySequence.Value.Length - 1 &&
                                   stabilitySequence.Value[index] == SiteStability.Stable &&
                                   stabilitySequence.Value[index - 1] == SiteStability.Unstable &&
                                   stabilitySequence.Value[index + 1] == SiteStability.Unstable;
            if (!isInteriorStable)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.DonorEqualsAcceptor,
                    "A self state change is only valid at an interior stable spectator position with unstable neighbours.",
                    "chain",
                    chain.Id.Value,
                    "groupIds"));
            }
        }
    }

    private static void ValidateUnstablePositions(
        TransitionChain chain,
        Dictionary<StateChangeGroupId, StateChangeGroup> groupById,
        Dictionary<StateChangeId, StateChange> stateChangeById,
        DiagnosticBag bag)
    {
        var stabilitySequence = GetBuiltInStabilitySequence(chain.Pattern);
        if (stabilitySequence is null)
        {
            if (!chain.GroupIds.IsDefault &&
                !chain.GroupIds.Any(groupId =>
                    groupById.TryGetValue(groupId, out var group) &&
                    GroupHasUnstableRole(group, stateChangeById)))
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition,
                    "Kinetic custom chain has no unstable position.",
                    "chain",
                    chain.Id.Value,
                    "groupIds"));
            }

            return;
        }

        if (!stabilitySequence.Value.Contains(SiteStability.Unstable))
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition,
                "Kinetic chain has no unstable position.",
                "chain",
                chain.Id.Value,
                "groupIds"));
            return;
        }

        if (chain.GroupIds.IsDefault || chain.GroupIds.Length != stabilitySequence.Value.Length)
        {
            return;
        }

        for (var index = 0; index < stabilitySequence.Value.Length; index++)
        {
            if (stabilitySequence.Value[index] != SiteStability.Unstable)
            {
                continue;
            }

            if (!groupById.TryGetValue(chain.GroupIds[index], out var group))
            {
                continue;
            }

            var voidStateChangeCount = group.StateChangeIds
                .Where(stateChangeById.ContainsKey)
                .Count(stateChangeId => stateChangeById[stateChangeId].AcceptorParticleId == ParticleId.Void);
            if (voidStateChangeCount != 1)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.InvalidUnstablePositionGroup,
                    "Unstable position group must contain exactly one [donor, void] state change.",
                    "chain",
                    chain.Id.Value,
                    "groupIds"));
            }
        }
    }

    private static bool GroupHasUnstableRole(
        StateChangeGroup group,
        Dictionary<StateChangeId, StateChange> stateChangeById) =>
        !group.StateChangeIds.IsDefault &&
        group.StateChangeIds.Any(stateChangeId =>
            stateChangeById.TryGetValue(stateChangeId, out var stateChange) &&
            stateChange.AcceptorParticleId == ParticleId.Void);

    private static bool IsComplexPattern(TransitionPattern pattern) =>
        pattern is TransitionPattern.Interstitialcy5
            or TransitionPattern.Interstitialcy7
            or TransitionPattern.Vehicle5
            or TransitionPattern.Vehicle6
            or TransitionPattern.Custom;

    private static ImmutableArray<SiteStability>? GetBuiltInStabilitySequence(TransitionPattern pattern) => pattern switch
    {
        TransitionPattern.Metropolis2 => [SiteStability.Stable, SiteStability.Stable],
        TransitionPattern.Migration3 => [SiteStability.Stable, SiteStability.Unstable, SiteStability.Stable],
        TransitionPattern.Interstitialcy5 =>
        [
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
        ],
        TransitionPattern.Interstitialcy7 =>
        [
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
        ],
        TransitionPattern.Vehicle5 =>
        [
            SiteStability.Stable,
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
            SiteStability.Stable,
        ],
        TransitionPattern.Vehicle6 =>
        [
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
            SiteStability.Stable,
            SiteStability.Unstable,
            SiteStability.Stable,
        ],
        TransitionPattern.Custom => null,
        _ => null,
    };

    private static void AddSelfReverseLink(Guid transitionId, string domain, DiagnosticBag bag) =>
        bag.Add(Error(
            TransitionDiagnosticCodes.InvalidReverseLink,
            "Reverse link must not reference the transition itself.",
            domain,
            transitionId,
            "reverseTransitionId"));

    private static void AddAsymmetricReverseLink(Guid transitionId, string domain, DiagnosticBag bag) =>
        bag.Add(Error(
            TransitionDiagnosticCodes.InvalidReverseLink,
            "Reverse link must be symmetric; the referenced transition does not link back.",
            domain,
            transitionId,
            "reverseTransitionId"));

    private static void AddMissingReverseLink(
        Guid transitionId,
        string domain,
        bool crossKind,
        DiagnosticBag bag) =>
        bag.Add(Error(
            TransitionDiagnosticCodes.InvalidReverseLink,
            crossKind
                ? "Reverse link points to a transition of the other kind."
                : "Reverse link points to an unknown transition.",
            domain,
            transitionId,
            "reverseTransitionId"));
}
