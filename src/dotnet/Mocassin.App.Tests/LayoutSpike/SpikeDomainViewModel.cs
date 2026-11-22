// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Editing;

using Mocassin.App.Tests.LayoutSpike.Data;
using Mocassin.App.Tests.LayoutSpike.Editing;

namespace Mocassin.App.Tests.LayoutSpike;

/// <summary>
/// One domain's selection surface: the shared column policy, the search/filter/sort state, the visible rows, and the
/// type-specific editor for the current selection.
/// </summary>
/// <remarks>
/// All five layout options bind to this same view model. The spike therefore compares layouts, not data plumbing:
/// sorting and filtering are view-model code in every option, and only the option-specific wiring differs.
/// </remarks>
internal sealed partial class SpikeDomainViewModel : ObservableObject
{
    private readonly ImmutableArray<SpikeRow> breadthRows;
    private readonly ImmutableArray<SpikeRow> generatedRows;
    private string searchText = string.Empty;
    private SpikeFilterOption filter;
    private string sortHeader = "Name";
    private bool sortDescending;
    private SpikeRow? selectedRow;
    private string actionText = string.Empty;
    private bool useGeneratedBreadth;

    /// <summary>
    /// Initializes a domain surface.
    /// </summary>
    /// <param name="domain">The domain.</param>
    /// <param name="editable">The editable dataset that owns this domain, if any.</param>
    /// <param name="breadthRows">Display-only rows from the other samples.</param>
    /// <param name="generatedTable">The generated breadth table for this domain, if any.</param>
    public SpikeDomainViewModel(
        SpikeDomain domain,
        SpikeDataset? editable,
        ImmutableArray<SpikeRow> breadthRows,
        SpikeTable? generatedTable)
    {
        Domain = domain;
        Editable = editable;
        this.breadthRows = breadthRows;
        generatedRows = generatedTable?.Rows ?? [];
        Columns = SpikeColumns.For(domain);
        Filters = SpikeColumns.Filters(domain);
        filter = Filters[0];
        Editor = new SpikeEditorViewModel(this);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, CanRemoveSelected);
        ApplyView();
    }

    /// <summary>Gets the domain.</summary>
    public SpikeDomain Domain { get; }

    /// <summary>Gets the visible domain title.</summary>
    public string Title => Domain switch
    {
        SpikeDomain.Particles => "Particles",
        SpikeDomain.ParticleSets => "Particle sets",
        SpikeDomain.Sites => "Sites",
        SpikeDomain.StateChanges => "State changes",
        SpikeDomain.Chains => "Chains",
        SpikeDomain.Transitions => "Transitions",
        _ => Domain.ToString(),
    };

    /// <summary>Gets the shared column policy.</summary>
    public ImmutableArray<SpikeColumn> Columns { get; }

    /// <summary>Gets the filter choices.</summary>
    public ImmutableArray<SpikeFilterOption> Filters { get; }

    /// <summary>Gets the sort choices, which are the column headers.</summary>
    public IReadOnlyList<string> SortOptions => [.. Columns.Select(column => column.Header)];

    /// <summary>Gets the editable dataset for this domain, or <see langword="null"/> for display-only domains.</summary>
    public SpikeDataset? Editable { get; }

    /// <summary>Gets the visible rows, filtered and sorted.</summary>
    public ObservableCollection<SpikeRow> Items { get; } = [];

    /// <summary>Gets the type-specific editor for the current selection.</summary>
    public SpikeEditorViewModel Editor { get; }

    /// <summary>Gets the bulk-remove command; enabled only where multi-select delete is safe.</summary>
    public RelayCommand RemoveSelectedCommand { get; }

    /// <summary>Gets a value indicating whether the domain supports multi-select.</summary>
    public bool SupportsMultiSelect =>
        Domain is SpikeDomain.Particles or SpikeDomain.ParticleSets or SpikeDomain.Sites;

    /// <summary>Gets a value indicating whether generated breadth rows replace the real rows.</summary>
    public bool HasGeneratedBreadth => !generatedRows.IsDefaultOrEmpty;

    /// <summary>Gets or sets the search query.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                ApplyView();
            }
        }
    }

    /// <summary>Gets or sets the active filter.</summary>
    public SpikeFilterOption Filter
    {
        get => filter;
        set
        {
            if (value is not null && SetProperty(ref filter, value))
            {
                ApplyView();
            }
        }
    }

    /// <summary>Gets or sets the sort header.</summary>
    public string SortHeader
    {
        get => sortHeader;
        set
        {
            if (!string.IsNullOrEmpty(value) && SetProperty(ref sortHeader, value))
            {
                ApplyView();
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether the sort is descending.</summary>
    public bool SortDescending
    {
        get => sortDescending;
        set
        {
            if (SetProperty(ref sortDescending, value))
            {
                ApplyView();
            }
        }
    }

    /// <summary>Gets or sets the selected row.</summary>
    public SpikeRow? SelectedRow
    {
        get => selectedRow;
        private set => SetProperty(ref selectedRow, value);
    }

    /// <summary>Gets the selected rows, in visible order.</summary>
    public List<SpikeRow> SelectedRows { get; } = [];

    /// <summary>Gets the selection and row-count status text.</summary>
    public string StatusText
    {
        get
        {
            var selection = SelectedRows.Count == 0 ? "no selection" : $"{SelectedRows.Count} selected";
            var filterText = filter.RequiredTag is null ? string.Empty : $", filter {filter.Label}";
            var sortText = string.IsNullOrEmpty(sortHeader)
                ? string.Empty
                : $", sort {sortHeader}{(sortDescending ? " ↓" : " ↑")}";
            var generated = useGeneratedBreadth ? ", generated breadth" : string.Empty;
            return $"{Title}: {Items.Count} rows, {selection}{filterText}{sortText}{generated}";
        }
    }

    /// <summary>Gets the last bulk action message, if any.</summary>
    public string ActionText
    {
        get => actionText;
        private set => SetProperty(ref actionText, value);
    }

    /// <summary>Gets a value indicating whether the visible rows are being rebuilt.</summary>
    public bool IsRefreshing { get; private set; }

    /// <summary>
    /// Enables or disables the generated 1,024-row breadth for this domain.
    /// </summary>
    /// <param name="enabled">Whether generated rows replace the real rows.</param>
    public void SetGeneratedBreadth(bool enabled)
    {
        if (!HasGeneratedBreadth || useGeneratedBreadth == enabled)
        {
            return;
        }

        useGeneratedBreadth = enabled;
        ApplyView();
    }

    /// <summary>
    /// Applies the multi-select selection reported by the list surface.
    /// </summary>
    /// <param name="rows">The selected rows in list order.</param>
    public void SetSelection(IEnumerable<SpikeRow> rows)
    {
        SelectedRows.Clear();
        SelectedRows.AddRange(rows);
        SelectedRow = SelectedRows.Count == 0 ? null : SelectedRows[^1];
        Editor.Load(SelectedRow);
        RemoveSelectedCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Rebuilds the visible rows from the current project values while preserving the selection by row key.
    /// </summary>
    public void Refresh()
    {
        ApplyView();
    }

    /// <summary>
    /// Cycles the sort on a header click.
    /// </summary>
    /// <param name="header">The clicked column header.</param>
    public void CycleSort(string header)
    {
        if (string.Equals(sortHeader, header, StringComparison.Ordinal))
        {
            SortDescending = !SortDescending;
            return;
        }

        sortDescending = false;
        SortHeader = header;
        OnPropertyChanged(nameof(SortDescending));
    }

    /// <summary>
    /// Clears the search text and resets the filter.
    /// </summary>
    public void ClearFilters()
    {
        searchText = string.Empty;
        OnPropertyChanged(nameof(SearchText));
        Filter = Filters[0];
    }

    /// <summary>
    /// Returns the rows that back the surface before filtering.
    /// </summary>
    /// <returns>The source rows.</returns>
    public IEnumerable<SpikeRow> SourceRows()
    {
        if (useGeneratedBreadth && !generatedRows.IsDefaultOrEmpty)
        {
            return generatedRows;
        }

        var editableRows = Editable?.Table(Domain)?.Rows ?? [];
        return editableRows.AddRange(breadthRows);
    }

    private void ApplyView()
    {
        IsRefreshing = true;
        try
        {
            var selectedKeys = SelectedRows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            if (selectedRow is not null)
            {
                selectedKeys.Add(selectedRow.Key);
            }

            var query = searchText.Trim();
            IEnumerable<SpikeRow> rows = SourceRows()
                .Where(row => filter.Matches(row))
                .Where(row => query.Length == 0 || row.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase));
            rows = Sort(rows);

            Items.Clear();
            foreach (var row in rows)
            {
                Items.Add(row);
            }

            var reselected = Items.Where(row => selectedKeys.Contains(row.Key)).ToList();
            if (reselected.Count == 0 && Items.Count > 0 && selectedRow is not null)
            {
                reselected.Add(Items[0]);
            }

            SelectedRows.Clear();
            SelectedRows.AddRange(reselected);
            SelectedRow = SelectedRows.Count == 0 ? null : SelectedRows[^1];
            Editor.Load(SelectedRow);
            RemoveSelectedCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsRefreshing = false;
        }

        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HasGeneratedBreadth));
    }

    private IEnumerable<SpikeRow> Sort(IEnumerable<SpikeRow> rows)
    {
        var pinned = rows.Where(row => row.IsPinned);
        var rest = rows.Where(row => !row.IsPinned);
        var index = -1;
        for (var candidate = 0; candidate < Columns.Length; candidate++)
        {
            if (string.Equals(Columns[candidate].Header, sortHeader, StringComparison.Ordinal))
            {
                index = candidate;
                break;
            }
        }

        if (index < 0)
        {
            return pinned.Concat(rest);
        }

        var comparer = Comparer<string>.Create(
            static (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));
        // The bound document's rows always precede display-only sample rows; the sample order is otherwise noise.
        var primary = rest.Where(row => !row.IsReadOnly);
        var secondary = rest.Where(row => row.IsReadOnly);
        var ordered = sortDescending
            ? primary.OrderByDescending(row => Cell(row, index), comparer)
                .Concat(secondary.OrderByDescending(row => Cell(row, index), comparer))
            : primary.OrderBy(row => Cell(row, index), comparer)
                .Concat(secondary.OrderBy(row => Cell(row, index), comparer));
        return pinned.Concat(ordered);
    }

    private static string Cell(SpikeRow row, int index) =>
        index >= 0 && index < row.Cells.Length ? row.Cells[index] : string.Empty;

    private bool CanRemoveSelected() =>
        SupportsMultiSelect
        && SelectedRows.Count > 0
        && Editable is not null
        && !useGeneratedBreadth
        && SelectedRows.All(row => !row.IsReadOnly);

    private void RemoveSelected()
    {
        if (Editable?.Coordinator is not { } coordinator)
        {
            return;
        }

        var ids = SelectedRows.Select(row => row.Id).ToHashSet();
        var candidate = SpikeEditorMutation.Remove(
            coordinator.Document.Project,
            Domain,
            ids,
            out var description);
        var result = coordinator.Apply(description, _ => candidate);
        ActionText = result.Outcome switch
        {
            EditApplyOutcome.Applied => $"Removed {ids.Count} object(s).",
            _ => $"Removal refused: {result.Message ?? "the project validator rejected the edit."}",
        };
        OnPropertyChanged(nameof(StatusText));
    }
}
