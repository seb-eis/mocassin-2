// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Navigation;

/// <summary>
/// A document or provider can contribute inspector content for the current selection.
/// </summary>
internal interface IInspectorContributor
{
    /// <summary>
    /// Creates inspector content for a selection.
    /// </summary>
    /// <param name="selection">The current shell selection.</param>
    /// <returns>The inspector content, or <see langword="null"/>.</returns>
    public object? CreateInspector(InspectorSelection selection);
}
