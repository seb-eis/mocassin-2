// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// Indicates whether a cell site is a stable ground-state site or an unstable transition state.
/// </summary>
/// <remarks>
/// The persisted project JSON writes these values as the camel-case strings <c>stable</c> and
/// <c>unstable</c> once the structure section is serialized.
/// </remarks>
public enum SiteStability
{
    /// <summary>
    /// The site is a stable ground-state site and may host particles.
    /// </summary>
    Stable = 0,

    /// <summary>
    /// The site is an unstable transition state.
    /// </summary>
    Unstable = 1,
}
