// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// A donor/acceptor particle exchange at one position of a transition chain.
/// </summary>
/// <param name="Id">The stable state-change identifier.</param>
/// <param name="Name">The user-visible state-change name.</param>
/// <param name="DonorParticleId">The particle replaced at the position.</param>
/// <param name="AcceptorParticleId">The particle that replaces the donor.</param>
/// <remarks>
/// The pair <c>[D, A]</c> means donor <c>D</c> is replaced by acceptor <c>A</c>. The implicit void particle is a
/// valid acceptor, and a void donor is allowed only for an explicit reverse vacancy pair.
/// </remarks>
public sealed record StateChange(
    StateChangeId Id,
    string Name,
    ParticleId DonorParticleId,
    ParticleId AcceptorParticleId);
