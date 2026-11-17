// SPDX-License-Identifier: MIT

using System.Globalization;
using Mocassin.Cli.CommandLine.Reports;

namespace Mocassin.Cli.Build;

/// <summary>
/// Writes the <c>build-v1</c> report in its JSON and human forms.
/// </summary>
internal static class BuildReportWriter
{
    /// <summary>
    /// Writes the report as exactly one camelCase JSON object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, BuildReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        JsonReportWriter.Write(writer, report);
    }

    /// <summary>
    /// Writes the human summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, BuildReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);

        writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Mode: {report.Mode}"));
        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Project: {Format(report.ProjectId?.ToString("D"))}"));
        writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Model: {Format(report.ModelSignature)}"));
        if (report.Selection is not null)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Selection: '{Format(report.Selection.JobTemplate.Name)}' / {FormatParameterization(report.Selection.Parameterization)}"));
        }

        if (report.JobSelection is not null)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Jobs: {report.JobSelection.SelectedCount} selected of {report.JobSelection.ExpandedCount} " +
                $"({Format(report.JobSelection.Expression)})"));
        }

        if (report.Package is not null)
        {
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Package: {report.Package.Path} ({report.Package.SizeBytes} bytes, sha256 {report.Package.Sha256})"));
        }

        writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Verification: {report.Verification}"));
        if (report.Timings.Count > 0)
        {
            writer.WriteLine(
                "Timings: "
                + string.Join(
                    ", ",
                    report.Timings.Select(static timing => string.Create(
                        CultureInfo.InvariantCulture,
                        $"{timing.Stage} {timing.Ms.ToString("0.###", CultureInfo.InvariantCulture)} ms"))));
        }

        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Summary: {report.Summary.Errors} error(s), {report.Summary.Warnings} warning(s), " +
            $"{report.Summary.Infos} info(s)"));
        writer.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Result: {(report.Ok ? "ok" : "failed")} (exit {report.ExitCode})"));
    }

    private static string Format(string? value) => string.IsNullOrEmpty(value) ? "-" : value;

    private static string FormatParameterization(BuildReportParameterization? parameterization) =>
        parameterization is null
            ? "-"
            : parameterization.ImplicitDefault
                ? "implicit default"
                : $"'{Format(parameterization.Name)}'";
}
