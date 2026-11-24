// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.ReferencePicker;

/// <summary>
/// One selectable reference in a <see cref="ReferencePickerViewModel{TReference}"/>.
/// </summary>
/// <typeparam name="TReference">The referenced value type.</typeparam>
/// <param name="Value">The referenced value.</param>
/// <param name="DisplayName">The primary display text.</param>
/// <param name="Details">Optional disambiguating details.</param>
internal sealed record ReferenceOption<TReference>(TReference Value, string DisplayName, string? Details = null)
    where TReference : notnull
{
    /// <summary>
    /// Gets the search text used by the picker.
    /// </summary>
    public string SearchText => Details is null ? DisplayName : $"{DisplayName} {Details}";
}
