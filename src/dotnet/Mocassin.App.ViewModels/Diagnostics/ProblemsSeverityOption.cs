// SPDX-License-Identifier: MIT

using Mocassin.Core.Diagnostics;

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// One severity option in the Problems filter.
/// </summary>
/// <param name="Label">The display label.</param>
/// <param name="Severity">The minimum severity, or <see langword="null"/> for all severities.</param>
internal sealed record ProblemsSeverityOption(string Label, DiagnosticSeverity? Severity);
