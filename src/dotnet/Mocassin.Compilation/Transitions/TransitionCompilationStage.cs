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

using Mocassin.Compilation.Diagnostics;

namespace Mocassin.Compilation.Transitions;

/// <summary>
/// Expands transition paths and compiles forward, inverse, and merged jump rules.
/// </summary>
internal sealed partial class TransitionCompilationStage : ICompilationStage
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

        if (context.Cache is not null &&
            context.Cache.TryGet<CompiledTransitionModel>(StageCacheKeys.Rules(context), out var cachedRules) &&
            cachedRules is not null &&
            cachedRules.ParticleCount == context.Particles.Count &&
            cachedRules.PositionCount == context.Structure.PositionCount)
        {
            return new CompilationStageOutcome(context with { Transitions = cachedRules }, []);
        }

        if (!TryBuildWorks(context, out var works, out var workDiagnostics))
        {
            return CompilationFailure.FromSource(
                context,
                CompilationDiagnosticCodes.Precondition,
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
                CompilationDiagnosticCodes.PathExpansion,
                "The transition path resolver cannot be built.",
                resolverDiagnostics);
        }

        var warnings = new DiagnosticBag();
        AddUnusedTransitionWarnings(context, works, warnings);
        var drafts = new List<DirectionDraft>();
        var inverseByForward = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var work in works)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryBuildDirections(context, resolver, work, drafts, inverseByForward, out var diagnostics))
            {
                return CompilationFailure.FromSource(
                    context,
                    CompilationDiagnosticCodes.PathExpansion,
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
                    CompilationDiagnosticCodes.NoValidRules,
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

        if (rulesByCollection.Length > context.Options.MaxRuleCount)
        {
            return CompilationFailure.FromMessage(
                context,
                CompilationDiagnosticCodes.CompileLimit,
                $"The compiled rule count {rulesByCollection.Length} exceeds the hard limit {context.Options.MaxRuleCount}.");
        }

        if (rulesByCollection.Length > context.Options.AdvisoryRuleCount)
        {
            warnings.Add(new Diagnostic(
                CompilationDiagnosticCodes.AdvisoryBudget,
                DiagnosticSeverity.Warning,
                $"The compiled rule count {rulesByCollection.Length} exceeds the advisory budget " +
                $"{context.Options.AdvisoryRuleCount}; the hard limit was not exceeded."));
        }

        var model = new CompiledTransitionModel(
            collections.MoveToImmutable(),
            directions.MoveToImmutable(),
            rules.MoveToImmutable(),
            [],
            [],
            [],
            context.Particles.Count,
            context.Structure.PositionCount)
        {
            RejectedByMass = rejectionCounts[0],
            RejectedByCharge = rejectionCounts[1],
            RejectedByInvalidState = rejectionCounts[2],
        };
        context.Cache?.GetOrAdd(StageCacheKeys.Rules(context), () => model);
        return new CompilationStageOutcome(context with { Transitions = model }, warnings.Sorted);
    }

    private static void AddUnusedTransitionWarnings(
        CompilationContext context,
        ImmutableArray<TransitionWork> works,
        DiagnosticBag bag)
    {
        if (!context.Options.EmitUnusedTransitionWarnings)
        {
            return;
        }

        var referenced = works.Select(static work => work.Transition).ToHashSet();
        if (!context.Project.KineticTransitions.IsDefault)
        {
            foreach (var transition in context.Project.KineticTransitions)
            {
                var reference = TransitionRef.From(transition.Id);
                if (!referenced.Contains(reference))
                {
                    bag.Add(new Diagnostic(
                        CompilationDiagnosticCodes.UnusedTransition,
                        DiagnosticSeverity.Warning,
                        $"Kinetic transition '{transition.Name}' is not referenced by any simulation and was not compiled.",
                        new ModelPath("kineticTransition", transition.Id.Value)));
                }
            }
        }

        if (!context.Project.MetropolisTransitions.IsDefault)
        {
            foreach (var transition in context.Project.MetropolisTransitions)
            {
                var reference = TransitionRef.From(transition.Id);
                if (!referenced.Contains(reference))
                {
                    bag.Add(new Diagnostic(
                        CompilationDiagnosticCodes.UnusedTransition,
                        DiagnosticSeverity.Warning,
                        $"Metropolis transition '{transition.Name}' is not referenced by any simulation and was not compiled.",
                        new ModelPath("metropolisTransition", transition.Id.Value)));
                }
            }
        }
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
                    CompilationDiagnosticCodes.Precondition,
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
                    CompilationDiagnosticCodes.Precondition,
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
                    CompilationDiagnosticCodes.PathExpansion,
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
        var key = DirectionKey(work.CollectionIndex, relative, IsInverse);
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
                CompilationDiagnosticCodes.Conservation,
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
                    CompilationDiagnosticCodes.Conservation,
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
                            CompilationDiagnosticCodes.Conservation,
                            $"A state-change group references an unknown state change '{stateChangeId:D}'.");
                        return false;
                    }

                    if (!context.Particles!.TryGetIndex(stateChange.DonorParticleId, out var donor) ||
                        !context.Particles.TryGetIndex(stateChange.AcceptorParticleId, out var acceptor))
                    {
                        error = CompilationFailure.FromMessage(
                            context,
                            CompilationDiagnosticCodes.Conservation,
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
        var generated = 0;
        Enumerate(0);

        void Enumerate(int node)
        {
            if (node == choices.Count)
            {
                generated++;
                if (generated > context.Options.MaxRuleCount)
                {
                    failure ??= CompilationFailure.FromMessage(
                        context,
                        CompilationDiagnosticCodes.CompileLimit,
                        $"Rule generation exceeded the hard limit {context.Options.MaxRuleCount}.");
                    return;
                }

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
}
