// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Groups every keyboard-reachable command by category.
/// </summary>
internal sealed partial class ShortcutCheatSheetViewModel : ObservableObject
{
    private readonly Action close;

    /// <summary>
    /// Initializes a cheat sheet.
    /// </summary>
    /// <param name="catalog">The command catalog.</param>
    /// <param name="close">The callback that closes the cheat sheet.</param>
    public ShortcutCheatSheetViewModel(ICommandCatalog catalog, Action close)
    {
        this.close = close;
        Groups = catalog.Commands
            .Where(command => command.Gesture is not null)
            .GroupBy(command => command.Category, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ShortcutGroup(
                group.Key,
                group.OrderBy(command => command.Title, StringComparer.OrdinalIgnoreCase).ToList()))
            .ToList();
    }

    /// <summary>
    /// Gets the shortcut groups.
    /// </summary>
    public IReadOnlyList<ShortcutGroup> Groups { get; }

    /// <summary>
    /// Closes the cheat sheet.
    /// </summary>
    [RelayCommand]
    private void Close() => close();
}
