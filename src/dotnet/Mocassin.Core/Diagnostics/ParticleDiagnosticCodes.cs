// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by particle and particle-set validation.
/// </summary>
public static class ParticleDiagnosticCodes
{
    /// <summary>
    /// A particle name is empty or used more than once (case-insensitive).
    /// </summary>
    public const string InvalidName = "PAR001";

    /// <summary>
    /// The model defines more than the maximum number of authored particles.
    /// </summary>
    public const string ParticleLimitExceeded = "PAR002";

    /// <summary>
    /// A particle set is empty or contains the implicit void particle.
    /// </summary>
    public const string InvalidSetMembers = "PAR005";

    /// <summary>
    /// A particle set references a particle that does not exist.
    /// </summary>
    public const string UnknownParticleReference = "PAR006";

    /// <summary>
    /// The model defines more than the maximum number of authored particle sets.
    /// </summary>
    public const string SetLimitExceeded = "PAR007";

    /// <summary>
    /// A particle symbol is empty, too long, or does not match the recommended pattern.
    /// </summary>
    public const string NonRecommendedSymbol = "PAR008";

    /// <summary>
    /// Two authored particle sets have the same ordered particle list.
    /// </summary>
    public const string DuplicateSetContent = "PAR009";

    /// <summary>
    /// An authored object reuses a reserved implicit-object identifier.
    /// </summary>
    public const string ReservedIdentifier = "PAR010";

    /// <summary>
    /// A particle-set name is empty or used more than once (case-insensitive).
    /// </summary>
    public const string InvalidSetName = "PAR011";

    /// <summary>
    /// A particle set contains the same particle identifier more than once.
    /// </summary>
    public const string DuplicateSetMember = "PAR012";
}
