// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Icons;

/// <summary>
/// A presentation-only icon reference. The view resolves the key through the single icon data template.
/// </summary>
/// <param name="Key">The stable application-local icon key.</param>
internal sealed record AppIcon(string Key)
{
    /// <summary>
    /// Gets a value indicating whether the key resolves to a vendored glyph.
    /// </summary>
    public bool IsKnown => IconKeys.IsKnown(Key);
}
