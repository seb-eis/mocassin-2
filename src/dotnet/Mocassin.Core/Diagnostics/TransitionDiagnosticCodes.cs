// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by transition-model validation.
/// </summary>
/// <remarks>
/// Core owns the definition-level codes. <c>Mocassin.Symmetry.Transitions.TransitionPathValidator</c> emits the
/// resolved-path subset and references these constants.
/// </remarks>
public static class TransitionDiagnosticCodes
{
    /// <summary>
    /// A state change references an unknown particle.
    /// </summary>
    public const string UnknownParticle = "TRN001";

    /// <summary>
    /// A state change has the same donor and acceptor.
    /// </summary>
    public const string DonorEqualsAcceptor = "TRN002";

    /// <summary>
    /// Both donor and acceptor are the implicit void particle.
    /// </summary>
    public const string BothParticlesVoid = "TRN003";

    /// <summary>
    /// A void donor is replaced by a charged acceptor, which is likely an implicit reverse pair.
    /// </summary>
    public const string ChargedAcceptorFromVoid = "TRN004";

    /// <summary>
    /// A state-change group is empty or references an unknown state change.
    /// </summary>
    public const string InvalidGroup = "TRN005";

    /// <summary>
    /// A state-change group mixes physical exchanges with charge-state exchanges.
    /// </summary>
    public const string MixedGroupExchange = "TRN006";

    /// <summary>
    /// A chain's group count and linker count do not satisfy <c>linkers = groups - 1</c>.
    /// </summary>
    public const string GroupLinkerCountMismatch = "TRN007";

    /// <summary>
    /// A chain pattern has the wrong number of positions.
    /// </summary>
    public const string InvalidPatternPositionCount = "TRN008";

    /// <summary>
    /// A chain's linker sequence is invalid for its pattern or for the custom grammar.
    /// </summary>
    public const string InvalidLinkerSequence = "TRN009";

    /// <summary>
    /// A kinetic reference path has a different length than its chain.
    /// </summary>
    public const string PathLengthMismatch = "TRN010";

    /// <summary>
    /// A kinetic reference path endpoint is not stable.
    /// </summary>
    public const string UnstablePathEndpoint = "TRN011";

    /// <summary>
    /// A kinetic path position's group stability does not match the resolved site stability.
    /// </summary>
    public const string GroupStabilityMismatch = "TRN012";

    /// <summary>
    /// A kinetic reference path contains consecutive unstable positions.
    /// </summary>
    public const string ConsecutiveUnstablePositions = "TRN013";

    /// <summary>
    /// A kinetic reference path contains a repeated full encoded position.
    /// </summary>
    public const string RepeatedPathPosition = "TRN014";

    /// <summary>
    /// A Metropolis transition does not use a Metropolis-compatible chain.
    /// </summary>
    public const string InvalidMetropolisChain = "TRN015";

    /// <summary>
    /// A Metropolis site reference is unknown, unstable, or otherwise invalid.
    /// </summary>
    public const string InvalidMetropolisSite = "TRN016";

    /// <summary>
    /// A name is empty or duplicated within its own named collection.
    /// </summary>
    public const string DuplicateName = "TRN017";

    /// <summary>
    /// A complex transition has no explicitly defined reverse link.
    /// </summary>
    public const string MissingReverseLink = "TRN018";

    /// <summary>
    /// The model defines more than the supported number of transitions.
    /// </summary>
    public const string TransitionLimitExceeded = "TRN019";

    /// <summary>
    /// A kinetic path point does not match exactly one P1 position and owning reference site.
    /// </summary>
    public const string UnmatchedPathPosition = "TRN020";

    /// <summary>
    /// A chain references an unknown state-change group.
    /// </summary>
    public const string UnknownChainGroup = "TRN021";

    /// <summary>
    /// A kinetic chain has no unstable position.
    /// </summary>
    public const string KineticChainWithoutUnstablePosition = "TRN022";

    /// <summary>
    /// A kinetic transition uses a Metropolis or two-position custom chain.
    /// </summary>
    public const string KineticTransitionUsesMetropolisChain = "TRN023";

    /// <summary>
    /// A chain's <c>generatesReverse</c> flag disagrees with its pattern.
    /// </summary>
    public const string GeneratesReverseMismatch = "TRN024";

    /// <summary>
    /// A state-change group mixes stable-role and unstable-role state changes.
    /// </summary>
    public const string MixedGroupStability = "TRN025";

    /// <summary>
    /// An unstable position's group does not contain exactly one <c>[D, ∅]</c> state change.
    /// </summary>
    public const string InvalidUnstablePositionGroup = "TRN026";

    /// <summary>
    /// A reverse link is unknown, cross-kind, asymmetric, or self-referential.
    /// </summary>
    public const string InvalidReverseLink = "TRN027";

    /// <summary>
    /// A resolved kinetic path stability sequence does not match the built-in pattern sequence.
    /// </summary>
    public const string PathStabilityMismatch = "TRN028";
}
