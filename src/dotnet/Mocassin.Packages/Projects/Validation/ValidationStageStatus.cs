// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// The execution status of one validation stage.
/// </summary>
public enum ValidationStageStatus
{
    /// <summary>
    /// The stage ran and produced no raw error.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The stage did not run.
    /// </summary>
    Skipped = 1,

    /// <summary>
    /// The stage ran and produced at least one raw error.
    /// </summary>
    Failed = 2,
}
