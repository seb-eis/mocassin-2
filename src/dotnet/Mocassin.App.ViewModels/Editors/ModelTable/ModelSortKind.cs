// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ModelTable;

/// <summary>
/// The comparer a model-table column uses. Sorting is typed, never over the formatted cell text.
/// </summary>
internal enum ModelSortKind
{
    /// <summary>
    /// Case-insensitive text.
    /// </summary>
    Text,

    /// <summary>
    /// A numeric value.
    /// </summary>
    Number,

    /// <summary>
    /// A stable identifier.
    /// </summary>
    Id,
}
