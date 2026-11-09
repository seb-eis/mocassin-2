// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Ids;
using Mocassin.Core.Model.Particles;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// Validates the definition-level transition rules that need only the model graph.
/// </summary>
/// <remarks>
/// Resolved-path and reference-site rules belong to <c>Mocassin.Symmetry.Transitions.TransitionPathValidator</c>.
/// The validator never throws for user data.
/// </remarks>
internal static partial class TransitionModelValidator
{
    /// <summary>
    /// Validates state changes, groups, chains, and transitions.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public static ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        var particles = model.Particles.IsDefault ? ImmutableArray<Particle>.Empty : model.Particles;
        var stateChanges = model.StateChanges.IsDefault ? ImmutableArray<StateChange>.Empty : model.StateChanges;
        var groups = model.StateChangeGroups.IsDefault
            ? ImmutableArray<StateChangeGroup>.Empty
            : model.StateChangeGroups;

        var particleById = CreateParticleMap(particles);
        ValidateStateChanges(stateChanges, particleById, bag);
        ValidateStateChangeGroups(stateChanges, groups, particleById, bag);
        ValidateModelTransitions(model, bag);

        return bag.Sorted;
    }

    private static void ValidateStateChanges(
        ImmutableArray<StateChange> stateChanges,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var stateChange in stateChanges)
        {
            ValidateName(stateChange.Name, "stateChange", stateChange.Id.Value, names, bag);

            var donorKnown = particleById.TryGetValue(stateChange.DonorParticleId, out var donor);
            var acceptorKnown = particleById.TryGetValue(stateChange.AcceptorParticleId, out var acceptor);

            if (!donorKnown)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.UnknownParticle,
                    $"State change references unknown donor particle '{stateChange.DonorParticleId:D}'.",
                    "stateChange",
                    stateChange.Id.Value,
                    "donorParticleId"));
            }

            if (!acceptorKnown)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.UnknownParticle,
                    $"State change references unknown acceptor particle '{stateChange.AcceptorParticleId:D}'.",
                    "stateChange",
                    stateChange.Id.Value,
                    "acceptorParticleId"));
            }

            if (stateChange.DonorParticleId == stateChange.AcceptorParticleId)
            {
                if (stateChange.DonorParticleId == ParticleId.Void)
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.BothParticlesVoid,
                        "State change must not use the implicit void particle as both donor and acceptor.",
                        "stateChange",
                        stateChange.Id.Value));
                }
                else
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.DonorEqualsAcceptor,
                        "State change donor and acceptor must differ.",
                        "stateChange",
                        stateChange.Id.Value));
                }
            }

            if (donorKnown &&
                acceptorKnown &&
                stateChange.DonorParticleId == ParticleId.Void &&
                acceptor!.Charge.Value != 0.0)
            {
                bag.Add(Warning(
                    TransitionDiagnosticCodes.ChargedAcceptorFromVoid,
                    $"Void donor is replaced by charged particle '{acceptor.Name}'; this is likely an implicit " +
                    "reverse pair and should be authored explicitly.",
                    "stateChange",
                    stateChange.Id.Value,
                    "acceptorParticleId"));
            }
        }
    }

    private static void ValidateStateChangeGroups(
        ImmutableArray<StateChange> stateChanges,
        ImmutableArray<StateChangeGroup> groups,
        Dictionary<ParticleId, Particle> particleById,
        DiagnosticBag bag)
    {
        var stateChangeById = stateChanges
            .GroupBy(static stateChange => stateChange.Id)
            .ToDictionary(static group => group.Key, static group => group.First());
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            ValidateName(group.Name, "stateChangeGroup", group.Id.Value, names, bag);

            if (group.StateChangeIds.IsDefaultOrEmpty)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.InvalidGroup,
                    "State-change group must contain at least one state change.",
                    "stateChangeGroup",
                    group.Id.Value,
                    "stateChangeIds"));
                continue;
            }

            var resolved = new List<StateChange>();
            foreach (var stateChangeId in group.StateChangeIds)
            {
                if (stateChangeById.TryGetValue(stateChangeId, out var stateChange))
                {
                    resolved.Add(stateChange);
                }
                else
                {
                    bag.Add(Error(
                        TransitionDiagnosticCodes.InvalidGroup,
                        $"State-change group references unknown state change '{stateChangeId:D}'.",
                        "stateChangeGroup",
                        group.Id.Value,
                        "stateChangeIds"));
                }
            }

            if (resolved.Count == 0)
            {
                continue;
            }

            var hasStableRole = false;
            var hasUnstableRole = false;
            var hasPhysicalExchange = false;
            var hasChargeStateExchange = false;

            foreach (var stateChange in resolved)
            {
                var unstableRole = stateChange.AcceptorParticleId == ParticleId.Void;
                hasStableRole |= !unstableRole;
                hasUnstableRole |= unstableRole;

                if (!particleById.TryGetValue(stateChange.DonorParticleId, out var donor) ||
                    !particleById.TryGetValue(stateChange.AcceptorParticleId, out var acceptor) ||
                    stateChange.DonorParticleId == stateChange.AcceptorParticleId)
                {
                    continue;
                }

                if (string.Equals(donor.Symbol, acceptor.Symbol, StringComparison.Ordinal) &&
                    donor.Charge.Value != acceptor.Charge.Value)
                {
                    hasChargeStateExchange = true;
                }
                else
                {
                    hasPhysicalExchange = true;
                }
            }

            if (hasPhysicalExchange && hasChargeStateExchange)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.MixedGroupExchange,
                    "State-change group must not mix physical exchanges with charge-state exchanges.",
                    "stateChangeGroup",
                    group.Id.Value,
                    "stateChangeIds"));
            }

            if (hasStableRole && hasUnstableRole)
            {
                bag.Add(Error(
                    TransitionDiagnosticCodes.MixedGroupStability,
                    "State-change group must not mix stable-role and unstable-role state changes.",
                    "stateChangeGroup",
                    group.Id.Value,
                    "stateChangeIds"));
            }
        }
    }

    private static Dictionary<ParticleId, Particle> CreateParticleMap(ImmutableArray<Particle> particles)
    {
        var map = new Dictionary<ParticleId, Particle>();
        foreach (var particle in particles)
        {
            map[particle.Id] = particle;
        }

        map[Particle.Void.Id] = Particle.Void;
        return map;
    }

    private static void ValidateName(
        string name,
        string domain,
        Guid objectId,
        HashSet<string> names,
        DiagnosticBag bag)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.DuplicateName,
                $"{DomainName(domain)} name must not be empty.",
                domain,
                objectId,
                "name"));
            return;
        }

        if (!names.Add(name))
        {
            bag.Add(Error(
                TransitionDiagnosticCodes.DuplicateName,
                $"{DomainName(domain)} name '{name}' is used more than once.",
                domain,
                objectId,
                "name"));
        }
    }

    private static string DomainName(string domain) => domain switch
    {
        "stateChange" => "State-change",
        "stateChangeGroup" => "State-change-group",
        "chain" => "Chain",
        "kineticTransition" => "Kinetic-transition",
        "metropolisTransition" => "Metropolis-transition",
        _ => domain,
    };

    private static Diagnostic Error(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property), HintFor(code));

    private static Diagnostic Warning(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Warning, message, new ModelPath(domain, objectId, property), HintFor(code));

    private static string? HintFor(string code) => code switch
    {
        TransitionDiagnosticCodes.UnknownParticle => "Use an authored particle ID or the implicit void particle.",
        TransitionDiagnosticCodes.DonorEqualsAcceptor => "Choose a different acceptor particle.",
        TransitionDiagnosticCodes.BothParticlesVoid => "Use a real donor or acceptor; void cannot replace void.",
        TransitionDiagnosticCodes.ChargedAcceptorFromVoid => "Author the reverse pair explicitly if it is intentional.",
        TransitionDiagnosticCodes.InvalidGroup => "Add at least one existing state change to the group.",
        TransitionDiagnosticCodes.MixedGroupExchange => "Split physical and charge-state exchanges into separate groups.",
        TransitionDiagnosticCodes.GroupLinkerCountMismatch => "Provide exactly one linker per adjacent position pair.",
        TransitionDiagnosticCodes.InvalidPatternPositionCount => "Match the position count required by the pattern.",
        TransitionDiagnosticCodes.InvalidLinkerSequence => "Use the pattern's D/S sequence; custom chains reject D S D.",
        TransitionDiagnosticCodes.DuplicateName => "Use a unique non-empty name within the collection.",
        TransitionDiagnosticCodes.MissingReverseLink => "Add a symmetric reverseTransitionId for the complex transition.",
        TransitionDiagnosticCodes.TransitionLimitExceeded => "Keep at most 100 kinetic and Metropolis transitions in total.",
        TransitionDiagnosticCodes.UnknownChainGroup => "Reference an existing state-change group.",
        TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition => "Add at least one unstable position to the kinetic chain.",
        TransitionDiagnosticCodes.KineticTransitionUsesMetropolisChain => "Use a kinetic chain such as migration3.",
        TransitionDiagnosticCodes.GeneratesReverseMismatch => "Set generatesReverse true only for metropolis2 and migration3.",
        TransitionDiagnosticCodes.MixedGroupStability => "Split stable-role and unstable-role pairs into separate groups.",
        TransitionDiagnosticCodes.InvalidUnstablePositionGroup => "Use exactly one [donor, void] pair at an unstable position.",
        TransitionDiagnosticCodes.InvalidReverseLink => "Use a symmetric, existing, same-kind reverse transition.",
        _ => null,
    };
}
