// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// One model-table column. Header and rows consume the same list, so a header cannot drift from its cells.
/// </summary>
/// <param name="Header">The visible header text.</param>
/// <param name="Width">The framework-neutral width.</param>
/// <param name="Minimum">The minimum pixel width the view enforces.</param>
/// <param name="SortKind">The typed comparer.</param>
/// <param name="IsSortable">Whether a header click may sort by this column.</param>
internal sealed record ModelColumn(
    string Header,
    ModelColumnWidth Width,
    double Minimum = 48d,
    ModelSortKind SortKind = ModelSortKind.Text,
    bool IsSortable = true);
