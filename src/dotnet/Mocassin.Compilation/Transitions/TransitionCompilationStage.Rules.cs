// SPDX-License-Identifier: MIT

using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Compilation.Diagnostics;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Model.Structure;
using static Mocassin.Compilation.Transitions.TransitionRuleHelpers;

namespace Mocassin.Compilation.Transitions;

internal sealed partial class TransitionCompilationStage
{
    private static RuleOutcome? Evaluate(
        CompilationContext context,
        DirectionDraft direction,
        int[] state0,
        int[] state1,
        int[] state2,
        bool allowVoidDonor,
        int[] rejectionCounts)
    {
        for (var node = 0; node < state0.Length; node++)
        {
            if (direction.Stability[node] == SiteStability.Stable
                && state0[node] == 0
                && !allowVoidDonor)
            {
                rejectionCounts[2]++;
                return null;
            }
        }

        if (!MassConserved(context, state0, state2))
        {
            rejectionCounts[0]++;
            return null;
        }

        if (!ChargeConserved(context, state0, state2))
        {
            rejectionCounts[1]++;
            return null;
        }

        var transport = ComputeTransport(context, direction, state0, state2);
        if (transport is null)
        {
            rejectionCounts[0]++;
            return null;
        }

        return new RuleOutcome(
            EncodeState(state0),
            EncodeState(state1),
            EncodeState(state2),
            [.. transport.Value.Movement],
            transport.Value.ChargeTransportVector,
            IsInverse: false);
    }

    private static void AddRule(
        CompilationContext context,
        DirectionDraft direction,
        RuleOutcome outcome,
        Dictionary<(int Collection, int StartP, ulong State0), RuleDraft> rules,
        out CompilationStageOutcome? ambiguity)
    {
        ambiguity = null;
        if (rules.Count >= context.Options.MaxRuleCount)
        {
            ambiguity = CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.CompileLimit,
                $"Rule generation exceeded the hard limit {context.Options.MaxRuleCount}.");
            return;
        }

        var key = (direction.CollectionIndex, direction.StartP, outcome.State0);
        if (rules.TryGetValue(key, out var existing))
        {
            if (existing.OutcomeKey == outcome.OutcomeKey)
            {
                if (!existing.DirectionIndices.Contains(direction.Index))
                {
                    existing.DirectionIndices.Add(direction.Index);
                }

                if (direction.Index < existing.RepresentativeDirection.Index)
                {
                    existing.RepresentativeDirection = direction;
                }

                return;
            }

            ambiguity = CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.RuleAmbiguity,
                $"Two rules in collection {direction.CollectionIndex} share state0 {outcome.State0} with different outcomes " +
                $"('{existing.ProvisionalRuleId}' and '{ComputeRuleGeometryId(outcome, direction)}').",
                new ModelPath(
                    direction.Transition.IsKinetic ? "kineticTransition" : "metropolisTransition",
                    direction.Transition.Id,
                    "state0"));
            return;
        }

        rules.Add(key, new RuleDraft(
            direction.CollectionIndex,
            outcome.State0,
            outcome.State1,
            outcome.State2,
            outcome.OutcomeKey,
            direction,
            direction,
            [direction.Index],
            outcome.Movement,
            outcome.ChargeTransportVector,
            outcome.IsInverse,
            ComputeRuleGeometryId(outcome, direction)));
    }
}
