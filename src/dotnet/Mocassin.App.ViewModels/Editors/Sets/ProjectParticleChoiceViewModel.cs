// SPDX-License-Identifier: MIT

using Mocassin.Core.Ids;

namespace Mocassin.App.ViewModels.Editors.Sets;

/// <summary>
/// One row of the set detail's "Project particles" drag source.
/// </summary>
/// <param name="Id">The particle id.</param>
/// <param name="Name">The particle name.</param>
/// <param name="Symbol">The particle symbol.</param>
/// <param name="IsMember">Whether the particle is already a member of the open set.</param>
internal sealed record ProjectParticleChoiceViewModel(
    ParticleId Id,
    string Name,
    string Symbol,
    bool IsMember)
{
    /// <summary>
    /// Gets the display text.
    /// </summary>
    public string DisplayName => IsMember ? $"{Name} ({Symbol}) — member" : $"{Name} ({Symbol})";
}
