// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Projects;

/// <summary>
/// Result of one project-session operation.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Session">The affected session, if one exists.</param>
/// <param name="Diagnostics">The operation diagnostics.</param>
/// <param name="Message">The optional user-facing message.</param>
internal sealed record ProjectSessionResult(
    ProjectSessionOutcome Outcome,
    ProjectSession? Session = null,
    ImmutableArray<Diagnostic> Diagnostics = default,
    string? Message = null)
{
    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess => Outcome is ProjectSessionOutcome.Created
        or ProjectSessionOutcome.Opened
        or ProjectSessionOutcome.Activated
        or ProjectSessionOutcome.OpenedReadOnly
        or ProjectSessionOutcome.Saved
        or ProjectSessionOutcome.Reloaded
        or ProjectSessionOutcome.Closed
        or ProjectSessionOutcome.Recovered
        or ProjectSessionOutcome.Discarded;

    /// <summary>
    /// Gets a value indicating whether the session is read-only.
    /// </summary>
    public bool IsReadOnly => Outcome == ProjectSessionOutcome.OpenedReadOnly;
}
