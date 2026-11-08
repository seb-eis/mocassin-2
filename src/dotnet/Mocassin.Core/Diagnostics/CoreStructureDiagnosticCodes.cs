// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes emitted by Core-side structure validation.
/// </summary>
/// <remarks>
/// Catalog-dependent codes such as <c>STR001</c>, <c>STR002</c>, <c>STR003</c>, <c>STR007</c>,
/// <c>STR009</c>, and <c>STR014</c> belong to <c>Mocassin.Symmetry</c>.
/// </remarks>
public static class CoreStructureDiagnosticCodes
{
    /// <summary>
    /// A reference site lies outside <c>[0,1)</c> beyond the fractional trim tolerance, or is not finite.
    /// </summary>
    public const string InvalidSitePosition = "STR004";

    /// <summary>
    /// A site name is empty or used more than once (case-insensitive).
    /// </summary>
    public const string InvalidSiteName = "STR005";

    /// <summary>
    /// A site has no particle set, or uses the reserved empty particle-set identifier.
    /// </summary>
    public const string MissingParticleSet = "STR006";

    /// <summary>
    /// A dummy position is outside <c>[0,1)</c> beyond the fractional trim tolerance, or is not finite.
    /// </summary>
    public const string InvalidDummyPosition = "STR010";

    /// <summary>
    /// The structure defines more than the maximum number of reference sites.
    /// </summary>
    public const string SiteLimitExceeded = "STR012";

    /// <summary>
    /// The structure name is empty or whitespace-only.
    /// </summary>
    public const string InvalidStructureName = "STR013";

    /// <summary>
    /// A dummy-position name is empty or used more than once (case-insensitive).
    /// </summary>
    public const string InvalidDummyName = "STR015";
}
