// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// One code/message pair shown in an error dialog's details section.
/// </summary>
/// <param name="Code">The stable diagnostic code.</param>
/// <param name="Message">The human-readable message.</param>
internal sealed record ErrorDialogDetail(string Code, string Message);
