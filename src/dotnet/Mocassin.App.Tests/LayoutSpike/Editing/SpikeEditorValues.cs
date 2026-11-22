// SPDX-License-Identifier: MIT

namespace Mocassin.App.Tests.LayoutSpike.Editing;

/// <summary>
/// A typed snapshot of the current editor field values.
/// </summary>
internal sealed class SpikeEditorValues
{
    private readonly Dictionary<string, SpikeEditorField> fields;

    /// <summary>
    /// Initializes a snapshot from the editor's fields.
    /// </summary>
    /// <param name="fields">The editor fields.</param>
    public SpikeEditorValues(IEnumerable<SpikeEditorField> fields) =>
        this.fields = fields.ToDictionary(field => field.Key, StringComparer.Ordinal);

    /// <summary>
    /// Gets a text value.
    /// </summary>
    /// <param name="key">The field key.</param>
    /// <returns>The text, or an empty string.</returns>
    public string Text(string key) => fields.TryGetValue(key, out var field) ? field.Text : string.Empty;

    /// <summary>
    /// Gets a choice value.
    /// </summary>
    /// <param name="key">The field key.</param>
    /// <returns>The choice, or an empty string.</returns>
    public string Choice(string key) => fields.TryGetValue(key, out var field) ? field.SelectedChoice : string.Empty;

    /// <summary>
    /// Gets a reference value.
    /// </summary>
    /// <param name="key">The field key.</param>
    /// <returns>The referenced identifier, or <see langword="null"/>.</returns>
    public Guid? Reference(string key) =>
        fields.TryGetValue(key, out var field) ? field.SelectedOption?.Id : null;
}
