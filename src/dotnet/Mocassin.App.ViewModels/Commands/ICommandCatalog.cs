// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Stores the explicit shell command catalog.
/// </summary>
internal interface ICommandCatalog
{
    /// <summary>
    /// Gets every registered command.
    /// </summary>
    public IReadOnlyList<ShellCommand> Commands { get; }

    /// <summary>
    /// Occurs when a command is registered.
    /// </summary>
    public event EventHandler? CommandsChanged;

    /// <summary>
    /// Finds a command by id.
    /// </summary>
    /// <param name="id">The command id.</param>
    /// <returns>The command, or <see langword="null"/>.</returns>
    public ShellCommand? Find(string id);

    /// <summary>
    /// Registers a command.
    /// </summary>
    /// <param name="id">The stable command id.</param>
    /// <param name="title">The visible title.</param>
    /// <param name="category">The palette category.</param>
    /// <param name="menu">The optional menu placement.</param>
    /// <param name="gesture">The optional gesture.</param>
    /// <param name="command">The executable command.</param>
    /// <param name="description">The optional description.</param>
    /// <returns>The registered command.</returns>
    public ShellCommand Add(
        string id,
        string title,
        string category,
        MenuKind? menu,
        KeyGestureDescriptor? gesture,
        System.Windows.Input.ICommand command,
        string? description = null);
}
