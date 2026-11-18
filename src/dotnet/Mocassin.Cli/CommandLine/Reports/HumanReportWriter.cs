// SPDX-License-Identifier: MIT

using System.Globalization;

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// Writes the human diagnostic table and summary shared by the command reports.
/// </summary>
internal static class HumanReportWriter
{
    /// <summary>
    /// Writes the diagnostic table, the summary counts, and the outcome line.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="envelope">The report envelope.</param>
    public static void Write(TextWriter writer, ReportEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(envelope);

        writer.WriteLine("SEVERITY  CODE    MESSAGE");
        foreach (var diagnostic in envelope.Diagnostics)
        {
            var path = diagnostic.Path is null
                ? string.Empty
                : string.Create(CultureInfo.InvariantCulture, $" [{diagnostic.Path.Domain}/{diagnostic.Path.ObjectId}]");
            var hint = string.IsNullOrWhiteSpace(diagnostic.Hint) ? string.Empty : $" Hint: {diagnostic.Hint}";
            writer.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{diagnostic.Severity.ToUpperInvariant(),-8}  {diagnostic.Code,-6}  {diagnostic.Message}{path}{hint}"));
        }

        writer.WriteLine();
        writer.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Summary: {envelope.Summary.Errors} error(s), {envelope.Summary.Warnings} warning(s), " +
                $"{envelope.Summary.Infos} info(s)"));
        writer.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Result: {(envelope.Ok ? "ok" : "failed")} (exit {envelope.ExitCode})"));
    }
}
