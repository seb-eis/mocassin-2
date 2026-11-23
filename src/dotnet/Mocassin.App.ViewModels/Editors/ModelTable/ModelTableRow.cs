// SPDX-License-Identifier: MIT

using System.Collections.Immutable;

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// The severity a row surfaces as text. Status is never colour-only.
/// </summary>
internal enum ModelRowSeverity
{
    /// <summary>
    /// No diagnostic.
    /// </summary>
    None,

    /// <summary>
    /// Informational.
    /// </summary>
    Info,

    /// <summary>
    /// A warning.
    /// </summary>
    Warning,

    /// <summary>
    /// An error.
    /// </summary>
    Error,
}

/// <summary>
/// One model-table row: the stable object id, the formatted cells, and the textual severity.
/// </summary>
/// <param name="ObjectId">The stable project object id used for selection and navigation.</param>
/// <param name="Cells">The cells in column order.</param>
/// <param name="Severity">The row's worst diagnostic severity.</param>
/// <param name="SeverityText">The announcement text for the severity.</param>
internal sealed record ModelTableRow(
    Guid ObjectId,
    ImmutableArray<ModelTableCell> Cells,
    ModelRowSeverity Severity = ModelRowSeverity.None,
    string SeverityText = "")
{
    /// <summary>
    /// Gets a value indicating whether the row has an error.
    /// </summary>
    public bool HasError => Severity == ModelRowSeverity.Error;

    /// <summary>
    /// Gets a value indicating whether the row has a warning or error.
    /// </summary>
    public bool HasWarning => Severity is ModelRowSeverity.Warning or ModelRowSeverity.Error;
}
