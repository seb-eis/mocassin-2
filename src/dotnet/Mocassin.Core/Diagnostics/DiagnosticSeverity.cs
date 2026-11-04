// SPDX-License-Identifier: MIT

namespace Mocassin.Core.Diagnostics;

/// <summary>
/// Classifies a diagnostic message by its effect on model usability.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Informational message that does not affect the model.
    /// </summary>
    Info,

    /// <summary>
    /// Suspicious condition that still permits the operation.
    /// </summary>
    Warning,

    /// <summary>
    /// Invalid condition that prevents the operation.
    /// </summary>
    Error,
}
