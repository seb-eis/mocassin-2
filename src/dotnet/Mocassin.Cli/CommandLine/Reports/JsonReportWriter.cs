// SPDX-License-Identifier: MIT

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// Serializes a command report as exactly one camelCase JSON object.
/// </summary>
/// <remarks>
/// The report is serialized to a buffer before it is written, so a serialization failure cannot leave a
/// truncated object on standard output. Numbers use the invariant shortest round-trip form, which is
/// <see cref="JsonSerializer"/>'s default.
/// </remarks>
internal static class JsonReportWriter
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Serializes a report to its canonical JSON text.
    /// </summary>
    /// <typeparam name="TReport">The report type.</typeparam>
    /// <param name="report">The report.</param>
    /// <returns>The JSON text without a trailing newline.</returns>
    public static string Serialize<TReport>(TReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, options);
    }

    /// <summary>
    /// Writes a report as exactly one JSON object followed by a newline.
    /// </summary>
    /// <typeparam name="TReport">The report type.</typeparam>
    /// <param name="writer">The standard-output writer.</param>
    /// <param name="report">The report.</param>
    public static void Write<TReport>(TextWriter writer, TReport report)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine(Serialize(report));
    }
}
