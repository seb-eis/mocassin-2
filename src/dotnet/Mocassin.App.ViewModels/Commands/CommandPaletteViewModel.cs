// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Filters and executes registered commands from the keyboard.
/// </summary>
internal sealed partial class CommandPaletteViewModel : ObservableObject
{
    private readonly ICommandCatalog catalog;
    private readonly Action close;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ShellCommand? selectedCommand;

    /// <summary>
    /// Initializes a command palette.
    /// </summary>
    /// <param name="catalog">The command catalog.</param>
    /// <param name="close">The callback that closes the palette.</param>
    public CommandPaletteViewModel(ICommandCatalog catalog, Action close)
    {
        this.catalog = catalog;
        this.close = close;
    }

    /// <summary>
    /// Gets the filtered results.
    /// </summary>
    public ObservableCollection<ShellCommand> Results { get; } = [];

    /// <summary>
    /// Refreshes the results for the current search text.
    /// </summary>
    public void Refresh()
    {
        var query = SearchText.Trim();
        var matches = catalog.Commands
            .Where(command => Matches(command, query))
            .OrderBy(command => command.Title, StringComparer.OrdinalIgnoreCase);
        Results.Clear();
        foreach (var command in matches)
        {
            Results.Add(command);
        }

        SelectedCommand = Results.FirstOrDefault();
    }

    /// <summary>
    /// Executes the selected command and closes the palette.
    /// </summary>
    [RelayCommand]
    private void ExecuteSelected()
    {
        var command = SelectedCommand;
        close();
        if (command?.CanExecute == true)
        {
            command.Command.Execute(null);
        }
    }

    /// <summary>
    /// Closes the palette.
    /// </summary>
    [RelayCommand]
    private void Cancel() => close();

    partial void OnSearchTextChanged(string value) => Refresh();

    private static bool Matches(ShellCommand command, string query)
    {
        if (query.Length == 0)
        {
            return true;
        }

        return command.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || command.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
            || command.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || command.GestureText.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
