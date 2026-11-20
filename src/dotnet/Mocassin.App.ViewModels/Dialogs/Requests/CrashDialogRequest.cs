// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Dialogs.Requests;

/// <summary>
/// Requests the crash dialog after an unhandled exception.
/// </summary>
/// <param name="Message">The human-readable message.</param>
/// <param name="LogFilePath">The log folder or file the user can open.</param>
/// <param name="Details">The optional stack trace and context.</param>
internal sealed record CrashDialogRequest(
    string Message,
    string? LogFilePath,
    string? Details = null) : DialogRequest("MOCASSIN stopped unexpectedly", Message);
