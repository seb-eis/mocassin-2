// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Consoles;

/// <summary>
/// Severity of a console entry. It mirrors the logging levels without depending on a logging package.
/// </summary>
internal enum ConsoleSeverity
{
    /// <summary>
    /// Trace-level diagnostic output.
    /// </summary>
    Trace,

    /// <summary>
    /// Debug output.
    /// </summary>
    Debug,

    /// <summary>
    /// Informational message.
    /// </summary>
    Information,

    /// <summary>
    /// Warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Error.
    /// </summary>
    Error,

    /// <summary>
    /// Critical failure.
    /// </summary>
    Critical,
}
