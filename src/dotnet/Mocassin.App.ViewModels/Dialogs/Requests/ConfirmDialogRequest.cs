// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Requests a confirmation.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="ConfirmText">The confirm button text.</param>
/// <param name="CancelText">The cancel button text.</param>
/// <param name="IsDestructive">Whether the confirm action is destructive.</param>
internal sealed record ConfirmDialogRequest(
    string Title,
    string Message,
    string ConfirmText = "OK",
    string CancelText = "Cancel",
    bool IsDestructive = false) : DialogRequest(Title, Message);
