// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// Read-only access to the authored transition domain of a project snapshot.
/// </summary>
public interface ITransitionQuery
{
    /// <summary>
    /// Gets the authored state changes.
    /// </summary>
    public ImmutableArray<StateChange> StateChanges { get; }

    /// <summary>
    /// Gets the authored state-change groups.
    /// </summary>
    public ImmutableArray<StateChangeGroup> StateChangeGroups { get; }

    /// <summary>
    /// Gets the authored transition chains.
    /// </summary>
    public ImmutableArray<TransitionChain> Chains { get; }

    /// <summary>
    /// Gets the authored kinetic transitions.
    /// </summary>
    public ImmutableArray<KineticTransition> KineticTransitions { get; }

    /// <summary>
    /// Gets the authored Metropolis transitions.
    /// </summary>
    public ImmutableArray<MetropolisTransition> MetropolisTransitions { get; }

    /// <summary>
    /// Finds an authored state change.
    /// </summary>
    /// <param name="id">The state-change identifier.</param>
    /// <returns>The matching state change, or <see langword="null"/> when the identifier is unknown.</returns>
    public StateChange? Find(StateChangeId id);

    /// <summary>
    /// Finds an authored state-change group.
    /// </summary>
    /// <param name="id">The state-change-group identifier.</param>
    /// <returns>The matching group, or <see langword="null"/> when the identifier is unknown.</returns>
    public StateChangeGroup? Find(StateChangeGroupId id);

    /// <summary>
    /// Finds an authored transition chain.
    /// </summary>
    /// <param name="id">The chain identifier.</param>
    /// <returns>The matching chain, or <see langword="null"/> when the identifier is unknown.</returns>
    public TransitionChain? Find(TransitionChainId id);

    /// <summary>
    /// Finds an authored kinetic transition.
    /// </summary>
    /// <param name="id">The kinetic-transition identifier.</param>
    /// <returns>The matching transition, or <see langword="null"/> when the identifier is unknown.</returns>
    public KineticTransition? Find(KineticTransitionId id);

    /// <summary>
    /// Finds an authored Metropolis transition.
    /// </summary>
    /// <param name="id">The Metropolis-transition identifier.</param>
    /// <returns>The matching transition, or <see langword="null"/> when the identifier is unknown.</returns>
    public MetropolisTransition? Find(MetropolisTransitionId id);
}
