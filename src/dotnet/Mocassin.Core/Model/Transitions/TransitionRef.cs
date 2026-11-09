// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using Mocassin.Core.Ids;

namespace Mocassin.Core.Model.Transitions;

/// <summary>
/// A common in-memory reference to a kinetic or Metropolis transition.
/// </summary>
/// <param name="Kind">The transition kind.</param>
/// <param name="Id">The underlying transition identifier.</param>
/// <remarks>
/// The reference is never persisted on its own inside the project; simulations keep the kind-specific
/// <c>kineticTransitionIds</c> and <c>metropolisTransitionIds</c> arrays. A reference never points across kinds.
/// </remarks>
[JsonConverter(typeof(TransitionRefJsonConverter))]
public readonly record struct TransitionRef(TransitionKind Kind, Guid Id)
{
    /// <summary>
    /// Creates a reference to a kinetic transition.
    /// </summary>
    /// <param name="id">The kinetic-transition identifier.</param>
    /// <returns>The typed reference.</returns>
    public static TransitionRef From(KineticTransitionId id) => new(TransitionKind.Kinetic, id.Value);

    /// <summary>
    /// Creates a reference to a Metropolis transition.
    /// </summary>
    /// <param name="id">The Metropolis-transition identifier.</param>
    /// <returns>The typed reference.</returns>
    public static TransitionRef From(MetropolisTransitionId id) => new(TransitionKind.Metropolis, id.Value);

    /// <summary>
    /// Gets a value indicating whether the reference points to a kinetic transition.
    /// </summary>
    public bool IsKinetic => Kind == TransitionKind.Kinetic;
}
