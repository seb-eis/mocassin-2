// SPDX-License-Identifier: MIT

using Mocassin.App.ViewModels.Editors.ReferencePicker;

namespace Mocassin.App.Tests.Editors;

/// <summary>
/// Pins the reusable reference picker's search, highlight, selection, and close contract.
/// </summary>
[Trait("Tier", "PR-fast")]
public sealed class ReferencePickerTests
{
    [Fact]
    public void SearchFiltersByNameAndDetails()
    {
        var picker = Create();
        picker.SetOptions(
        [
            new ReferenceOption<int>(1, "Cerium", "Ce4+"),
            new ReferenceOption<int>(2, "Oxygen", "O2-"),
            new ReferenceOption<int>(3, "Vacancy", null),
        ]);

        picker.SearchText = "o2";
        var filtered = Assert.Single(picker.FilteredOptions);
        Assert.Equal(2, filtered.Value);

        picker.SearchText = "vac";
        Assert.Equal(3, Assert.Single(picker.FilteredOptions).Value);

        picker.SearchText = "missing";
        Assert.Empty(picker.FilteredOptions);
        Assert.Equal(-1, picker.HighlightedIndex);
    }

    [Fact]
    public void KeyboardSelectionMovesTheHighlightAndRaisesTheSelection()
    {
        var picker = Create();
        picker.SetOptions(
        [
            new ReferenceOption<int>(1, "A"),
            new ReferenceOption<int>(2, "B"),
            new ReferenceOption<int>(3, "C"),
        ]);
        var selected = 0;
        var closed = false;
        picker.SelectionChanged += (_, option) => selected = option?.Value ?? 0;
        picker.CloseRequested += (_, _) => closed = true;

        picker.Open();
        picker.MoveDown();
        picker.MoveDown();
        Assert.True(picker.SelectCurrent());

        Assert.Equal(3, selected);
        Assert.Equal(3, picker.Selected?.Value);
        Assert.True(closed);
        Assert.False(picker.IsOpen);
    }

    [Fact]
    public void ANonMatchingOptionCannotBeSelected()
    {
        var picker = Create();
        picker.SetOptions([new ReferenceOption<int>(1, "Only")]);
        picker.SearchText = "missing";

        Assert.False(picker.SelectCurrent());
        Assert.Null(picker.Selected);
    }

    [Fact]
    public void ReplacingOptionsKeepsAStillValidSelection()
    {
        var picker = Create();
        picker.SetOptions([new ReferenceOption<int>(1, "A"), new ReferenceOption<int>(2, "B")]);
        picker.Selected = picker.FilteredOptions[1];

        picker.SetOptions([new ReferenceOption<int>(2, "B renamed"), new ReferenceOption<int>(3, "C")]);

        Assert.Equal(2, picker.Selected?.Value);
        Assert.Equal("B renamed", picker.Selected?.DisplayName);
    }

    private static ReferencePickerViewModel<int> Create() => new("Select a particle");
}
