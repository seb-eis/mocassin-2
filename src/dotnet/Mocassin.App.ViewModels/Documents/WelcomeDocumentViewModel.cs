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
        : base(new DocumentKey(DocumentKinds.Welcome))
    {
        ShowShortcutsCommand = showShortcutsCommand;
    }

    /// <inheritdoc />
    public override string Title => "Welcome";

    /// <summary>
    /// Gets the command that opens the shortcut cheat sheet.
    /// </summary>
    public ICommand ShowShortcutsCommand { get; }
}
