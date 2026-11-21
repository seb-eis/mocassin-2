// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Diagnostics;

/// <summary>
/// The sortable Problems columns.
/// </summary>
internal enum ProblemsSortMode
{
    /// <summary>
    /// Sort by severity.
    /// </summary>
    Severity,

    /// <summary>
    /// Sort by diagnostic code.
    /// </summary>
    Code,

    /// <summary>
    /// Sort by message.
    /// </summary>
    Message,

    /// <summary>
    /// Sort by source category.
    /// </summary>
    Source,
}
