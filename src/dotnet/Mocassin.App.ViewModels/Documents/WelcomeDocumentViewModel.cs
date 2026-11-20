// SPDX-License-Identifier: MIT

using System.Windows.Input;

namespace Mocassin.App.ViewModels.Documents;

/// <summary>
/// The empty-state document. It closes automatically when the first real document opens.
/// </summary>
internal sealed class WelcomeDocumentViewModel : DocumentViewModel
{
    /// <summary>
    /// Initializes the welcome document.
    /// </summary>
    /// <param name="showShortcutsCommand">The command that opens the shortcut cheat sheet.</param>
    public WelcomeDocumentViewModel(ICommand showShortcutsCommand)
        : this(showShortcutsCommand, null, null)
    {
    }

    /// <summary>
    /// Initializes the welcome document with project actions.
    /// </summary>
    /// <param name="showShortcutsCommand">The shortcut command.</param>
    /// <param name="newProjectCommand">The new-project command.</param>
    /// <param name="openProjectCommand">The open-project command.</param>
    public WelcomeDocumentViewModel(
        ICommand showShortcutsCommand,
        ICommand? newProjectCommand,
        ICommand? openProjectCommand)
        : base(new DocumentKey(DocumentKinds.Welcome))
    {
        ShowShortcutsCommand = showShortcutsCommand;
        NewProjectCommand = newProjectCommand;
        OpenProjectCommand = openProjectCommand;
    }

    /// <inheritdoc />
    public override string Title => "Welcome";

    /// <summary>
    /// Gets the command that opens the shortcut cheat sheet.
    /// </summary>
    public ICommand ShowShortcutsCommand { get; }

    /// <summary>
    /// Gets the new-project command, when available.
    /// </summary>
    public ICommand? NewProjectCommand { get; }

    /// <summary>
    /// Gets the open-project command, when available.
    /// </summary>
    public ICommand? OpenProjectCommand { get; }
}
