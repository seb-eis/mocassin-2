// SPDX-License-Identifier: MIT

using CommunityToolkit.Mvvm.Input;
using Mocassin.App.ViewModels.Commands;
using Mocassin.App.ViewModels.Editing;
using Mocassin.App.ViewModels.Projects;

namespace Mocassin.App.ViewModels.Shell;

/// <summary>
/// Owns the shell Undo/Redo commands and follows the active project's edit coordinator.
/// </summary>
internal sealed class ShellEditCommands
{
    private readonly IProjectSessionService sessions;
    private ProjectEditCoordinator? editor;

    /// <summary>
    /// Initializes the commands and attaches them to the current active session.
    /// </summary>
    /// <param name="sessions">The project session service.</param>
    public ShellEditCommands(IProjectSessionService sessions)
    {
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        UndoCommand = new RelayCommand(Undo, CanUndo);
        RedoCommand = new RelayCommand(Redo, CanRedo);
        Attach(sessions.ActiveSession);
    }

    /// <summary>
    /// Gets the Undo command.
    /// </summary>
    public RelayCommand UndoCommand { get; }

    /// <summary>
    /// Gets the Redo command.
    /// </summary>
    public RelayCommand RedoCommand { get; }

    /// <summary>
    /// Registers the Edit-menu commands in the shell command catalog.
    /// </summary>
    /// <param name="catalog">The command catalog.</param>
    public void Register(ICommandCatalog catalog)
    {
        catalog.Add(
            "edit.undo",
            "Undo",
            "Edit",
            MenuKind.Edit,
            new KeyGestureDescriptor("Z", GestureModifier.Primary),
            UndoCommand);
        catalog.Add(
            "edit.redo",
            "Redo",
            "Edit",
            MenuKind.Edit,
            new KeyGestureDescriptor("Y", GestureModifier.Primary),
            RedoCommand);
    }

    /// <summary>
    /// Follows the supplied session's edit coordinator.
    /// </summary>
    /// <param name="session">The active session, or <see langword="null"/>.</param>
    public void Attach(ProjectSession? session)
    {
        var next = session?.EditCoordinator;
        if (ReferenceEquals(editor, next))
        {
            return;
        }

        if (editor is not null)
        {
            editor.ProjectChanged -= OnProjectChanged;
        }

        editor = next;
        if (editor is not null)
        {
            editor.ProjectChanged += OnProjectChanged;
        }

        Refresh();
    }

    private void OnProjectChanged(object? sender, ProjectChangedEventArgs args) => Refresh();

    private void Refresh()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private bool CanUndo() =>
        sessions.ActiveSession is { } session
        && session.EditCoordinator.CanEdit
        && session.EditCoordinator.CanUndo;

    private bool CanRedo() =>
        sessions.ActiveSession is { } session
        && session.EditCoordinator.CanEdit
        && session.EditCoordinator.CanRedo;

    private void Undo()
    {
        if (sessions.ActiveSession is { } session)
        {
            session.EditCoordinator.Undo();
        }
    }

    private void Redo()
    {
        if (sessions.ActiveSession is { } session)
        {
            session.EditCoordinator.Redo();
        }
    }
}
