// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// One diagnostic in a command report, using exactly the <c>validation-v1</c> element shape.
/// </summary>
/// <param name="Code">The stable diagnostic code.</param>
/// <param name="Severity">The lower-case severity name.</param>
/// <param name="Message">The human-readable message.</param>
/// <param name="Path">The optional model path.</param>
/// <param name="Hint">An optional actionable hint.</param>
/// <param name="Related">Optional paths that help explain the diagnostic.</param>
internal sealed record ReportDiagnostic(
    string Code,
    string Severity,
    string Message,
    ReportPath? Path,
    string? Hint,
    IReadOnlyList<ReportPath> Related);
