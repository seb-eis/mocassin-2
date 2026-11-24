// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.ViewModels.Editors.ReferencePicker;

/// <summary>
/// The one reusable searchable reference picker. Avalonia has no searchable combo, so this composes a search box
/// with a filtered combo; the same view model is used for particle sets, set membership, and later group/site/chain
/// references.
/// </summary>
/// <typeparam name="TReference">The referenced value type.</typeparam>
internal sealed partial class ReferencePickerViewModel<TReference> : ObservableObject
    where TReference : notnull
{
    private ImmutableArray<ReferenceOption<TReference>> options = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ReferenceOption<TReference>? selected;

    [ObservableProperty]
    private int highlightedIndex = -1;

    [ObservableProperty]
    private string? validationMessage;

    /// <summary>
    /// Initializes a picker.
    /// </summary>
    /// <param name="placeholder">The empty-state prompt.</param>
    /// <param name="isEnabled">Whether the picker accepts input.</param>
    public ReferencePickerViewModel(string placeholder = "Select…", bool isEnabled = true)
    {
        Placeholder = placeholder;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Gets or sets the empty-state prompt.
    /// </summary>
    public string Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the picker accepts input.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the filtered options for the current search text.
    /// </summary>
    public IReadOnlyList<ReferenceOption<TReference>> FilteredOptions
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return options;
            }

            var term = SearchText.Trim();
            return options
                .Where(option => option.SearchText.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the drop-down is open.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Raised when the picker asks the view to close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised when the selection changes through the picker's keyboard path.
    /// </summary>
    public event EventHandler<ReferenceOption<TReference>?>? SelectionChanged;

    /// <summary>
    /// Replaces the available options and keeps a selection whose value still exists.
    /// </summary>
    /// <param name="value">The options in display order.</param>
    public void SetOptions(IEnumerable<ReferenceOption<TReference>> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var previous = Selected;
        options = [.. value];
        OnPropertyChanged(nameof(FilteredOptions));
        if (previous is not null)
        {
            Selected = options.FirstOrDefault(option => EqualityComparer<TReference>.Default.Equals(
                option.Value,
                previous.Value));
        }
    }

    /// <summary>
    /// Opens the drop-down and resets the highlight.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
        HighlightedIndex = FilteredOptions.Count > 0 ? 0 : -1;
        OnPropertyChanged(nameof(IsOpen));
    }

    /// <summary>
    /// Closes the drop-down and asks the view to return focus.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        OnPropertyChanged(nameof(IsOpen));
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Moves the highlight down.
    /// </summary>
    public void MoveDown()
    {
        var count = FilteredOptions.Count;
        HighlightedIndex = count == 0 ? -1 : Math.Min(HighlightedIndex + 1, count - 1);
    }

    /// <summary>
    /// Moves the highlight up.
    /// </summary>
    public void MoveUp()
    {
        var count = FilteredOptions.Count;
        HighlightedIndex = count == 0 ? -1 : Math.Max(HighlightedIndex - 1, 0);
    }

    /// <summary>
    /// Selects the highlighted option, or the only filtered option when nothing is highlighted.
    /// </summary>
    /// <returns><see langword="true"/> when a selection was made.</returns>
    public bool SelectCurrent()
    {
        var filtered = FilteredOptions;
        var index = HighlightedIndex;
        if (index < 0 && filtered.Count == 1)
        {
            index = 0;
        }

        if (index < 0 || index >= filtered.Count)
        {
            return false;
        }

        Selected = filtered[index];
        SearchText = string.Empty;
        Close();
        SelectionChanged?.Invoke(this, Selected);
        return true;
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredOptions));
        HighlightedIndex = FilteredOptions.Count > 0 ? 0 : -1;
    }
}
