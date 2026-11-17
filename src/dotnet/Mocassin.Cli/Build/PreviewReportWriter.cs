// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text.Json;

namespace Mocassin.Cli.Build;

/// <summary>
/// Builds and writes the human and <c>build-preview-v1</c> forms of a no-write build preview.
/// </summary>
internal static class PreviewReportWriter
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// Writes the JSON report as exactly one object.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteJson(TextWriter writer, BuildPreviewReport report) =>
        writer.WriteLine(JsonSerializer.Serialize(report, jsonOptions));

    /// <summary>
    /// Writes the stable human-readable preview and diagnostic summary.
    /// </summary>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void WriteHuman(TextWriter writer, BuildPreviewReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var preview = report.Preview;
        if (preview is not null)
        {
            writer.WriteLine($"Template: {preview.TemplateName} ({preview.TemplateId})");
            writer.WriteLine(
                $"Jobs: {preview.TotalJobs} across {report.Summary.CollectionCount} collection(s) and " +
                $"{report.Summary.ConfigCount} configuration(s)");
            foreach (var collection in preview.Collections)
            {
                writer.WriteLine();
                writer.WriteLine(
                    $"Collection {collection.Name} [{collection.Kind}] jobs={collection.JobCount} " +
                    $"dualDof={collection.UseDualDofCorrection}");
                foreach (var optimizer in collection.Optimizers)
                {
                    writer.WriteLine(
                        $"  Optimizer site={optimizer.SiteId} particle={optimizer.ParticleId} targets={optimizer.TargetCount} " +
                        $"removedStartOptions={FormatNullable(optimizer.RemovedStartOptions)}");
                }

                foreach (var config in collection.Configs)
                {
                    writer.WriteLine(
                        $"  Config {config.Name}: {config.CellA}x{config.CellB}x{config.CellC}, " +
                        $"positions={config.PositionCount}, jobs={config.JobCount}, " +
                        $"charge {config.TotalChargeBefore.ToString("R", CultureInfo.InvariantCulture)} -> " +
                        $"{config.TotalChargeAfter.ToString("R", CultureInfo.InvariantCulture)}, " +
                        $"neutral={config.IsChargeNeutral}");
                    foreach (var doping in config.Dopings)
                    {
                        writer.WriteLine(
                            $"    Doping {doping.DopingId}: requested={doping.RequestedFraction.ToString("R", CultureInfo.InvariantCulture)} " +
                            $"available={doping.AvailablePrimary} truncated={doping.RequestedPrimary} " +
                            $"applied={doping.AppliedPrimary}+{doping.AppliedDependent} " +
                            $"effective={doping.EffectiveFraction.ToString("R", CultureInfo.InvariantCulture)}");
                    }
                }
            }

            writer.WriteLine();
        }

        writer.WriteLine("SEVERITY  CODE    MESSAGE");
        foreach (var diagnostic in report.Diagnostics)
        {
            var path = diagnostic.Path is null ? string.Empty : $" [{diagnostic.Path.Domain}/{diagnostic.Path.ObjectId}]";
            var hint = string.IsNullOrWhiteSpace(diagnostic.Hint) ? string.Empty : $" Hint: {diagnostic.Hint}";
            writer.WriteLine(
                $"{diagnostic.Severity.ToUpperInvariant(),-8}  {diagnostic.Code,-6}  {diagnostic.Message}{path}{hint}");
        }

        writer.WriteLine();
        writer.WriteLine(
            $"Summary: {report.Summary.Errors} error(s), {report.Summary.Warnings} warning(s), " +
            $"{report.Summary.Infos} info(s)");
        writer.WriteLine($"Result: {(report.Valid ? "valid" : "invalid")} (exit {report.ExitCode})");
    }

    private static string FormatNullable(int? value) =>
        value is null ? "unknown" : value.Value.ToString(CultureInfo.InvariantCulture);
}
