// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using Mocassin.Core.Model;

namespace Mocassin.Core.Symmetry;

/// <summary>
/// Identifies the axis setting of a space-group reference.
/// </summary>
/// <remarks>
/// The member names map to the project wire strings <c>none</c>, <c>uniqueAxisA</c>, <c>uniqueAxisB</c>,
/// <c>uniqueAxisC</c>, <c>hexagonal</c>, and <c>rhombohedral</c> under a camelCase enum serializer.
/// <see cref="UniqueAxisA"/> is reserved for a future catalog extension and is rejected by the v1 project
/// validator.
/// </remarks>
[JsonConverter(typeof(CamelCaseEnumJsonConverter<AxisSetting>))]
public enum AxisSetting
{
    /// <summary>
    /// No unique-axis or alternative-axis setting applies.
    /// </summary>
    None = 0,

    /// <summary>
    /// Reserved unique axis A. Not populated by the v1 compatibility catalog.
    /// </summary>
    UniqueAxisA = 1,

    /// <summary>
    /// Monoclinic unique axis B.
    /// </summary>
    UniqueAxisB = 2,

    /// <summary>
    /// Monoclinic unique axis C.
    /// </summary>
    UniqueAxisC = 3,

    /// <summary>
    /// Trigonal or hexagonal description with hexagonal axes.
    /// </summary>
    Hexagonal = 4,

    /// <summary>
    /// Trigonal description with rhombohedral axes.
    /// </summary>
    Rhombohedral = 5,
}
