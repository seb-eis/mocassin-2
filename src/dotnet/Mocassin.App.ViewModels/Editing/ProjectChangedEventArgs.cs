// SPDX-License-Identifier: MIT

using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Editing;

/// <summary>
/// Notification that the coordinator changed, saved, or replaced its document.
/// </summary>
/// <param name="Kind">The kind of change.</param>
/// <param name="Previous">The project value before the event.</param>
/// <param name="Current">The project value after the event; equal to <paramref name="Previous"/> for a save.</param>
/// <param name="Description">The user-visible change description.</param>
internal sealed record ProjectChangedEventArgs(
    ProjectChangeKind Kind,
    ProjectModel Previous,
    ProjectModel Current,
    string Description);
