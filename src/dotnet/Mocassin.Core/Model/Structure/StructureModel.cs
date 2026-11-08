// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mocassin.Core.Symmetry;
using Mocassin.Core.Units;

namespace Mocassin.Core.Model.Structure;

/// <summary>
/// The authored crystal structure of the model.
/// </summary>
/// <param name="Name">The user-visible structure name.</param>
/// <param name="SpaceGroup">The persisted space-group reference.</param>
/// <param name="Cell">The six cell parameters.</param>
/// <param name="Sites">The reference sites.</param>
/// <param name="DummyPositions">The visual-only dummy positions.</param>
/// <remarks>
/// This is the Core model only. Resolved catalog settings, symmetry-expanded positions, and previews belong to
/// Symmetry services and are never stored on the structure value.
/// </remarks>
[JsonConverter(typeof(StructureModelJsonConverter))]
public sealed record StructureModel(
    string Name,
    SpaceGroupRef SpaceGroup,
    CellParameters Cell,
    ImmutableArray<CellSite> Sites,
    ImmutableArray<CellDummyPosition> DummyPositions)
{
    /// <summary>
    /// The default structure name for a new project.
    /// </summary>
    public const string DefaultName = "New Structure";

    /// <summary>
    /// The maximum number of reference sites.
    /// </summary>
    public const int MaxReferenceSites = 1000;

    private static readonly SpaceGroupRef defaultSpaceGroup = new(1, AxisSetting.None, OriginChoice.None);

    private static readonly CellParameters defaultCell = new(
        new LengthAngstrom(1.0),
        new LengthAngstrom(1.0),
        new LengthAngstrom(1.0),
        new AngleDegrees(90.0),
        new AngleDegrees(90.0),
        new AngleDegrees(90.0));

    /// <summary>
    /// Gets the default structure: space group P1, a unit cell with <c>a = b = c = 1 Å</c> and <c>90°</c>
    /// angles, no sites, and no dummy positions.
    /// </summary>
    public static StructureModel Default { get; } = new(
        DefaultName,
        defaultSpaceGroup,
        defaultCell,
        [],
        []);

    /// <summary>
    /// Initializes the default structure for an empty or missing <c>structure</c> section.
    /// </summary>
    [JsonConstructor]
    public StructureModel()
        : this(DefaultName, defaultSpaceGroup, defaultCell, [], [])
    {
    }
}
