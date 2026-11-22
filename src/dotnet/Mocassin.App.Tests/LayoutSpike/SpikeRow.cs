// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One selectable row of a spike table. The row carries display cells only; the type-specific editor resolves the
/// model object through <see cref="Id"/> and its owning dataset.
/// </summary>
/// <param name="Domain">The owning domain.</param>
/// <param name="Id">The stable model identifier, or an empty id for a generated breadth row.</param>
/// <param name="Title">The accessible row title.</param>
/// <param name="Cells">The display cells, one per column of the domain's column policy.</param>
/// <param name="Tags">The filter tags used by the domain filter.</param>
/// <param name="IsReadOnly">Whether the row is display-only (implicit, breadth, or non-bound sample).</param>
/// <param name="IsPinned">Whether the row stays at the top regardless of sort.</param>
/// <param name="SourceLabel">The sample label, or <see langword="null"/> for the bound project.</param>
/// <param name="Explanation">The read-only explanation shown when the row is selected, if any.</param>
internal sealed record SpikeRow(
    SpikeDomain Domain,
    Guid Id,
    string Title,
    ImmutableArray<string> Cells,
    ImmutableArray<string> Tags,
    bool IsReadOnly,
    bool IsPinned,
    string? SourceLabel,
    string? Explanation)
{
    /// <summary>
    /// Gets the lower-case search text built from the visible cells and the row title.
    /// </summary>
    public string SearchText { get; } = string.Join('\u001f', Cells.Add(Title)).ToLowerInvariant();

    /// <summary>
    /// Gets the stable row key used to re-select the row after an edit rebuilds the table.
    /// </summary>
    /// <remarks>The key deliberately excludes the title: a rename must not lose the selection.</remarks>
    public string Key { get; } = $"{Domain}:{Id:D}";
}
