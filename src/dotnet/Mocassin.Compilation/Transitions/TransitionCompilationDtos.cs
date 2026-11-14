// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Core.Geometry;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using static Mocassin.Compilation.Transitions.TransitionRuleHelpers;

namespace Mocassin.Compilation.Transitions;

internal sealed class DirectionDraft
{
    public DirectionDraft(
        int collectionIndex,
        TransitionRef transition,
        TransitionChain chain,
        bool isKinetic,
        bool isInverse,
        Vector4I[] absolute,
        Vector4I[] relative,
        int startP,
        SiteStability[] stability,
        string key,
        string? forwardKey)
    {
        CollectionIndex = collectionIndex;
        Transition = transition;
        Chain = chain;
        IsKinetic = isKinetic;
        IsInverse = isInverse;
        Absolute = absolute;
        Relative = relative;
        StartP = startP;
        Stability = stability;
        Key = key;
        ForwardKey = forwardKey;
    }

    public int CollectionIndex { get; }

    public TransitionRef Transition { get; }

    public TransitionChain Chain { get; }

    public bool IsKinetic { get; }

    public bool IsInverse { get; }

    public Vector4I[] Absolute { get; }

    public Vector4I[] Relative { get; }

    public int StartP { get; }

    public SiteStability[] Stability { get; }

    public string Key { get; }

    public string? ForwardKey { get; }

    public int Index { get; set; }

    public List<int> EquivalentStarts { get; } = [];

    public DirectionDraft Reverse()
    {
        var reversed = Absolute.Reverse().ToArray();
        var start = reversed[0];
        var relative = reversed
            .Select(encoding => new Vector4I(
                encoding.A - start.A,
                encoding.B - start.B,
                encoding.C - start.C,
                encoding.P))
            .ToArray();
        return new DirectionDraft(
            CollectionIndex,
            Transition,
            Chain,
            IsKinetic,
            isInverse: true,
            reversed,
            relative,
            start.P,
            Stability.Reverse().ToArray(),
            DirectionKey(CollectionIndex, relative, isInverse: true),
            Key);
    }
}

internal sealed record TransitionWork(
    int CollectionIndex,
    TransitionRef Transition,
    TransitionChain Chain,
    bool IsKinetic);

internal sealed record RuleOutcome(
    ulong State0,
    ulong State1,
    ulong State2,
    ImmutableArray<Fractional3> Movement,
    Cartesian3 ChargeTransportVector,
    bool IsInverse)
{
    public string OutcomeKey => string.Create(
        CultureInfo.InvariantCulture,
        $"{State0}|{State1}|{State2}|{MovementKey(Movement)}|{ChargeTransportVector.X:R},{ChargeTransportVector.Y:R},{ChargeTransportVector.Z:R}");

    private static string MovementKey(ImmutableArray<Fractional3> movement) =>
        string.Join(
            ';',
            movement.Select(static value => string.Create(
                CultureInfo.InvariantCulture,
                $"{value.A:R},{value.B:R},{value.C:R}")));
}

internal sealed class RuleDraft(
    int collectionIndex,
    ulong state0,
    ulong state1,
    ulong state2,
    string outcomeKey,
    DirectionDraft representativeDirection,
    DirectionDraft firstDirection,
    List<int> directionIndices,
    ImmutableArray<Fractional3> movement,
    Cartesian3 chargeTransportVector,
    bool isInverse,
    string provisionalRuleId)
{
    public int CollectionIndex { get; } = collectionIndex;

    public ulong State0 { get; } = state0;

    public ulong State1 { get; } = state1;

    public ulong State2 { get; } = state2;

    public string OutcomeKey { get; } = outcomeKey;

    public DirectionDraft RepresentativeDirection { get; set; } = representativeDirection;

    public DirectionDraft FirstDirection { get; } = firstDirection;

    public List<int> DirectionIndices { get; } = directionIndices;

    public ImmutableArray<Fractional3> Movement { get; } = movement;

    public Cartesian3 ChargeTransportVector { get; } = chargeTransportVector;

    public bool IsInverse { get; } = isInverse;

    public string ProvisionalRuleId { get; } = provisionalRuleId;
}
