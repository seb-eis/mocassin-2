// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// The kind of input one editor field renders.
/// </summary>
internal enum SpikeFieldKind
{
    /// <summary>A free-text field.</summary>
    Text,

    /// <summary>A searchable reference picker over model objects.</summary>
    Reference,

    /// <summary>A small fixed choice list.</summary>
    Choice,

    /// <summary>A read-only detail line.</summary>
    Static,
}

/// <summary>
/// One field of the type-specific editor. The editor is data-driven: each domain contributes a field list, and all
/// five layouts host the same editor, so the comparison isolates the selection surface.
/// </summary>
internal sealed partial class SpikeEditorField : ObservableObject
{
    private string text = string.Empty;
    private string searchText = string.Empty;
    private string selectedChoice = string.Empty;
    private SpikeReferenceOption? selectedOption;

    /// <summary>
    /// Initializes a field.
    /// </summary>
    /// <param name="key">The stable mutation key.</param>
    /// <param name="label">The visible label.</param>
    /// <param name="kind">The field kind.</param>
    /// <param name="isReadOnly">Whether the field is read-only.</param>
    public SpikeEditorField(string key, string label, SpikeFieldKind kind, bool isReadOnly = false)
    {
        Key = key;
        Label = label;
        Kind = kind;
        IsReadOnly = isReadOnly;
    }

    /// <summary>Gets the stable mutation key.</summary>
    public string Key { get; }

    /// <summary>Gets the visible label and the automation name stem.</summary>
    public string Label { get; }

    /// <summary>Gets the field kind.</summary>
    public SpikeFieldKind Kind { get; }

    /// <summary>Gets a value indicating whether the field is read-only.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Gets the automation name of the field input.</summary>
    public string AutomationName => $"{Label} field";

    /// <summary>Gets the automation name of the reference-picker search box.</summary>
    public string SearchAutomationName => $"{Label} search";

    /// <summary>Gets the text value, also used by the static detail fields.</summary>
    public string Text
    {
        get => text;
        set => SetProperty(ref text, value);
    }

    /// <summary>Gets or sets the picker search query.</summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetProperty(ref searchText, value))
            {
                RefreshVisibleOptions();
            }
        }
    }

    /// <summary>Gets the picker choices before the search filter.</summary>
    public ObservableCollection<SpikeReferenceOption> Options { get; } = [];

    /// <summary>Gets the picker choices after the search filter.</summary>
    public ObservableCollection<SpikeReferenceOption> VisibleOptions { get; } = [];

    /// <summary>Gets or sets the selected picker choice.</summary>
    public SpikeReferenceOption? SelectedOption
    {
        get => selectedOption;
        set => SetProperty(ref selectedOption, value);
    }

    /// <summary>Gets the choice values.</summary>
    public ObservableCollection<string> Choices { get; } = [];

    /// <summary>Gets or sets the selected choice.</summary>
    public string SelectedChoice
    {
        get => selectedChoice;
        set => SetProperty(ref selectedChoice, value);
    }

    /// <summary>
    /// Replaces the picker choices and keeps the selection by identifier.
    /// </summary>
    /// <param name="options">The new choices.</param>
    /// <param name="selectedId">The selected identifier.</param>
    public void SetOptions(IEnumerable<SpikeReferenceOption> options, Guid selectedId)
    {
        Options.Clear();
        foreach (var option in options)
        {
            Options.Add(option);
        }

        SelectedOption = Options.FirstOrDefault(option => option.Id == selectedId) ?? Options.FirstOrDefault();
        RefreshVisibleOptions();
    }

    private void RefreshVisibleOptions()
    {
        var query = SearchText.Trim();
        var filtered = Options.Where(option => option.Matches(query)).ToList();
        VisibleOptions.Clear();
        foreach (var option in filtered)
        {
            VisibleOptions.Add(option);
        }

        if (SelectedOption is not null && !filtered.Contains(SelectedOption))
        {
            SelectedOption = filtered.FirstOrDefault() ?? SelectedOption;
        }
    }
}
