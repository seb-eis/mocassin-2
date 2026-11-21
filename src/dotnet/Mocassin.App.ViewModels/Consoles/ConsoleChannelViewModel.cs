// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// One console channel with a bounded entry buffer and a severity/text filter.
/// </summary>
internal partial class ConsoleChannelViewModel : ObservableObject
{
    /// <summary>
    /// The maximum number of entries retained by the channel.
    /// </summary>
    public const int MaxEntries = 2000;

    private readonly ObservableCollection<ConsoleEntryViewModel> entries = [];

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private ConsoleSeverity minimumSeverity = ConsoleSeverity.Debug;

    /// <summary>
    /// Initializes a new console channel.
    /// </summary>
    /// <param name="id">The stable channel id.</param>
    /// <param name="title">The channel title.</param>
    public ConsoleChannelViewModel(string id, string title)
    {
        Id = id;
        Title = title;
    }

    /// <summary>
    /// Gets the stable channel id.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the channel title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the entries currently passing the filter.
    /// </summary>
    public ObservableCollection<ConsoleEntryViewModel> VisibleEntries { get; } = [];

    /// <summary>
    /// Adds an entry to the channel.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    public void Add(ConsoleEntryViewModel entry)
    {
        entries.Add(entry);
        while (entries.Count > MaxEntries)
        {
            entries.RemoveAt(0);
        }

        if (Matches(entry))
        {
            VisibleEntries.Add(entry);
        }
    }

    /// <summary>
    /// Removes every entry.
    /// </summary>
    [RelayCommand]
    public void Clear()
    {
        entries.Clear();
        VisibleEntries.Clear();
    }

    partial void OnFilterTextChanged(string value) => RefreshFilter();

    partial void OnMinimumSeverityChanged(ConsoleSeverity value) => RefreshFilter();

    private void RefreshFilter()
    {
        VisibleEntries.Clear();
        foreach (var entry in entries)
        {
            if (Matches(entry))
            {
                VisibleEntries.Add(entry);
            }
        }
    }

    private bool Matches(ConsoleEntryViewModel entry)
    {
        if (entry.Severity < MinimumSeverity)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(FilterText)
            || entry.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || entry.Source.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
    }
}
