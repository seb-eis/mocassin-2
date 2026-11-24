// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// The kind of width a model-table column declares.
/// </summary>
internal enum ModelColumnWidthKind
{
    /// <summary>
    /// A fixed pixel width.
    /// </summary>
    Fixed,

    /// <summary>
    /// A proportional share of the remaining width.
    /// </summary>
    Star,
}

/// <summary>
/// A framework-neutral column width. The view maps it to a grid length; the view models never reference a control
/// type.
/// </summary>
/// <param name="Kind">Whether the width is fixed or proportional.</param>
/// <param name="Value">The pixel count for a fixed width, or the star weight for a proportional one.</param>
internal readonly record struct ModelColumnWidth(ModelColumnWidthKind Kind, double Value)
{
    /// <summary>
    /// Creates a fixed-pixel width.
    /// </summary>
    /// <param name="pixels">The pixel width; must be finite and positive.</param>
    /// <returns>The fixed width.</returns>
    public static ModelColumnWidth Fixed(double pixels) => new(ModelColumnWidthKind.Fixed, Validate(pixels));

    /// <summary>
    /// Creates a proportional width.
    /// </summary>
    /// <param name="weight">The star weight; must be finite and positive. Defaults to one.</param>
    /// <returns>The proportional width.</returns>
    public static ModelColumnWidth Star(double weight = 1d) => new(ModelColumnWidthKind.Star, Validate(weight));

    /// <summary>
    /// Gets a value indicating whether this is a proportional width.
    /// </summary>
    public bool IsStar => Kind == ModelColumnWidthKind.Star;

    /// <summary>
    /// Gets the pixel width for a fixed column, or zero.
    /// </summary>
    public double Pixels => Kind == ModelColumnWidthKind.Fixed ? Value : 0d;

    /// <summary>
    /// Gets the star weight for a proportional column, or zero.
    /// </summary>
    public double Weight => Kind == ModelColumnWidthKind.Star ? Value : 0d;

    private static double Validate(double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A column width must be finite and positive.");
        }

        return value;
    }
}
