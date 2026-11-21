// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Requests a single line of text.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Message">The dialog message.</param>
/// <param name="Value">The initial value.</param>
/// <param name="Watermark">The optional watermark.</param>
internal sealed record TextPromptRequest(
    string Title,
    string Message,
    string? Value = null,
    string? Watermark = null) : DialogRequest(Title, Message);
