// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Describes a long-running operation shown in the progress surface.
/// </summary>
/// <param name="Title">The operation title.</param>
/// <param name="StatusText">The initial status text.</param>
/// <param name="IsIndeterminate">Whether the operation duration is unknown.</param>
/// <param name="CanCancel">Whether the operation can be cancelled.</param>
internal sealed record ProgressDialogRequest(
    string Title,
    string? StatusText = null,
    bool IsIndeterminate = false,
    bool CanCancel = true);
