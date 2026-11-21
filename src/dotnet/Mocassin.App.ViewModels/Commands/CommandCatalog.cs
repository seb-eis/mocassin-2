// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Commands;

/// <summary>
/// Explicit command catalog that rejects duplicate ids.
/// </summary>
internal sealed class CommandCatalog : ICommandCatalog
{
    private readonly List<ShellCommand> commands = [];
    private readonly GestureFormatter formatter;

    /// <summary>
    /// Initializes a command catalog.
    /// </summary>
    /// <param name="formatter">The platform gesture formatter.</param>
    public CommandCatalog(GestureFormatter formatter)
    {
        this.formatter = formatter;
    }

    /// <inheritdoc />
    public IReadOnlyList<ShellCommand> Commands => commands;

    /// <inheritdoc />
    public event EventHandler? CommandsChanged;

    /// <inheritdoc />
    public ShellCommand? Find(string id) =>
        commands.FirstOrDefault(command => string.Equals(command.Id, id, StringComparison.Ordinal));

    /// <inheritdoc />
    public ShellCommand Add(
        string id,
        string title,
        string category,
        MenuKind? menu,
        KeyGestureDescriptor? gesture,
        System.Windows.Input.ICommand command,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(command);
        if (Find(id) is not null)
        {
            throw new InvalidOperationException($"A command with id '{id}' is already registered.");
        }

        var registered = new ShellCommand(
            id,
            title,
            category,
            menu,
            gesture,
            command,
            gesture is null ? string.Empty : formatter.Format(gesture.Value),
            description);
        commands.Add(registered);
        CommandsChanged?.Invoke(this, EventArgs.Empty);
        return registered;
    }
}
