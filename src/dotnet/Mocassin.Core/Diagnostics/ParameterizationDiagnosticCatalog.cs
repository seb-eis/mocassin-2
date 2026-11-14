// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Registers the structural parameterization-template diagnostic codes owned by Core.
/// </summary>
/// <remarks>
/// Feature 1 of Step 09 registers only the five checks that need the project graph. The compiled-model-bound
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
            ParameterizationDiagnosticCodes.DuplicateName,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "Two parameterization templates use the same name",
            "Give each parameterization template a distinct name.",
            "#prm010"),
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
