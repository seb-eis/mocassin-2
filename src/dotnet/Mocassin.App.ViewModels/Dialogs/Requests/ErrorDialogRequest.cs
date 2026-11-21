// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Requests an error report with optional diagnostics.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="Details">The optional technical details.</param>
internal sealed record ErrorDialogRequest(
    string Title,
    string Message,
    IReadOnlyList<ErrorDialogDetail>? Details = null) : DialogRequest(Title, Message);
