// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Globalization;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Context;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Symmetry.Transitions;
using static Mocassin.Compilation.Transitions.TransitionRuleHelpers;

namespace Mocassin.Compilation.Transitions;

/// <summary>
/// Expands transition paths and compiles forward, inverse, and merged jump rules.
/// </summary>
internal sealed class TransitionCompilationStage : ICompilationStage
{

    /// <inheritdoc />
    public CompilationStage Stage => CompilationStage.Rules;

    /// <inheritdoc />
    public CompilationStageOutcome Execute(CompilationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Structure is null || context.Encoder is null || context.Cell is null || context.Setting is null
            || context.Particles is null)
        {
            throw new InvalidOperationException("The rule stage requires the compiled structure and particles.");
        }

        if (!TryBuildWorks(context, out var works, out var workDiagnostics))
        {
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.Precondition,
                "A simulation transition cannot be resolved for compilation.",
                workDiagnostics);
        }

        if (works.Length == 0)
        {
            return new CompilationStageOutcome(
                context with { Transitions = CompiledTransitionModel.Empty },
                []);
        }

        if (!TransitionPathResolver.TryCreate(context.Project.Structure, context.Catalog, out var resolver, out var resolverDiagnostics)
            || resolver is null)
        {
            return CompilationFailure.FromSource(
                context,
                CompilationErrorCodes.PathExpansion,
                "The transition path resolver cannot be built.",
                resolverDiagnostics);
        }

        var drafts = new List<DirectionDraft>();
        var inverseByForward = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var work in works)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryBuildDirections(context, resolver, work, drafts, inverseByForward, out var diagnostics))
            {
                return CompilationFailure.FromSource(
                    context,
                    CompilationErrorCodes.PathExpansion,
                    "A transition path cannot be expanded into compiled directions.",
                    diagnostics);
            }
        }

        drafts.Sort(CompareDrafts);
        for (var index = 0; index < drafts.Count; index++)
        {
            drafts[index].Index = index;
        }

        var ruleStore = new Dictionary<(int Collection, int StartP, ulong State0), RuleDraft>();
        var rejectionCounts = new int[3];
        foreach (var direction in drafts.Where(static direction => !direction.IsInverse))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGenerateForwardRules(context, direction, ruleStore, rejectionCounts, out var error))
            {
                return error!;
            }
        }

        var inverseDrafts = drafts.Where(static direction => direction.IsInverse).ToArray();
        foreach (var inverse in inverseDrafts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var forwardKey = inverse.ForwardKey;
            if (forwardKey is null || !inverseByForward.ContainsKey(forwardKey))
            {
                continue;
            }

            var forward = drafts.FirstOrDefault(direction => string.Equals(direction.Key, forwardKey, StringComparison.Ordinal));
            if (forward is null)
            {
                continue;
            }

            if (!TryGenerateInverseRules(context, forward, inverse, ruleStore, rejectionCounts, out var error))
            {
                return error!;
            }
        }

        foreach (var work in works)
        {
            if (!ruleStore.Keys.Any(key => key.Collection == work.CollectionIndex))
            {
                return CompilationFailure.FromMessage(
                    context,
                    CompilationErrorCodes.NoValidRules,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Transition '{work.Transition.Id:D}' produced no valid rules after conservation filtering " +
                        $"(mass {rejectionCounts[0]}, charge {rejectionCounts[1]}, invalid state {rejectionCounts[2]})."),
                    new ModelPath(work.IsKinetic ? "kineticTransition" : "metropolisTransition", work.Transition.Id));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var rulesByCollection = ruleStore.Values
            .OrderBy(static rule => rule.CollectionIndex)
            .ThenBy(static rule => rule.State0)
            .ThenBy(static rule => rule.State1)
            .ThenBy(static rule => rule.State2)
            .ThenBy(static rule => rule.RepresentativeDirection.Index)
            .ToArray();
        var rules = ImmutableArray.CreateBuilder<CompiledJumpRule>(rulesByCollection.Length);
        for (var index = 0; index < rulesByCollection.Length; index++)
        {
            var draft = rulesByCollection[index];
            if (index % 250 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var representative = draft.RepresentativeDirection;
            var ruleGeometryId = ComputeRuleGeometryId(draft, representative);
            rules.Add(new CompiledJumpRule(
                index,
                ruleGeometryId,
                draft.CollectionIndex,
                representative.Index,
                [.. draft.DirectionIndices.Order()],
                draft.State0,
                draft.State1,
                draft.State2,
                [.. draft.Movement],
                draft.ChargeTransportVector,
                StaticCorrection: null,
                [],
                draft.IsInverse,
                CompiledJumpRule.DefaultFrequencyHz));
        }

        var directionMovements = rules
            .GroupBy(static rule => rule.DirectionIndex)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Movement);
        var directions = ImmutableArray.CreateBuilder<CompiledJumpDirection>(drafts.Count);
        foreach (var draft in drafts)
        {
            var movement = directionMovements.TryGetValue(draft.Index, out var value)
                ? value
                : [.. Enumerable.Repeat(Fractional3.Zero, draft.Absolute.Length)];
            var pathVector = SumMovement(context.Cell, movement);
            directions.Add(new CompiledJumpDirection(
                draft.Index,
                ComputeDirectionId(draft),
                draft.CollectionIndex,
                draft.StartP,
                [.. draft.Relative],
                movement,
                pathVector,
                pathVector.Length,
                draft.IsInverse,
                [.. draft.EquivalentStarts.Order()]));
        }

        var collections = ImmutableArray.CreateBuilder<CompiledJumpCollection>(works.Length);
        foreach (var work in works)
        {
            var collectionRules = rules.Where(rule => rule.CollectionIndex == work.CollectionIndex).ToArray();
            var collectionDirections = directions
                .Where(direction => direction.CollectionIndex == work.CollectionIndex)
                .Select(static direction => direction.Index)
                .ToArray();
            var mask = collectionRules
                .SelectMany(static rule => DecodeState(rule.State0, rule.Movement.Length))
                .Where(static particle => particle != 0)
                .Aggregate(0UL, static (current, particle) => current | (1UL << particle));
            collections.Add(new CompiledJumpCollection(
                work.CollectionIndex,
                work.Transition,
                work.IsKinetic ? Core.Model.Simulations.SimulationKind.Kmc : Core.Model.Simulations.SimulationKind.Mmc,
                mask,
                [.. collectionDirections],
                [.. collectionRules.Select(static rule => rule.Index)]));
        }

        var model = new CompiledTransitionModel(
            collections.MoveToImmutable(),
            directions.MoveToImmutable(),
            rules.MoveToImmutable(),
            [],
            [],
            [],
            context.Particles.Count,
            context.Structure.PositionCount);
        return new CompilationStageOutcome(context with { Transitions = model }, []);
    }

    private static bool TryBuildWorks(
        CompilationContext context,
        out ImmutableArray<TransitionWork> works,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var bag = new DiagnosticBag();
        var references = context.Simulations
            .SelectMany(static simulation => simulation.TransitionIds.IsDefault ? [] : simulation.TransitionIds)
            .Distinct()
            .ToArray();
        var kinetic = references
            .Where(static reference => reference.IsKinetic)
            .OrderBy(static reference => reference.Id)
            .ToArray();
        var metropolis = references
            .Where(static reference => !reference.IsKinetic)
            .OrderBy(static reference => reference.Id)
            .ToArray();
        var builder = ImmutableArray.CreateBuilder<TransitionWork>(kinetic.Length + metropolis.Length);
        var collection = 0;
        foreach (var reference in kinetic)
        {
            var transition = context.Project.Find(new KineticTransitionId(reference.Id));
            var chain = transition is null ? null : context.Project.Find(transition.ChainId);
            if (transition is null || chain is null)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.Precondition,
                    DiagnosticSeverity.Error,
                    $"Simulation kinetic transition '{reference.Id:D}' cannot be resolved.",
                    new ModelPath("kineticTransition", reference.Id)));
                continue;
            }

            builder.Add(new TransitionWork(collection++, reference, chain, IsKinetic: true));
        }

        foreach (var reference in metropolis)
        {
            var transition = context.Project.Find(new MetropolisTransitionId(reference.Id));
            var chain = transition is null ? null : context.Project.Find(transition.ChainId);
            if (transition is null || chain is null)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.Precondition,
                    DiagnosticSeverity.Error,
                    $"Simulation Metropolis transition '{reference.Id:D}' cannot be resolved.",
                    new ModelPath("metropolisTransition", reference.Id)));
                continue;
            }

            builder.Add(new TransitionWork(collection++, reference, chain, IsKinetic: false));
        }

        works = builder.ToImmutable();
        diagnostics = bag.Sorted;
        return !bag.HasErrors;
    }

    private static bool TryBuildDirections(
        CompilationContext context,
        TransitionPathResolver resolver,
        TransitionWork work,
        List<DirectionDraft> drafts,
        Dictionary<string, string> inverseByForward,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        var bag = new DiagnosticBag();
        var setting = context.Setting!;
        var encoder = resolver.Encoder;
        var forwardDrafts = new List<DirectionDraft>();
        if (work.IsKinetic)
        {
            var transition = context.Project.Find(new KineticTransitionId(work.Transition.Id))!;
            if (!resolver.TryResolvePath(transition.ReferencePath, out var referencePositions, out var referenceDiagnostics))
            {
                bag.AddRange(referenceDiagnostics);
                diagnostics = bag.Sorted;
                return false;
            }

            foreach (var operation in setting.Operations)
            {
                var transformed = referencePositions
                    .Select(position => operation.Transform(encoder.Decode(position.Encoding)))
                    .ToImmutableArray();
                if (!resolver.TryResolvePath(transformed, out var resolved, out var pathDiagnostics))
                {
                    bag.AddRange(pathDiagnostics);
                    continue;
                }

                AddDraft(context, drafts, forwardDrafts, work, resolved, IsInverse: false, forwardKey: null);
            }
        }
        else
        {
            var transition = context.Project.Find(new MetropolisTransitionId(work.Transition.Id))!;
            var siteA = context.Project.Find(transition.SiteAId);
            var siteB = context.Project.Find(transition.SiteBId);
            if (siteA is null || siteB is null)
            {
                bag.Add(new Diagnostic(
                    CompilationErrorCodes.PathExpansion,
                    DiagnosticSeverity.Error,
                    "A Metropolis transition references an unknown site.",
                    new ModelPath("metropolisTransition", work.Transition.Id)));
                diagnostics = bag.Sorted;
                return false;
            }

            foreach (var operation in setting.Operations)
            {
                var resolvedB = (ResolvedPathPosition?)null;
                ImmutableArray<Diagnostic> bDiagnostics = [];
                if (!resolver.TryResolvePoint(operation.Transform(siteA.Fractional), out var resolvedA, out var aDiagnostics)
                    || resolvedA is null
                    || !resolver.TryResolvePoint(operation.Transform(siteB.Fractional), out resolvedB, out bDiagnostics)
                    || resolvedB is null)
                {
                    bag.AddRange(aDiagnostics);
                    bag.AddRange(bDiagnostics);
                    continue;
                }

                AddDraft(context, drafts, forwardDrafts, work, [resolvedA, resolvedB], IsInverse: false, forwardKey: null);
            }
        }

        if (work.Chain.GeneratesReverse)
        {
            foreach (var forward in forwardDrafts)
            {
                var reverse = forward.Reverse();
                var key = reverse.Key;
                if (drafts.All(draft => !string.Equals(draft.Key, key, StringComparison.Ordinal)))
                {
                    drafts.Add(reverse);
                }

                inverseByForward[forward.Key] = key;
            }
        }

        diagnostics = bag.Sorted;
        return !bag.HasErrors;
    }

    private static void AddDraft(
        CompilationContext context,
        List<DirectionDraft> all,
        List<DirectionDraft> forward,
        TransitionWork work,
        ImmutableArray<ResolvedPathPosition> resolved,
        bool IsInverse,
        string? forwardKey)
    {
        if (resolved.IsDefaultOrEmpty)
        {
            return;
        }

        var start = resolved[0].Encoding;
        var absolute = resolved.Select(static position => position.Encoding).ToArray();
        var relative = absolute
            .Select(encoding => new Vector4I(
                encoding.A - start.A,
                encoding.B - start.B,
                encoding.C - start.C,
                encoding.P))
            .ToArray();
        var key = DirectionKey(relative, IsInverse);
        var existing = all.FirstOrDefault(draft => string.Equals(draft.Key, key, StringComparison.Ordinal));
        if (existing is not null)
        {
            if (!existing.EquivalentStarts.Contains(resolved[0].P))
            {
                existing.EquivalentStarts.Add(resolved[0].P);
            }

            return;
        }

        var draft = new DirectionDraft(
            work.CollectionIndex,
            work.Transition,
            work.Chain,
            work.IsKinetic,
            IsInverse,
            absolute,
            relative,
            resolved[0].P,
            resolved.Select(static position => position.Stability).ToArray(),
            key,
            forwardKey);
        all.Add(draft);
        if (!IsInverse)
        {
            forward.Add(draft);
        }
    }

    private static bool TryGenerateForwardRules(
        CompilationContext context,
        DirectionDraft direction,
        Dictionary<(int Collection, int StartP, ulong State0), RuleDraft> rules,
        int[] rejectionCounts,
        out CompilationStageOutcome? error)
    {
        error = null;
        var chain = direction.Chain;
        var groups = chain.GroupIds.IsDefault ? [] : chain.GroupIds;
        if (groups.Length != direction.Absolute.Length)
        {
            error = CompilationFailure.FromMessage(
                context,
                CompilationErrorCodes.Conservation,
                "The transition chain length does not match the compiled path length.");
            return false;
        }

        var choices = new List<List<(int S0, int S1, int S2)>>(groups.Length);
        for (var node = 0; node < groups.Length; node++)
        {
            var group = context.Project.Find(groups[node]);
            if (group is null)
            {
                error = CompilationFailure.FromMessage(
                    context,
                    CompilationErrorCodes.Conservation,
                    $"The transition chain references an unknown state-change group '{groups[node]:D}'.");
                return false;
            }

            var nodeChoices = new List<(int S0, int S1, int S2)>();
            var stateChanges = group.StateChangeIds.IsDefault ? [] : group.StateChangeIds;
            if (direction.Stability[node] == SiteStability.Stable)
            {
                foreach (var stateChangeId in stateChanges)
                {
                    var stateChange = context.Project.Find(stateChangeId);
                    if (stateChange is null)
                    {
                        error = CompilationFailure.FromMessage(
                            context,
                            CompilationErrorCodes.Conservation,
                            $"A state-change group references an unknown state change '{stateChangeId:D}'.");
                        return false;
                    }

                    if (!context.Particles!.TryGetIndex(stateChange.DonorParticleId, out var donor) ||
                        !context.Particles.TryGetIndex(stateChange.AcceptorParticleId, out var acceptor))
                    {
                        error = CompilationFailure.FromMessage(
                            context,
                            CompilationErrorCodes.Conservation,
                            "A state change references an uncompiled particle.");
                        return false;
                    }

                    nodeChoices.Add((donor, 0, acceptor));
                }
            }
            else
            {
                var valid = stateChanges
                    .Select(context.Project.Find)
                    .Where(static stateChange => stateChange is not null)
                    .Select(static stateChange => stateChange!)
                    .Where(static stateChange => stateChange.AcceptorParticleId == ParticleId.Void)
                    .ToArray();
                if (valid.Length != 1 ||
                    !context.Particles!.TryGetIndex(valid[0].DonorParticleId, out var donor))
                {
                    rejectionCounts[2]++;
                    continue;
                }

                nodeChoices.Add((0, donor, 0));
            }

            if (nodeChoices.Count == 0)
            {
                rejectionCounts[2]++;
                continue;
            }

            choices.Add(nodeChoices);
        }

        if (choices.Count != groups.Length)
        {
            return true;
        }

        var state0 = new int[groups.Length];
        var state1 = new int[groups.Length];
        var state2 = new int[groups.Length];
        var allowVoidDonor = direction.Transition.IsKinetic
            && context.Project.Find(new KineticTransitionId(direction.Transition.Id))?.ReverseTransitionId is not null;
        CompilationStageOutcome? failure = null;
        Enumerate(0);

        void Enumerate(int node)
        {
            if (node == choices.Count)
            {
                var outcome = Evaluate(context, direction, state0, state1, state2, allowVoidDonor, rejectionCounts);
                if (outcome is null)
                {
                    return;
                }

                AddRule(context, direction, outcome, rules, out var ambiguity);
                if (ambiguity is not null)
                {
                    failure ??= ambiguity;
                }

                return;
            }

            foreach (var choice in choices[node])
            {
                state0[node] = choice.S0;
                state1[node] = choice.S1;
                state2[node] = choice.S2;
                Enumerate(node + 1);
                if (failure is not null)
                {
                    return;
                }
            }
        }

        error = failure;
        return failure is null;
    }

    private static bool TryGenerateInverseRules(
        CompilationContext context,
        DirectionDraft forward,
        DirectionDraft inverse,
        Dictionary<(int Collection, int StartP, ulong State0), RuleDraft> rules,
        int[] rejectionCounts,
        out CompilationStageOutcome? error)
    {
        error = null;
        var forwardRules = rules.Values
            .Where(rule => rule.DirectionIndices.Contains(forward.Index))
            .ToArray();
        foreach (var rule in forwardRules)
        {
            var state0 = DecodeState(rule.State2, inverse.Absolute.Length);
            var state1 = DecodeState(rule.State1, inverse.Absolute.Length);
            var state2 = DecodeState(rule.State0, inverse.Absolute.Length);
            var outcome = Evaluate(
                context,
                inverse,
                [.. state0],
                [.. state1],
                [.. state2],
                allowVoidDonor: false,
                rejectionCounts);
            if (outcome is null)
            {
                continue;
            }

            outcome = outcome with { IsInverse = true };
            AddRule(context, inverse, outcome, rules, out var ambiguity);
            if (ambiguity is not null)
            {
                error = ambiguity;
                return false;
            }
        }

        return true;
    }

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
                CompilationErrorCodes.RuleAmbiguity,
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
