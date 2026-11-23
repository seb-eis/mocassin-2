// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.App.ViewModels.Editors.Sets;

/// <summary>
/// One ordered member row of a particle set. The first member is the default occupation badge.
/// </summary>
/// <param name="Id">The referenced particle id.</param>
/// <param name="Name">The particle name.</param>
/// <param name="Symbol">The particle symbol.</param>
/// <param name="IsDefault">Whether this is the first member and therefore the default occupation.</param>
/// <param name="IsMissing">Whether the referenced particle no longer exists.</param>
internal sealed record ParticleSetMemberViewModel(
    ParticleId Id,
    string Name,
    string Symbol,
    bool IsDefault,
    bool IsMissing = false)
{
    /// <summary>
    /// Gets the display text, including the default-occupation badge.
    /// </summary>
    public string DisplayName => IsMissing
        ? $"(missing particle {Id.Value:D})"
        : IsDefault ? $"{Name} ({Symbol}) — default" : $"{Name} ({Symbol})";
}
