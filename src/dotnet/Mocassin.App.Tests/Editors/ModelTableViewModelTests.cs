// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.ModelTable;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the table view model's selection preservation, multi-select policy, search, and typed sort.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ModelTableViewModelTests
{
    private static readonly double[] ascending = [2d, 10d, 100d];
    private static readonly double[] descending = [100d, 10d, 2d];

    private static readonly IReadOnlyList<ModelColumn> columns =
    [
        new("Name", ModelColumnWidth.Star(2d)),
        new("Value", ModelColumnWidth.Fixed(80d), SortKind: ModelSortKind.Number),
    ];

    [Fact]
    public void RebuildingRowsPreservesSelectionByObjectId()
    {
        var table = new ModelTableViewModel();
        table.SetColumns(columns);
        var first = Row(1, "A", 2);
        table.SetRows([first, Row(2, "B", 1)]);
        table.UpdateSelection([first]);

        table.SetRows([Row(1, "A renamed", 3), Row(2, "B", 1)]);

        Assert.Equal(first.ObjectId, table.SelectedRow?.ObjectId);
        Assert.Equal("A renamed", table.SelectedRow?.Cells[0].Text);
    }

    [Fact]
    public void MultiSelectOffKeepsOnlyTheFirstRow()
    {
        var table = new ModelTableViewModel { IsMultiSelectEnabled = false };
        table.SetColumns(columns);
        var one = Row(1, "A", 2);
        var two = Row(2, "B", 1);
        table.SetRows([one, two]);

        table.UpdateSelection([one, two]);

        Assert.Single(table.SelectedRows);
        Assert.Same(one, table.SelectedRow);

        table.IsMultiSelectEnabled = true;
        table.UpdateSelection([one, two]);
        Assert.Equal(2, table.SelectedRows.Count);
    }

    [Fact]
    public void SelectingAFilteredRowClearsTheFilters()
    {
        var table = new ModelTableViewModel();
        table.SetColumns(columns);
        var target = Row(2, "Target", 1);
        table.SetRows([Row(1, "Other", 2), target]);
        table.SearchText = "Other";
        Assert.Single(table.Rows);

        Assert.True(table.SelectObject(target.ObjectId));

        Assert.Equal(string.Empty, table.SearchText);
        Assert.Equal(2, table.Rows.Count);
        Assert.Same(target, table.SelectedRow);
    }

    [Fact]
    public void TypedSortUsesTheNumericKeyAndTogglesDirection()
    {
        var table = new ModelTableViewModel();
        table.SetColumns(columns);
        table.SetRows([Row(1, "Ten", 10), Row(2, "Two", 2), Row(3, "Hundred", 100)]);

        table.SortBy(columns[1]);
        Assert.Equal(ascending, table.Rows.Select(row => (double)row.Cells[1].SortKey!).ToArray());

        table.SortBy(columns[1]);
        Assert.True(table.SortDescending);
        Assert.Equal(descending, table.Rows.Select(row => (double)row.Cells[1].SortKey!).ToArray());
    }

    [Fact]
    public void SearchAndSeverityFilterCompose()
    {
        var table = new ModelTableViewModel();
        table.SetColumns(columns);
        table.SetRows(
        [
            Row(1, "Alpha", 1, ModelRowSeverity.None),
            Row(2, "Alpha error", 2, ModelRowSeverity.Error),
            Row(3, "Beta error", 3, ModelRowSeverity.Error),
        ]);

        table.SearchText = "Alpha";
        table.SeverityFilter = ModelSeverityFilter.Errors;

        var row = Assert.Single(table.Rows);
        Assert.Equal(Id(2), row.ObjectId);
        Assert.Equal("1 of 3 rows", table.MatchCountText);
    }

    private static Guid Id(int value) => new(value, 0, 0, new byte[8]);

    private static ModelTableRow Row(int id, string name, double value, ModelRowSeverity severity = ModelRowSeverity.None) =>
        new(
            Id(id),
            [ModelTableCell.OfText(name), ModelTableCell.OfNumber(value)],
            severity,
            severity == ModelRowSeverity.None ? string.Empty : severity.ToString());
}
