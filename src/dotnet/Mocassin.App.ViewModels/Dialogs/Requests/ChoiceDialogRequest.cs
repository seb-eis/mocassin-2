// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Requests a choice from a fixed set of options.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="Choices">The selectable choices.</param>
internal sealed record ChoiceDialogRequest(
    string Title,
    string Message,
    IReadOnlyList<DialogChoice> Choices) : DialogRequest(Title, Message);
