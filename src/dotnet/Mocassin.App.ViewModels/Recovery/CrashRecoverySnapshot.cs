// SPDX-License-Identifier: MIT

using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.App.ViewModels.Recovery;

/// <summary>
/// One dirty project snapshot captured on the UI thread for the crash action.
/// </summary>
/// <param name="ProjectId">The project identity.</param>
/// <param name="ProjectPath">The project path.</param>
/// <param name="Project">The immutable project snapshot.</param>
internal sealed record CrashRecoverySnapshot(
    Guid ProjectId,
    string ProjectPath,
    ProjectModel Project);
