// SPDX-License-Identifier: MIT

namespace Mocassin.Packages.Projects.Validation;

/// <summary>
/// Raw diagnostic counts before any policy promotion.
/// </summary>
/// <param name="Errors">The number of error diagnostics.</param>
/// <param name="Warnings">The number of warning diagnostics.</param>
/// <param name="Infos">The number of informational diagnostics.</param>
public sealed record DiagnosticSummary(int Errors, int Warnings, int Infos);
