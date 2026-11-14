// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Compilation.Diagnostics;

/// <summary>
/// Registers the Step 08 compilation diagnostic descriptors.
/// </summary>
public static class CompilationDiagnosticCatalog
{
    /// <summary>
    /// Gets the registered Step 08 descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> Descriptors { get; } =
    [
        new(
            CompilationDiagnosticCodes.Precondition,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "A required compilation precondition failed",
            "Correct the referenced model object or the compiler input, then compile again.",
            "#cmp001"),
        new(
            CompilationDiagnosticCodes.Structure,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "The structure cannot be encoded or P1-expanded for compilation",
            "Correct the structure, space group, cell, or site coordinates and revalidate.",
            "#cmp002"),
        new(
            CompilationDiagnosticCodes.EnergyGeometry,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "Pair or cluster geometry cannot be resolved during compilation",
            "Correct the environment, cluster, or site geometry and revalidate.",
            "#cmp003"),
        new(
            CompilationDiagnosticCodes.RuleAmbiguity,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "Two rules in one collection share a start state but differ in outcome",
            "Split or correct the overlapping transition rules so one start state has one outcome.",
            "#cmp004"),
        new(
            CompilationDiagnosticCodes.NoValidRules,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "A transition produces no valid rules after conservation filtering",
            "Check the state changes, particle symbols, and total charge for mass and charge conservation.",
            "#cmp005"),
        new(
            CompilationDiagnosticCodes.CompileLimit,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "A configured compilation limit is exceeded",
            "Reduce the model size or raise the documented compiler limit knowingly.",
            "#cmp006"),
        new(
            CompilationDiagnosticCodes.PathExpansion,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "A transition path cannot be expanded into compiled directions",
            "Place every path point on a canonical P1 position and revalidate the transition.",
            "#cmp007"),
        new(
            CompilationDiagnosticCodes.Conservation,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "Mass or charge conservation cannot be evaluated",
            "Correct the state changes, particle symbols, or charges and revalidate.",
            "#cmp008"),
        new(
            CompilationDiagnosticCodes.UnusedTransition,
            DiagnosticSeverity.Warning,
            "Step 08 — model compilation pipeline",
            "A transition is not referenced by any simulation and was not compiled",
            "Reference the transition from a simulation or remove it.",
            "#cmp009"),
        new(
            CompilationDiagnosticCodes.AdvisoryBudget,
            DiagnosticSeverity.Warning,
            "Step 08 — model compilation pipeline",
            "The advisory rule or table budget is exceeded",
            "Review the model size; the hard compilation limit has not been exceeded.",
            "#cmp010"),
        new(
            CompilationDiagnosticCodes.Signature,
            DiagnosticSeverity.Error,
            "Step 08 — model compilation pipeline",
            "The compiled model cannot be signed",
            "Check the compiled model for missing provenance or non-finite values.",
            "#cmp011"),
        new(
            CompilationDiagnosticCodes.CacheIntegrity,
            DiagnosticSeverity.Warning,
            "Step 08 — model compilation pipeline",
            "A cached stage result failed its integrity check and was recomputed",
            "No action is required; the compiler recomputed the affected stage.",
            "#cmp012"),
    ];
}
