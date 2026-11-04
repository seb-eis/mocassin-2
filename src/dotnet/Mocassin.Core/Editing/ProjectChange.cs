// SPDX-License-Identifier: MIT

using ProjectModel = Mocassin.Core.Project.Project;

namespace Mocassin.Core.Editing;

/// <summary>
/// One undoable transition between two immutable project values.
/// </summary>
/// <param name="Description">The user-visible description of the change.</param>
/// <param name="Before">The project value before the change.</param>
/// <param name="After">The project value after the change.</param>
public sealed record ProjectChange(string Description, ProjectModel Before, ProjectModel After);
