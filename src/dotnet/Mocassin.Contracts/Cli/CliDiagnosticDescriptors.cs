// SPDX-License-Identifier: MIT

using System.Collections.Immutable;
using Mocassin.Core.Diagnostics;

namespace Mocassin.Contracts.Cli;

/// <summary>
/// Registers the Step 12 <c>CLI</c> diagnostic descriptors.
/// </summary>
public static class CliDiagnosticDescriptors
{
    /// <summary>
    /// Gets the registered Step 12 descriptors, sorted by ordinal code.
    /// </summary>
    public static ImmutableArray<DiagnosticDescriptor> All { get; } =
    [
        new(
            CliDiagnosticCodes.Usage,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The command line is not valid",
            "Correct the command line; see the command help and the response-file rules in the CLI reference.",
            "#cli001"),
        new(
            CliDiagnosticCodes.Io,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "An input or output operation failed",
            "Check the path, permissions, free disk space, and that the requested input exists.",
            "#cli002"),
        new(
            CliDiagnosticCodes.OutputExists,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The output already exists and overwriting was not requested",
            "Choose another output path or pass --force to replace the existing file.",
            "#cli003"),
        new(
            CliDiagnosticCodes.Selection,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The build recipe selection is missing, ambiguous, or of the wrong kind",
            "Pass an explicit --template, --job-template, or --parameterization ID, or make the project recipe unique.",
            "#cli004"),
        new(
            CliDiagnosticCodes.JobSelection,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The job selection is malformed, empty, or out of range",
            "Use \"all\" or a comma-separated list of 1-based job IDs and inclusive ascending ranges.",
            "#cli005"),
        new(
            CliDiagnosticCodes.Cancelled,
            DiagnosticSeverity.Info,
            "Step 12 — build orchestration and CLI",
            "The operation was cancelled",
            "No action is required; rerun the command to complete it.",
            "#cli006"),
        new(
            CliDiagnosticCodes.Internal,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "An unexpected internal error occurred",
            "Set MOCASSIN_DEBUG=1 to print a stack trace and report the failure.",
            "#cli007"),
        new(
            CliDiagnosticCodes.Reserved,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The command or option is reserved for a later step",
            "Use the command or option in the step named in the message.",
            "#cli008"),
        new(
            CliDiagnosticCodes.NotAPackage,
            DiagnosticSeverity.Error,
            "Step 12 — build orchestration and CLI",
            "The input cannot be inspected as a simulation package",
            "Run \"mocassin verify <package>\" for package-specific detail, or rebuild the package.",
            "#cli009"),
    ];
}
