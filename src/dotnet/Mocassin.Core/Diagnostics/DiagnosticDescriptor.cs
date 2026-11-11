// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// The stable, documented description of one diagnostic code.
/// </summary>
/// <param name="Code">The stable diagnostic code, for example <c>TRN012</c>.</param>
/// <param name="DefaultSeverity">The severity the producing rule normally uses.</param>
/// <param name="Owner">The step or domain that owns the rule.</param>
/// <param name="Title">A short human-readable title.</param>
/// <param name="Help">General help text that does not contradict the per-instance hint.</param>
/// <param name="DocumentationAnchor">The anchor of the diagnostic in the reference documentation.</param>
public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticSeverity DefaultSeverity,
    string Owner,
    string Title,
    string Help,
    string DocumentationAnchor);
