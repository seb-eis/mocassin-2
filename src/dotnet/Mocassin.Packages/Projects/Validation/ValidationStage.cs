// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// The ordered stages of the project validation pipeline.
/// </summary>
public enum ValidationStage
{
    /// <summary>
    /// File bounds, encoding, JSON, schema, version, and migration checks.
    /// </summary>
    Load = 0,

    /// <summary>
    /// Core graph rules and domain invariants composed by <see cref="ProjectValidationService"/>.
    /// </summary>
    Model = 1,

    /// <summary>
    /// Relationships that span several model domains.
    /// </summary>
    CrossDomain = 2,

    /// <summary>
    /// Buildability, rule-count, and memory-budget checks.
    /// </summary>
    Feasibility = 3,

    /// <summary>
    /// Extension point for parameterization, job-template, and package checks owned by later steps.
    /// </summary>
    Build = 4,
}
