// SPDX-License-Identifier: MIT

namespace Mocassin.Cli.CommandLine.Reports;

/// <summary>
/// A navigable model path in a command report.
/// </summary>
/// <param name="Domain">The model domain, for example <c>transitions</c>.</param>
/// <param name="ObjectId">The canonical UUID of the offending object.</param>
/// <param name="Property">The optional property name inside the object.</param>
internal sealed record ReportPath(string Domain, string ObjectId, string? Property);
