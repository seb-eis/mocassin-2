// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Registers the parameterization-template diagnostic codes owned by Core.
/// </summary>
/// <remarks>
/// Feature 1 of Step 09 registered the five structural checks that need the project graph. Feature 4 adds the
/// compiled-bound codes that <c>ParameterizationService.Apply</c> itself emits. The remaining compiled-bound
/// <c>PRM</c> codes are registered by the parameterization validation feature.
/// </remarks>
public static class ParameterizationDiagnosticCatalog
{
    /// <summary>
    /// Gets the registered descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> Descriptors { get; } =
    [
        new(
            ParameterizationDiagnosticCodes.ModelSignatureMismatch,
            DiagnosticSeverity.Error,
            "Step 09 — parameterization templates",
            "A template model signature does not match the compiled model",
            "Rebase or recreate the template against the current compiled model.",
            "#prm001"),
        new(
            ParameterizationDiagnosticCodes.UnknownReference,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "A template entry references an unknown key or disagrees with the current model",
            "Remove the stale entry or rebase the template against the current model.",
            "#prm002"),
        new(
            ParameterizationDiagnosticCodes.InvalidEnergy,
            DiagnosticSeverity.Error,
            "Step 09 — parameterization templates",
            "An energy is non-finite or outside the accepted range",
            "Set the energy to a finite value within the accepted -100..100 eV range.",
            "#prm004"),
        new(
            ParameterizationDiagnosticCodes.InvalidFrequency,
            DiagnosticSeverity.Error,
            "Step 09 — parameterization templates",
            "A frequency is non-positive or non-finite",
            "Set the attempt frequency to a positive, finite value.",
            "#prm005"),
        new(
            ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "A site-energy value targets an unknown or forbidden site/particle",
            "Correct the site/particle key or remove the site-energy override.",
            "#prm008"),
        new(
            ParameterizationDiagnosticCodes.DuplicateName,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "Two parameterization templates use the same name",
            "Give each parameterization template a distinct name.",
            "#prm010"),
        new(
            ParameterizationDiagnosticCodes.ForbiddenParticle,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "A pair or group entry names a particle the reference site cannot occupy",
            "Use a particle the reference site can occupy, or remove the entry.",
            "#prm012"),
        new(
            ParameterizationDiagnosticCodes.LimitExceeded,
            DiagnosticSeverity.Error,
            "Step 09 — parameterization templates",
            "A template resource limit is exceeded",
            "Reduce the template size or split the work into smaller templates.",
            "#prm013"),
        new(
            ParameterizationDiagnosticCodes.InvalidKey,
            DiagnosticSeverity.Error,
            "Step 09 — parameterization templates",
            "A stable key is malformed, duplicated, or missing",
            "Use lowercase 64-character hexadecimal stable keys and remove duplicate value keys.",
            "#prm014"),
    ];
}
