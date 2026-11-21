// SPDX-License-Identifier: MIT

using System.Windows.Input;

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// One registered shell command with its menu placement and gesture.
/// </summary>
/// <param name="Id">The stable command id.</param>
/// <param name="Title">The visible title.</param>
/// <param name="Category">The palette and cheat-sheet category.</param>
/// <param name="Menu">The optional menu placement.</param>
/// <param name="Gesture">The optional gesture.</param>
/// <param name="Command">The executable command.</param>
/// <param name="GestureText">The formatted platform gesture.</param>
/// <param name="Description">The optional palette description.</param>
internal sealed record ShellCommand(
    string Id,
    string Title,
    string Category,
    MenuKind? Menu,
    KeyGestureDescriptor? Gesture,
    ICommand Command,
    string GestureText,
    string? Description = null)
{
    /// <summary>
    /// Gets a value indicating whether the command can execute.
    /// </summary>
    public bool CanExecute => Command.CanExecute(null);
}
