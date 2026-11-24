// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// The severity filter applied to a model table.
/// </summary>
internal enum ModelSeverityFilter
{
    /// <summary>
    /// Every row.
    /// </summary>
    All,

    /// <summary>
    /// Rows with a warning or error.
    /// </summary>
    Warnings,

    /// <summary>
    /// Rows with an error.
    /// </summary>
    Errors,
}
