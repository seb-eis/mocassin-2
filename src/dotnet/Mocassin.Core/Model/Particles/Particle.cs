// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using Mocassin.Core.Ids;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Particles;

/// <summary>
/// An authored particle species that can occupy a crystal site.
/// </summary>
/// <param name="Id">The stable particle identifier.</param>
/// <param name="Name">The user-visible particle name.</param>
/// <param name="Symbol">The short mass-conservation symbol.</param>
/// <param name="Charge">The charge in units of the elementary charge.</param>
/// <param name="IsVacancy">Whether the particle represents an empty state.</param>
/// <remarks>
/// The implicit void particle is not stored in the model. It is exposed as <see cref="Void"/> and identified by
/// the reserved <see cref="ParticleId.Void"/> value. Authored particles must not reuse that identifier.
/// </remarks>
public sealed record Particle(
    ParticleId Id,
    string Name,
    string Symbol,
    ChargeElementary Charge,
    bool IsVacancy)
{
    /// <summary>
    /// The recommended regular-expression pattern for a particle symbol.
    /// </summary>
    /// <remarks>
    /// A symbol that does not match this pattern emits <c>PAR008</c> warning, not an error, so existing
    /// scientific labels remain usable.
    /// </remarks>
    public const string RecommendedSymbolPattern = "^[A-Z][A-Za-z0-9+\\-()]{0,10}$";

    /// <summary>
    /// Gets the implicit void particle.
    /// </summary>
    /// <remarks>
    /// The void particle is reserved, has charge zero, and is never persisted as an authored particle.
    /// </remarks>
    public static Particle Void { get; } = new(
        ParticleId.Void,
        "Void",
        "∅",
        new ChargeElementary(0.0),
        IsVacancy: false);

    /// <summary>
    /// Gets a value indicating whether this is the implicit void particle.
    /// </summary>
    /// <remarks>
    /// This value is computed from <see cref="Id"/> and is not part of the project JSON representation.
    /// </remarks>
    [JsonIgnore]
    public bool IsVoid => Id == ParticleId.Void;
}
