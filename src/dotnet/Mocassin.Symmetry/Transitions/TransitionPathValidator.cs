// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;
using Mocassin.Core.Geometry;
using Mocassin.Core.Ids;
using Mocassin.Core.Model;
using Mocassin.Core.Model.Structure;
using Mocassin.Core.Model.Transitions;
using Mocassin.Symmetry.SpaceGroups;

namespace Mocassin.Symmetry.Transitions;

/// <summary>
/// Validates the resolved-path and reference-site rules of the transition model.
/// </summary>
/// <remarks>
/// This validator covers <c>TRN010</c>–<c>TRN016</c>, <c>TRN020</c>, and <c>TRN028</c>. Definition-level
/// rules are emitted by <c>Mocassin.Core.Model.Transitions.TransitionModelValidator</c>. It never throws for
/// invalid user data.
/// </remarks>
public sealed class TransitionPathValidator
{
    private readonly SpaceGroupCatalog catalog;

    /// <summary>
    /// Initializes a transition-path validator.
    /// </summary>
    /// <param name="catalog">The optional catalog override.</param>
    public TransitionPathValidator(SpaceGroupCatalog? catalog = null)
    {
        this.catalog = catalog ?? SpaceGroupCatalog.Default;
    }

    /// <summary>
    /// Validates kinetic reference paths and Metropolis site references.
    /// </summary>
    /// <param name="model">The model to validate.</param>
    /// <returns>The diagnostics in stable order.</returns>
    public ImmutableArray<Diagnostic> Validate(ModelDefinition model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var bag = new DiagnosticBag();
        if (!TransitionPathResolver.TryCreate(model.Structure, catalog, out var resolver, out _) || resolver is null)
        {
            return bag.Sorted;
        }

        var sites = model.Structure.Sites.IsDefault ? ImmutableArray<CellSite>.Empty : model.Structure.Sites;
        var groups = model.StateChangeGroups.IsDefault
            ? ImmutableArray<StateChangeGroup>.Empty
            : model.StateChangeGroups;
        var stateChanges = model.StateChanges.IsDefault ? ImmutableArray<StateChange>.Empty : model.StateChanges;
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
        var siteById = sites
            .GroupBy(static site => site.Id)
            .ToDictionary(static group => group.Key, static group => group.First());

        foreach (var transition in kineticTransitions)
        {
            ValidateKineticTransition(
                transition,
                resolver,
                chainById,
                groupById,
                stateChangeById,
                bag);
        }

        foreach (var transition in metropolisTransitions)
        {
            ValidateMetropolisTransition(transition, chainById, siteById, bag);
        }

        return bag.Sorted;
    }

    private static void ValidateKineticTransition(
        KineticTransition transition,
        TransitionPathResolver resolver,
        Dictionary<TransitionChainId, TransitionChain> chainById,
        Dictionary<StateChangeGroupId, StateChangeGroup> groupById,
        Dictionary<StateChangeId, StateChange> stateChangeById,
        DiagnosticBag bag)
    {
        if (!chainById.TryGetValue(transition.ChainId, out var chain))
        {
            return;
        }

        var path = transition.ReferencePath.IsDefault
            ? ImmutableArray<Fractional3>.Empty
            : transition.ReferencePath;
        var groupIds = chain.GroupIds.IsDefault ? ImmutableArray<StateChangeGroupId>.Empty : chain.GroupIds;
        var chainPositionCount = groupIds.Length;
        if (path.Length != chainPositionCount)
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.PathLengthMismatch,
                $"Kinetic path has {path.Length} points but its chain has {chainPositionCount} positions.",
                "kineticTransition",
                transition.Id.Value,
                "referencePath"));
        }

        if (path.Length == 0)
        {
            return;
        }

        var resolved = resolver.TryResolve(transition, out var pathDiagnostics);
        bag.AddRange(pathDiagnostics);
        if (resolved is null)
        {
            return;
        }

        var positions = resolved.Positions;
        var endpointIndices = positions.Length == 1 ? new[] { 0 } : [0, positions.Length - 1];
        foreach (var index in endpointIndices)
        {
            if (positions[index].Stability != SiteStability.Stable)
            {
                bag.Add(TransitionError(
                    TransitionDiagnosticCodes.UnstablePathEndpoint,
                    $"Kinetic path endpoint {index} is not stable.",
                    "kineticTransition",
                    transition.Id.Value,
                    "referencePath"));
            }
        }

        for (var index = 1; index < positions.Length; index++)
        {
            if (positions[index - 1].Stability == SiteStability.Unstable &&
                positions[index].Stability == SiteStability.Unstable)
            {
                bag.Add(TransitionError(
                    TransitionDiagnosticCodes.ConsecutiveUnstablePositions,
                    $"Kinetic path positions {index - 1} and {index} are both unstable.",
                    "kineticTransition",
                    transition.Id.Value,
                    "referencePath"));
            }
        }

        if (chain.Pattern == TransitionPattern.Custom &&
            !positions.Any(static position => position.Stability == SiteStability.Unstable))
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.KineticChainWithoutUnstablePosition,
                "Kinetic custom chain has no unstable resolved path position.",
                "kineticTransition",
                transition.Id.Value,
                "referencePath"));
        }

        for (var index = 0; index < Math.Min(positions.Length, groupIds.Length); index++)
        {
            if (!groupById.TryGetValue(groupIds[index], out var group))
            {
                continue;
            }

            var groupRole = GetGroupStabilityRole(group, stateChangeById);
            if (groupRole is { } unstableRole &&
                unstableRole != (positions[index].Stability == SiteStability.Unstable))
            {
                bag.Add(TransitionError(
                    TransitionDiagnosticCodes.GroupStabilityMismatch,
                    $"Kinetic path position {index} has site stability '{positions[index].Stability}' but its group " +
                    $"is {(unstableRole ? "unstable" : "stable")}-role.",
                    "kineticTransition",
                    transition.Id.Value,
                    "referencePath"));
            }

            if (chain.Pattern == TransitionPattern.Custom &&
                positions[index].Stability == SiteStability.Unstable &&
                CountVoidAcceptorChanges(group, stateChangeById) != 1)
            {
                bag.Add(TransitionError(
                    TransitionDiagnosticCodes.InvalidUnstablePositionGroup,
                    $"Kinetic path position {index} is unstable but its group does not contain exactly one [donor, void] state change.",
                    "kineticTransition",
                    transition.Id.Value,
                    "referencePath"));
            }
        }

        var expectedSequence = GetBuiltInStabilitySequence(chain.Pattern);
        if (expectedSequence is { } expected && positions.Length == expected.Length)
        {
            for (var index = 0; index < expected.Length; index++)
            {
                if (positions[index].Stability != expected[index])
                {
                    bag.Add(TransitionError(
                        TransitionDiagnosticCodes.PathStabilityMismatch,
                        $"Kinetic path stability sequence does not match pattern '{chain.Pattern}' at position {index}.",
                        "kineticTransition",
                        transition.Id.Value,
                        "referencePath"));
                    break;
                }
            }
        }
    }

    private static void ValidateMetropolisTransition(
        MetropolisTransition transition,
        Dictionary<TransitionChainId, TransitionChain> chainById,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (!chainById.TryGetValue(transition.ChainId, out var chain))
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.InvalidMetropolisChain,
                $"Metropolis transition references unknown chain '{transition.ChainId:D}'.",
                "metropolisTransition",
                transition.Id.Value,
                "chainId"));
        }
        else if (chain.Pattern != TransitionPattern.Metropolis2 &&
                 !(chain.Pattern == TransitionPattern.Custom && !chain.GroupIds.IsDefault && chain.GroupIds.Length == 2))
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.InvalidMetropolisChain,
                "Metropolis transition must use a Metropolis2 or two-position custom chain.",
                "metropolisTransition",
                transition.Id.Value,
                "chainId"));
        }

        ValidateMetropolisSite(transition, transition.SiteAId, "siteAId", siteById, bag);
        ValidateMetropolisSite(transition, transition.SiteBId, "siteBId", siteById, bag);
    }

    private static void ValidateMetropolisSite(
        MetropolisTransition transition,
        CellSiteId siteId,
        string property,
        Dictionary<CellSiteId, CellSite> siteById,
        DiagnosticBag bag)
    {
        if (!siteById.TryGetValue(siteId, out var site))
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.InvalidMetropolisSite,
                $"Metropolis transition references unknown site '{siteId:D}'.",
                "metropolisTransition",
                transition.Id.Value,
                property));
        }
        else if (site.Stability != SiteStability.Stable)
        {
            bag.Add(TransitionError(
                TransitionDiagnosticCodes.InvalidMetropolisSite,
                $"Metropolis transition site '{site.Name}' is not stable.",
                "metropolisTransition",
                transition.Id.Value,
                property));
        }
    }

    private static bool? GetGroupStabilityRole(
        StateChangeGroup group,
        Dictionary<StateChangeId, StateChange> stateChangeById)
    {
        var hasStableRole = false;
        var hasUnstableRole = false;
        foreach (var stateChangeId in group.StateChangeIds.IsDefault ? [] : group.StateChangeIds)
        {
            if (!stateChangeById.TryGetValue(stateChangeId, out var stateChange))
            {
                continue;
            }

            if (stateChange.AcceptorParticleId == ParticleId.Void)
            {
                hasUnstableRole = true;
            }
            else
            {
                hasStableRole = true;
            }
        }

        return hasStableRole && hasUnstableRole ? null : hasUnstableRole;
    }

    private static int CountVoidAcceptorChanges(
        StateChangeGroup group,
        Dictionary<StateChangeId, StateChange> stateChangeById)
    {
        var count = 0;
        foreach (var stateChangeId in group.StateChangeIds.IsDefault ? [] : group.StateChangeIds)
        {
            if (stateChangeById.TryGetValue(stateChangeId, out var stateChange) &&
                stateChange.AcceptorParticleId == ParticleId.Void)
            {
                count++;
            }
        }

        return count;
    }

    private static ImmutableArray<SiteStability>? GetBuiltInStabilitySequence(TransitionPattern pattern) => pattern switch
    {
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
        TransitionPattern.Metropolis2 => null,
        _ => null,
    };

    private static Diagnostic TransitionError(
        string code,
        string message,
        string domain,
        Guid objectId,
        string? property = null) =>
        new(code, DiagnosticSeverity.Error, message, new ModelPath(domain, objectId, property));
}
