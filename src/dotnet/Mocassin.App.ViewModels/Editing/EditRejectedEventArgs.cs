// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// Notification that the structural validator refused an edit. It carries diagnostics, not a project value.
/// </summary>
/// <param name="Description">The user-visible change description.</param>
/// <param name="Diagnostics">The validation diagnostics; at least one is an error.</param>
internal sealed record EditRejectedEventArgs(
    string Description,
    ImmutableArray<Diagnostic> Diagnostics);
