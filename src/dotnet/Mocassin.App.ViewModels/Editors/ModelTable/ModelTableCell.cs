// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// One formatted table cell and its typed sort key.
/// </summary>
/// <param name="Text">The displayed text.</param>
/// <param name="SortKey">
/// The typed key the column's <see cref="ModelSortKind"/> compares; when omitted, <see cref="Text"/> is used.
/// </param>
internal readonly record struct ModelTableCell(string Text, object? SortKey = null)
{
    /// <summary>
    /// Creates a text cell.
    /// </summary>
    /// <param name="text">The displayed text.</param>
    /// <returns>The cell.</returns>
    public static ModelTableCell OfText(string text) => new(text ?? string.Empty, text);

    /// <summary>
    /// Creates a numeric cell whose displayed text is the invariant formatted value.
    /// </summary>
    /// <param name="value">The numeric sort key.</param>
    /// <param name="text">The optional display text.</param>
    /// <returns>The cell.</returns>
    public static ModelTableCell OfNumber(double value, string? text = null) =>
        new(text ?? value.ToString(System.Globalization.CultureInfo.InvariantCulture), value);

    /// <summary>
    /// Creates an identifier cell.
    /// </summary>
    /// <param name="value">The identifier sort key.</param>
    /// <param name="text">The optional display text.</param>
    /// <returns>The cell.</returns>
    public static ModelTableCell OfId(Guid value, string? text = null) => new(text ?? value.ToString("D"), value);
}
