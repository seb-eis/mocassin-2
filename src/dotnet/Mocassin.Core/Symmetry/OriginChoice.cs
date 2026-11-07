// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Symmetry;

/// <summary>
/// Identifies the origin choice of a space-group reference.
/// </summary>
/// <remarks>
/// The member names map to the project wire strings <c>none</c>, <c>originChoice1</c>, and
/// <c>originChoice2</c> under a camelCase enum serializer.
/// </remarks>
public enum OriginChoice
{
    /// <summary>
    /// The group has a single standard origin.
    /// </summary>
    None = 0,

    /// <summary>
    /// Origin choice 1.
    /// </summary>
    OriginChoice1 = 1,

    /// <summary>
    /// Origin choice 2.
    /// </summary>
    OriginChoice2 = 2,
}
