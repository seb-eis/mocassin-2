// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// The shared model-table surface state: the column policy, the projected rows, typed sorting, one search box, one
/// severity filter, and multi-select.
/// </summary>
/// <remarks>
/// The view is a virtualized list over <see cref="Rows"/>; it never owns the projection or the comparers. Header
/// and rows consume the same <see cref="Columns"/> list, so a column cannot drift between them.
/// </remarks>
internal sealed partial class ModelTableViewModel : ObservableObject
{
    private readonly List<ModelTableRow> allRows = [];
    private IReadOnlyList<ModelColumn> columns = [];
    private int sortColumnIndex = -1;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ModelSeverityFilter severityFilter = ModelSeverityFilter.All;

    [ObservableProperty]
    private ModelTableRow? selectedRow;

    [ObservableProperty]
    private ModelColumn? sortColumn;

    [ObservableProperty]
    private bool sortDescending;

    /// <summary>
    /// Gets the severity filter choices for the view.
    /// </summary>
    public IReadOnlyList<ModelSeverityFilter> SeverityFilters { get; } =
    [
        ModelSeverityFilter.All,
        ModelSeverityFilter.Warnings,
        ModelSeverityFilter.Errors,
    ];

    /// <summary>
    /// Gets the visible rows after search, severity filter, and sort.
    /// </summary>
    public ObservableCollection<ModelTableRow> Rows { get; } = [];

    /// <summary>
    /// Gets the selected rows, in table order. The first entry drives the detail pane.
    /// </summary>
    public ObservableCollection<ModelTableRow> SelectedRows { get; } = [];

    /// <summary>
    /// Gets the column policy shared by the header and every row.
    /// </summary>
    public IReadOnlyList<ModelColumn> Columns => columns;

    /// <summary>
    /// Gets the total row count before filtering.
    /// </summary>
    public int TotalCount => allRows.Count;

    /// <summary>
    /// Gets the visible row count.
    /// </summary>
    public int MatchCount => Rows.Count;

    /// <summary>
    /// Gets a value indicating whether the projection is empty.
    /// </summary>
    public bool IsEmpty => allRows.Count == 0;

    /// <summary>
    /// Gets a value indicating whether a filter hides every row.
    /// </summary>
    public bool IsFilteredEmpty => allRows.Count > 0 && Rows.Count == 0;

    /// <summary>
    /// Gets or sets a value indicating whether multi-select is enabled.
    /// </summary>
    public bool IsMultiSelectEnabled { get; set; }

    /// <summary>
    /// Gets or sets the empty-state text the editor supplies.
    /// </summary>
    public string? EmptyMessage { get; set; }

    /// <summary>
    /// Gets the announcement text for the match count.
    /// </summary>
    public string MatchCountText => MatchCount == TotalCount
        ? TotalCount == 1 ? "1 row" : string.Create(CultureInfo.InvariantCulture, $"{TotalCount} rows")
        : string.Create(CultureInfo.InvariantCulture, $"{MatchCount} of {TotalCount} rows");

    /// <summary>
    /// Sets the column policy. Header and rows rebuild from the returned list.
    /// </summary>
    /// <param name="value">The columns in display order.</param>
    public void SetColumns(IReadOnlyList<ModelColumn> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        columns = value;
        sortColumnIndex = SortColumn is null ? -1 : IndexOf(SortColumn);
        OnPropertyChanged(nameof(Columns));
        OnPropertyChanged(nameof(SortColumn));
    }

    /// <summary>
    /// Replaces the full row projection and preserves the selection by object id.
    /// </summary>
    /// <param name="rows">The rows in domain order.</param>
    /// <param name="preserveSelection">The optional object id to select after the rebuild.</param>
    public void SetRows(IEnumerable<ModelTableRow> rows, Guid? preserveSelection = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var selectedIds = SelectedRows.Select(static row => row.ObjectId).ToHashSet();
        if (preserveSelection is { } id)
        {
            selectedIds.Add(id);
        }

        allRows.Clear();
        allRows.AddRange(rows);
        RefreshRows(selectedIds);
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>
    /// Selects one row by object id, clearing the filters when they hide it.
    /// </summary>
    /// <param name="objectId">The stable object id.</param>
    /// <returns><see langword="true"/> when the row exists and was selected.</returns>
    public bool SelectObject(Guid objectId)
    {
        var row = allRows.FirstOrDefault(candidate => candidate.ObjectId == objectId);
        if (row is null)
        {
            return false;
        }

        if (!Rows.Contains(row))
        {
            SearchText = string.Empty;
            SeverityFilter = ModelSeverityFilter.All;
        }

        UpdateSelection([row]);
        return true;
    }

    /// <summary>
    /// Determines whether the unfiltered projection contains an object.
    /// </summary>
    /// <param name="objectId">The stable object id.</param>
    /// <returns><see langword="true"/> when the row exists before filtering.</returns>
    public bool ContainsObject(Guid objectId) =>
        allRows.Any(row => row.ObjectId == objectId);

    /// <summary>
    /// Applies a view selection to the row model.
    /// </summary>
    /// <param name="rows">The selected rows in table order.</param>
    public void UpdateSelection(IEnumerable<ModelTableRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var selection = rows.Where(Rows.Contains).ToList();
        if (!IsMultiSelectEnabled && selection.Count > 1)
        {
            selection = [selection[0]];
        }

        SelectedRows.Clear();
        foreach (var row in selection)
        {
            SelectedRows.Add(row);
        }

        SelectedRow = selection.Count > 0 ? selection[0] : null;
    }

    /// <summary>
    /// Sorts by a column, toggling the direction when the column is already active.
    /// </summary>
    /// <param name="column">The column whose header was activated.</param>
    public void SortBy(ModelColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!column.IsSortable)
        {
            return;
        }

        var index = IndexOf(column);
        if (index < 0)
        {
            return;
        }

        if (sortColumnIndex == index)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            sortColumnIndex = index;
            SortColumn = column;
            SortDescending = false;
        }

        RefreshRows(SelectedRows.Select(static row => row.ObjectId).ToHashSet());
    }

    /// <summary>
    /// Clears the search and severity filters.
    /// </summary>
    public void ClearFilters()
    {
        SearchText = string.Empty;
        SeverityFilter = ModelSeverityFilter.All;
    }

    partial void OnSearchTextChanged(string value) => RefreshRows(SelectedIds());

    partial void OnSeverityFilterChanged(ModelSeverityFilter value) => RefreshRows(SelectedIds());

    private HashSet<Guid> SelectedIds() => SelectedRows.Select(static row => row.ObjectId).ToHashSet();

    private void RefreshRows(HashSet<Guid> selectedIds)
    {
        var selected = selectedIds.Count == 0 ? SelectedIds() : selectedIds;
        IEnumerable<ModelTableRow> query = allRows;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(row => row.Cells.Any(cell => cell.Text.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        query = SeverityFilter switch
        {
            ModelSeverityFilter.Errors => query.Where(static row => row.HasError),
            ModelSeverityFilter.Warnings => query.Where(static row => row.HasWarning),
            _ => query,
        };

        if (sortColumnIndex >= 0 && sortColumnIndex < columns.Count)
        {
            var index = sortColumnIndex;
            var kind = columns[index].SortKind;
            query = SortDescending
                ? query.OrderByDescending(row => row, Comparer<ModelTableRow>.Create((left, right) => Compare(left, right, index, kind)))
                : query.OrderBy(row => row, Comparer<ModelTableRow>.Create((left, right) => Compare(left, right, index, kind)));
        }

        Rows.Clear();
        foreach (var row in query)
        {
            Rows.Add(row);
        }

        var selectedRows = Rows.Where(row => selected.Contains(row.ObjectId)).ToList();
        SelectedRows.Clear();
        foreach (var row in selectedRows)
        {
            SelectedRows.Add(row);
        }

        SelectedRow = selectedRows.Count > 0 ? selectedRows[0] : null;

        OnPropertyChanged(nameof(MatchCount));
        OnPropertyChanged(nameof(MatchCountText));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    private int IndexOf(ModelColumn column)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            if (ReferenceEquals(columns[index], column) || columns[index] == column)
            {
                return index;
            }
        }

        return -1;
    }

    private static int Compare(ModelTableRow left, ModelTableRow right, int index, ModelSortKind kind)
    {
        var leftCell = index < left.Cells.Length ? left.Cells[index] : default;
        var rightCell = index < right.Cells.Length ? right.Cells[index] : default;
        return kind switch
        {
            ModelSortKind.Number => ToDouble(leftCell).CompareTo(ToDouble(rightCell)),
            ModelSortKind.Id => ToGuid(leftCell).CompareTo(ToGuid(rightCell)),
            _ => string.Compare(Text(leftCell), Text(rightCell), StringComparison.OrdinalIgnoreCase),
        };
    }

    private static string Text(ModelTableCell cell) => cell.Text ?? string.Empty;

    private static double ToDouble(ModelTableCell cell) => cell.SortKey switch
    {
        double value => value,
        int value => value,
        long value => value,
        float value => value,
        _ => double.TryParse(cell.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0d,
    };

    private static Guid ToGuid(ModelTableCell cell) => cell.SortKey switch
    {
        Guid value => value,
        _ => Guid.TryParse(cell.Text, out var parsed) ? parsed : Guid.Empty,
    };
}
