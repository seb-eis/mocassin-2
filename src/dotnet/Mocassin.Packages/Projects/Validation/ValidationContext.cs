// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Project;

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// The immutable input passed to an additional <see cref="ValidationStage.Build"/> stage.
/// </summary>
/// <param name="Project">The loaded project snapshot.</param>
/// <param name="Options">The options of the current run.</param>
/// <param name="Catalog">The diagnostic catalog used by the run.</param>
/// <param name="PreviousStages">The results of the stages that already ran.</param>
public sealed record ValidationContext(
    Project Project,
    ValidationOptions Options,
    DiagnosticCatalog Catalog,
    ImmutableArray<ValidationStageResult> PreviousStages);
