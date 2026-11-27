// SPDX-License-Identifier: MIT

namespace Mocassin.App.ViewModels.Editors.Usage;

/// <summary>
/// One dependent object discovered by the usage index.
/// </summary>
/// <param name="Reference">The dependent object's typed reference.</param>
/// <param name="Kind">The dependency kind.</param>
/// <param name="DisplayName">The object's user-visible name.</param>
/// <param name="Description">A short description used by the dependents list and the inspector.</param>
internal sealed record ModelUsageEntry(
    ModelObjectRef Reference,
    ModelUsageKind Kind,
    string DisplayName,
    string Description)
{
    /// <summary>
    /// Gets a value indicating whether this is an energy reference. The site-deletion cascade is offered only when
    /// every dependent is an energy reference.
    /// </summary>
    public bool IsEnergyReference => Kind == ModelUsageKind.EnergyReference;
}
