// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Registers the parameterization-template diagnostic codes owned by Step 09.
/// </summary>
/// <remarks>
/// The catalog contains every non-retired <c>PRM</c> descriptor. Structural checks are emitted by Core
/// <c>ProjectValidator</c>; compiled-bound checks and the rebase report are emitted by <c>Mocassin.Packages</c>.
/// The retired <c>PRM007</c> code is not registered.
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
            ParameterizationDiagnosticCodes.RequiredValuesUnset,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "Required pair, group, or kinetic-rule values are unset and defaults are applied",
            "Set the missing values explicitly, or review the applied defaults before building.",
            "#prm003"),
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
            "A frequency is non-positive, non-finite, or too large for the canonical projection",
            "Set the attempt frequency to a positive, finite value that survives 12-decimal quantization.",
            "#prm005"),
        new(
            ParameterizationDiagnosticCodes.NoExplicitValues,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "An explicit template contains no explicit values",
            "Set at least one value, or build with the implicit default parameterization.",
            "#prm006"),
        new(
            ParameterizationDiagnosticCodes.InvalidSiteEnergyTarget,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "A site-energy value targets an unknown or forbidden site/particle",
            "Correct the site/particle key or remove the site-energy override.",
            "#prm008"),
        new(
            ParameterizationDiagnosticCodes.RebaseReport,
            DiagnosticSeverity.Info,
            "Step 09 — parameterization templates",
            "A parameterization template was rebased against the current compiled model",
            "Review every copied, defaulted, dropped, and ambiguous value before saving the rebased template.",
            "#prm009"),
        new(
            ParameterizationDiagnosticCodes.DuplicateName,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "Two parameterization templates use the same name",
            "Give each parameterization template a distinct name.",
            "#prm010"),
        new(
            ParameterizationDiagnosticCodes.MetropolisRuleSetIgnored,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "A Metropolis transition rule set is not parameterizable and is ignored",
            "Remove the Metropolis rule set; MMC rules are not parameterizable.",
            "#prm011"),
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
        new(
            ParameterizationDiagnosticCodes.UniformValues,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "Explicit pair or group values are suspiciously uniform",
            "Check for a bulk-fill or paste error and vary the reported values deliberately.",
            "#prm015"),
        new(
            ParameterizationDiagnosticCodes.ImplausibleFrequency,
            DiagnosticSeverity.Warning,
            "Step 09 — parameterization templates",
            "An attempt frequency is outside the plausible band but still positive and finite",
            "Check the attempt frequency; the accepted physical range is 1e-3..1e18 Hz.",
            "#prm016"),
    ];
}
