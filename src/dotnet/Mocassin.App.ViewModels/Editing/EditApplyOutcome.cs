// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// Outcome of one coordinator-mediated project edit.
/// </summary>
internal enum EditApplyOutcome
{
    /// <summary>
    /// The edit was accepted; <see cref="EditApplyResult.Edit"/> carries the result, whose
    /// <c>Change</c> is <see langword="null"/> for a no-op same-hash edit.
    /// </summary>
    Applied,

    /// <summary>
    /// The structural validator refused the candidate; nothing changed and the diagnostics explain why.
    /// </summary>
    Rejected,

    /// <summary>
    /// The session is read-only; nothing changed, no diagnostic code was added, and
    /// <see cref="EditApplyResult.Message"/> carries the app-owned refusal message.
    /// </summary>
    ReadOnly,
}
