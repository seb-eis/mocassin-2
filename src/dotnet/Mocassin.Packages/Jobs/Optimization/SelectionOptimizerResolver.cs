// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Compilation.Compiler;
using Mocassin.Compilation.Simulations;
using Mocassin.Compilation.Structure;
using Mocassin.Compilation.Transitions;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Jobs;
using Mocassin.Core.Model.Simulations;
using Mocassin.Core.Model.Structure;

namespace Mocassin.Packages.Jobs.Optimization;

/// <summary>
/// Maps authored selection optimizers to the compiled P1 start options and resolves the dual-DOF flag.
/// </summary>
/// <remarks>
/// Structural reference errors are already reported by the Core job-template validator. The resolver still
/// returns <c>JOB009</c> diagnostics defensively for project data that no longer matches the compiled model,
/// and throws <see cref="InvalidOperationException"/> only for a compiled-model contract inconsistency. The
/// compiled model is never mutated.
/// </remarks>
public static class SelectionOptimizerResolver
{
    /// <summary>
    /// The limitation recorded when the compiled jump table cannot evaluate start options.
    /// </summary>
    public const string MissingJumpTableLimitation =
        "The compiled transition model has no jump-count table sized for its positions and particles, so the " +
        "removes-all JOB009 start-option check was not evaluated.";

    /// <summary>
    /// The limitation recorded when the compiled simulation has no active collection.
    /// </summary>
    public const string NoActiveCollectionLimitation =
        "The compiled simulation has no active transition collection, so the removes-all JOB009 start-option " +
        "check was not evaluated.";

    /// <summary>
    /// Resolves one collection's authored optimizers against a compiled simulation.
    /// </summary>
    /// <param name="model">The compiled model that supplies sites, positions, particles, and transitions.</param>
    /// <param name="simulation">The compiled simulation that owns the collection.</param>
    /// <param name="collectionId">The authored collection identifier used for diagnostic paths.</param>
    /// <param name="optimizers">The authored optimizers; a default array means none.</param>
    /// <param name="diagnostics">The defensive diagnostics, including the compiled-bound <c>JOB009</c> result.</param>
    /// <returns>The immutable compiled plan.</returns>
    /// <exception cref="ArgumentNullException">A reference argument is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compiled model is internally inconsistent.</exception>
    public static SelectionOptimizerPlan Resolve(
        CompiledModel model,
        CompiledSimulation simulation,
        JobCollectionId collectionId,
        ImmutableArray<SelectionOptimizer> optimizers,
        out ImmutableArray<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(simulation);

        var authored = optimizers.IsDefault ? ImmutableArray<SelectionOptimizer>.Empty : optimizers;
        var startDual = simulation.Kind == SimulationKind.Kmc && simulation.Intent.UseDualDofCorrection;
        if (simulation.Kind != SimulationKind.Kmc)
        {
            diagnostics = [];
            return new SelectionOptimizerPlan([], authored.Length, false, [], true, null);
        }

        if (authored.Length == 0)
        {
            diagnostics = [];
            return SelectionOptimizerPlan.WithoutOptimizers(startDual);
        }

        var bag = new DiagnosticBag();
        var path = new ModelPath("jobCollection", collectionId.Value, "selectionOptimizers");
        var sites = IndexSites(model.Structure.Sites);
        var particles = model.Particles;
        var positions = model.Structure.Positions;
        var targets = new HashSet<CompiledSelectionOptimizer>();
        var effects = ImmutableArray.CreateBuilder<SelectionOptimizerEffect>(authored.Length);
        foreach (var optimizer in authored)
        {
            if (optimizer is null)
            {
                continue;
            }

            if (!sites.TryGetValue(optimizer.SiteId, out var site))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unknown compiled site '{optimizer.SiteId.Value:D}'.",
                    path));
                continue;
            }

            if (site.Stability != SiteStability.Stable)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unstable compiled site '{site.Name}'.",
                    path));
                continue;
            }

            if (!particles.TryGetIndex(optimizer.ParticleId, out var particleIndex))
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Optimizer references unknown compiled particle '{optimizer.ParticleId.Value:D}'.",
                    path));
                continue;
            }

            var sitePositions = site.PositionIndices.IsDefault
                ? ImmutableArray<int>.Empty
                : site.PositionIndices;
            if (sitePositions.Length == 0)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Compiled site '{site.Name}' owns no canonical P1 position.",
                    path));
                continue;
            }

            var effectTargets = ImmutableArray.CreateBuilder<CompiledSelectionOptimizer>(sitePositions.Length);
            foreach (var positionId in sitePositions)
            {
                if (positionId < 0 || positionId >= positions.Length)
                {
                    throw new InvalidOperationException(
                        $"Compiled site '{site.Name}' references P1 position {positionId}, but the compiled " +
                        $"structure has {positions.Length} positions.");
                }

                var target = new CompiledSelectionOptimizer(positionId, particleIndex);
                effectTargets.Add(target);
                targets.Add(target);
            }

            effects.Add(new SelectionOptimizerEffect(
                optimizer,
                Sort(effectTargets.ToImmutable()),
                RemovedStartOptions: null));
        }

        var sortedTargets = Sort([.. targets]);
        var useDual = startDual && !AllEnvironmentsReduced(model.Structure, sortedTargets);
        var (evaluated, limitation) = EvaluateStartOptions(
            model.Transitions,
            simulation,
            sortedTargets,
            effects,
            bag,
            path);

        diagnostics = bag.Sorted;
        return new SelectionOptimizerPlan(
            sortedTargets,
            authored.Length,
            useDual,
            effects.ToImmutable(),
            evaluated,
            limitation);
    }

    private static (bool Evaluated, string? Limitation) EvaluateStartOptions(
        CompiledTransitionModel transitions,
        CompiledSimulation simulation,
        ImmutableArray<CompiledSelectionOptimizer> targets,
        ImmutableArray<SelectionOptimizerEffect>.Builder effects,
        DiagnosticBag bag,
        ModelPath path)
    {
        var particleCount = transitions.ParticleCount;
        var positionCount = transitions.PositionCount;
        var jumpCounts = transitions.JumpCounts;
        if (particleCount <= 0 ||
            positionCount <= 0 ||
            jumpCounts.IsDefaultOrEmpty ||
            jumpCounts.Length != positionCount * particleCount)
        {
            return (false, MissingJumpTableLimitation);
        }

        var active = simulation.ActiveCollectionIndices.IsDefault
            ? ImmutableArray<int>.Empty
            : simulation.ActiveCollectionIndices;
        if (active.Length == 0)
        {
            return (false, NoActiveCollectionLimitation);
        }

        var collections = transitions.Collections;
        var activeKmc = new List<CompiledJumpCollection>(active.Length);
        foreach (var index in active.Distinct().Order())
        {
            if (index < 0 || index >= collections.Length)
            {
                throw new InvalidOperationException(
                    $"Compiled simulation references collection index {index}, but the compiled transition " +
                    $"model has {collections.Length} collections.");
            }

            var collection = collections[index];
            if (collection.Kind == SimulationKind.Kmc)
            {
                activeKmc.Add(collection);
            }
        }

        var targetSet = targets.ToHashSet();
        for (var index = 0; index < effects.Count; index++)
        {
            var effect = effects[index];
            var removed = 0;
            foreach (var target in effect.Targets)
            {
                if (IsSelectableStart(transitions, activeKmc, target))
                {
                    removed++;
                }
            }

            effects[index] = effect with { RemovedStartOptions = removed };
        }

        foreach (var collection in activeKmc)
        {
            var removedAny = false;
            var remaining = false;
            for (var position = 0; position < positionCount; position++)
            {
                for (var particle = 1; particle < particleCount; particle++)
                {
                    if ((collection.MobileParticleMask & (1UL << particle)) == 0)
                    {
                        continue;
                    }

                    var count = jumpCounts[(position * particleCount) + particle];
                    if (count <= 0)
                    {
                        continue;
                    }

                    var target = new CompiledSelectionOptimizer(position, particle);
                    if (targetSet.Contains(target))
                    {
                        removedAny = true;
                    }
                    else
                    {
                        remaining = true;
                        break;
                    }
                }

                if (remaining)
                {
                    break;
                }
            }

            if (removedAny && !remaining)
            {
                bag.Add(Error(
                    JobDiagnosticCodes.InvalidOptimizer,
                    $"Selection optimizers remove every selectable start option of compiled transition collection " +
                    $"'{collection.Transition.Id:D}'.",
                    path));
            }
        }

        return (true, null);
    }

    private static bool IsSelectableStart(
        CompiledTransitionModel transitions,
        List<CompiledJumpCollection> activeKmc,
        CompiledSelectionOptimizer target)
    {
        if (target.PositionId < 0 ||
            target.PositionId >= transitions.PositionCount ||
            target.ParticleId <= 0 ||
            target.ParticleId >= transitions.ParticleCount)
        {
            return false;
        }

        var particleMask = 1UL << target.ParticleId;
        foreach (var collection in activeKmc)
        {
            if ((collection.MobileParticleMask & particleMask) == 0)
            {
                continue;
            }

            if (transitions.JumpCounts[(target.PositionId * transitions.ParticleCount) + target.ParticleId] > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool AllEnvironmentsReduced(
        CompiledStructureModel structure,
        ImmutableArray<CompiledSelectionOptimizer> targets)
    {
        var environments = structure.Environments;
        if (environments.IsDefaultOrEmpty || targets.Length == 0)
        {
            return false;
        }

        var positions = structure.Positions;
        foreach (var environment in environments)
        {
            var reduced = false;
            foreach (var target in targets)
            {
                var position = positions[target.PositionId];
                if (position.EnvironmentIndex != environment.Index)
                {
                    continue;
                }

                if (target.ParticleId is < 0 or >= 64)
                {
                    continue;
                }

                if ((position.AllowedParticleMask & (1UL << target.ParticleId)) != 0)
                {
                    reduced = true;
                    break;
                }
            }

            if (!reduced)
            {
                return false;
            }
        }

        return true;
    }

    private static ImmutableArray<CompiledSelectionOptimizer> Sort(
        ImmutableArray<CompiledSelectionOptimizer> targets) =>
        targets
            .OrderBy(static target => target.PositionId)
            .ThenBy(static target => target.ParticleId)
            .ToImmutableArray();

    private static Dictionary<CellSiteId, CompiledSite> IndexSites(ImmutableArray<CompiledSite> sites)
    {
        var lookup = new Dictionary<CellSiteId, CompiledSite>(sites.IsDefault ? 0 : sites.Length);
        if (sites.IsDefault)
        {
            return lookup;
        }

        foreach (var site in sites)
        {
            if (site is not null)
            {
                lookup.TryAdd(site.Id, site);
            }
        }

        return lookup;
    }

    private static Diagnostic Error(string code, string message, ModelPath path) =>
        new(code, DiagnosticSeverity.Error, message, path, JobDiagnosticCodes.HelpFor(code));
}
