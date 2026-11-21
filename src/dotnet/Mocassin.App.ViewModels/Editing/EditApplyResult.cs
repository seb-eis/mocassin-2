// SPDX-License-Identifier: MIT

using Mocassin.Core.Editing;

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// Result of one <see cref="ProjectEditCoordinator.Apply"/> call.
/// </summary>
/// <param name="Outcome">The app-level outcome.</param>
/// <param name="Edit">
/// The Core edit result for an accepted or rejected edit, or <see langword="null"/> for a read-only refusal.
/// </param>
/// <param name="Message">
/// An app-owned message for a read-only refusal or the first validation error for a rejection; otherwise
/// <see langword="null"/>.
/// </param>
internal sealed record EditApplyResult(
    EditApplyOutcome Outcome,
    ProjectEditResult? Edit,
    string? Message)
{
    /// <summary>
    /// Gets a value indicating whether the edit produced an undoable change.
    /// </summary>
    /// <remarks>
    /// A no-op same-hash edit is <see cref="EditApplyOutcome.Applied"/> with no change; consumers must not treat it
    /// as a project edit (for example, it does not clear a pending recovery candidate).
    /// </remarks>
    public bool HasChange => Edit?.Change is not null;
}
