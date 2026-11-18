// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// The deduplicated error, warning, and info counts of a command report.
/// </summary>
/// <param name="Errors">The number of error diagnostics.</param>
/// <param name="Warnings">The number of warning diagnostics.</param>
/// <param name="Infos">The number of info diagnostics.</param>
internal sealed record ReportSummary(int Errors, int Warnings, int Infos);
